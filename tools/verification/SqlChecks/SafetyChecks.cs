using Microsoft.Data.SqlClient;

// Run with --safety-checks. These checks never open a connection or write files.
internal static class SafetyChecks
{
    public static int Run()
    {
        const string allowed = "Server=localhost\\SQLEXPRESS;Database=master;Integrated Security=True;TrustServerCertificate=True";
        var failures = new List<string>();
        var checks = 0;
        void Check(string name, Action action)
        {
            checks++;
            try { action(); Console.WriteLine($"PASS {name}"); }
            catch { failures.Add(name); Console.WriteLine($"FAIL {name}"); }
        }
        void Reject(string name, Action action) => Check(name, () =>
        {
            try { action(); }
            catch (VerificationSafetyException) { return; }
            throw new InvalidOperationException("Unsafe input was accepted.");
        });

        Check("local-master-connection-accepted", () =>
        {
            var settings = VerificationSettings.ParseConnection(allowed);
            if (settings.InitialCatalog != "master" || !settings.IntegratedSecurity)
                throw new InvalidOperationException();
        });
        Reject("daily-database-rejected", () => VerificationSettings.ParseConnection(allowed.Replace("Database=master", "Database=OnlineStore")));
        Reject("missing-catalog-rejected", () => VerificationSettings.ParseConnection("Server=localhost;Integrated Security=True"));
        Reject("missing-environment-connection-rejected", () => VerificationSettings.ParseConnection(null));
        Reject("remote-server-rejected", () => VerificationSettings.ParseConnection(allowed.Replace("localhost\\SQLEXPRESS", "production.example.com")));
        Reject("failover-server-rejected", () => VerificationSettings.ParseConnection(allowed + ";Failover Partner=production.example.com"));
        Reject("attached-file-rejected", () => VerificationSettings.ParseConnection(allowed + ";AttachDbFilename=E:\\daily.mdf"));
        Reject("user-instance-rejected", () => VerificationSettings.ParseConnection(allowed + ";User Instance=True"));
        Reject("read-only-connection-rejected", () => VerificationSettings.ParseConnection(allowed + ";ApplicationIntent=ReadOnly"));
        Reject("malformed-connection-rejected", () => VerificationSettings.ParseConnection("not-a-connection"));
        Reject("daily-database-name-rejected", () => VerificationSettings.ValidateDatabaseName("OnlineStore"));
        Reject("sql-identifier-injection-rejected", () => VerificationSettings.ValidateDatabaseName("OnlineStore_Verification_20261029_000000_0123456789abcdef];DROP DATABASE OnlineStore;--"));
        Check("fresh-generated-database-names-accepted", () =>
        {
            var first = VerificationSettings.NewDatabaseName();
            var second = VerificationSettings.NewDatabaseName();
            VerificationSettings.ValidateDatabaseName(first);
            VerificationSettings.ValidateDatabaseName(second);
            if (first == second) throw new InvalidOperationException();
        });
        Check("e-drive-path-accepted", () => VerificationSettings.RequireEDrivePath(@"E:\verification\run", "test"));
        Reject("c-drive-path-rejected", () => VerificationSettings.RequireEDrivePath(@"C:\Users\test", "test"));
        Reject("relative-path-rejected", () => VerificationSettings.RequireEDrivePath("artifacts/run", "test"));
        Reject("unc-path-rejected", () => VerificationSettings.RequireEDrivePath(@"\\server\share\run", "test"));
        Console.WriteLine($"Safety checks: {checks - failures.Count}/{checks} passed; no SQL connections opened.");
        return failures.Count == 0 ? 0 : 1;
    }
}
