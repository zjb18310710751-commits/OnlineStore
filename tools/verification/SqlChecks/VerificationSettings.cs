using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

internal static class VerificationSettings
{
    public const string ConnectionVariable = "ONLINESTORE_SQL_TEST_CONNECTION";
    public const string DataDirectoryVariable = "ONLINESTORE_SQL_TEST_DATA_DIRECTORY";
    private static readonly Regex DatabasePattern = new(
        @"\AOnlineStore_Verification_[0-9]{8}_[0-9]{6}_[a-f0-9]{32}\z", RegexOptions.CultureInvariant);
    private static readonly Regex LocalServerPattern = new(
        @"\A(localhost|127\.0\.0\.1|\.|\(local\))(\\[a-zA-Z0-9_]+)?(,[0-9]{1,5})?\z",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static string NewDatabaseName() => $"OnlineStore_Verification_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";

    public static void ValidateDatabaseName(string name)
    {
        if (!DatabasePattern.IsMatch(name))
            throw new VerificationSafetyException("Refusing a database outside the generated verification namespace.");
    }

    public static SqlConnectionStringBuilder ParseConnection(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new VerificationSafetyException($"Set {ConnectionVariable} to an explicit local SQL Server master connection.");
        SqlConnectionStringBuilder builder;
        try { builder = new SqlConnectionStringBuilder(value); }
        catch (ArgumentException)
        {
            // Do not echo the original value or parser message: either may contain a credential.
            throw new VerificationSafetyException($"{ConnectionVariable} is not a valid SQL Server connection string.");
        }
        if (!string.Equals(builder.InitialCatalog, "master", StringComparison.OrdinalIgnoreCase))
            throw new VerificationSafetyException("The verification connection must explicitly target master, never an application database.");
        if (!LocalServerPattern.IsMatch(builder.DataSource))
            throw new VerificationSafetyException("Only a local SQL Server instance is supported by this E-drive verification runner.");
        if (!string.IsNullOrWhiteSpace(builder.FailoverPartner))
            throw new VerificationSafetyException("Failover partners are not supported by this local verification runner.");
        if (!string.IsNullOrWhiteSpace(builder.AttachDBFilename) || builder.UserInstance)
            throw new VerificationSafetyException("Attaching database files and SQL user instances are not supported.");
        if (builder.ApplicationIntent != ApplicationIntent.ReadWrite)
            throw new VerificationSafetyException("The verification connection must permit writes to newly created verification databases.");
        builder.PersistSecurityInfo = false;
        builder.ApplicationName = "OnlineStore isolated SQL verification";
        builder.ConnectTimeout = Math.Min(Math.Max(builder.ConnectTimeout, 1), 30);
        return builder;
    }

    // Validate the Windows drive syntax independently of the host OS, so CI can exercise this guard.
    public static void RequireEDrivePath(string? path, string label)
    {
        if (string.IsNullOrWhiteSpace(path) || !Regex.IsMatch(path, @"^[eE]:[\\/]"))
            throw new VerificationSafetyException($"{label} must be an absolute directory on E:.");
    }

    public static string ExistingDataDirectory(string? value)
    {
        RequireEDrivePath(value, DataDirectoryVariable);
        var fullPath = Path.GetFullPath(value!);
        if (!Directory.Exists(fullPath))
            throw new VerificationSafetyException("The SQL data directory must already exist and be writable by the SQL Server service account.");
        return fullPath;
    }
}

internal sealed class VerificationSafetyException(string message) : Exception(message);
internal sealed class VerificationCheckException(string message) : Exception(message);
