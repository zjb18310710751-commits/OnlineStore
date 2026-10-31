using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

internal static class RecoveryDemo
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string[] RequiredSteps =
    ["create-isolated-database", "start-owned-web-process", "healthy-http", "baseline-data",
     "take-owned-database-offline", "outage-http", "restore-owned-database-online",
     "recovered-http", "stable-data-preserved", "stop-owned-web-process"];

    public static async Task<int> RunAsync(string outputArgument, string assemblyArgument)
    {
        string output;
        string assembly;
        try
        {
            if (!OperatingSystem.IsWindows()) throw new VerificationSafetyException("Recovery verification requires Windows and local SQL Server.");
            VerificationSettings.RequireEDrivePath(outputArgument, "Recovery output");
            VerificationSettings.RequireEDrivePath(assemblyArgument, "Published assembly");
            output = Path.GetFullPath(outputArgument);
            assembly = Path.GetFullPath(assemblyArgument);
            if (!string.Equals(Path.GetFileName(assembly), "OnlineStore.dll", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(assembly)
                || !File.Exists(Path.ChangeExtension(assembly, ".runtimeconfig.json")))
                throw new VerificationSafetyException("Supply the published OnlineStore.dll and its runtime configuration on E:.");
            if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
                throw new VerificationSafetyException("Recovery output must be new or empty; previous evidence will not be overwritten.");
            Directory.CreateDirectory(output);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(SafeError(exception));
            return 2;
        }

        var startedAtUtc = DateTime.UtcNow;
        await using var runLog = new StreamWriter(Path.Combine(output, "recovery-verification.log")) { AutoFlush = true };
        var steps = new List<StepResult>();
        var startupProbes = new List<HttpProbe>();
        var healthyProbes = new List<HttpProbe>();
        var outageProbes = new List<HttpProbe>();
        var recoveryProbes = new List<HttpProbe>();
        var databaseName = VerificationSettings.NewDatabaseName();
        VerificationSettings.ValidateDatabaseName(databaseName);
        SqlConnectionStringBuilder? masterSettings = null;
        string? connection = null, dataPath = null, logPath = null, failure = null;
        var databaseCreated = false;
        var creationAttempted = false;
        var offlineAttempted = false;
        var onlineRestored = false;
        int? databaseId = null, processId = null;
        long? recoveryMilliseconds = null;
        DatabaseSnapshot? before = null, after = null;
        Process? child = null;
        HttpClient? client = null;
        StreamWriter? stdout = null, stderr = null;
        Task stdoutPump = Task.CompletedTask, stderrPump = Task.CompletedTask;
        using var cancelled = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) => { eventArgs.Cancel = true; cancelled.Cancel(); };
        Console.CancelKeyPress += cancelHandler;

        async Task Step(string name, Func<Task<object>> action)
        {
            var timer = Stopwatch.StartNew();
            try
            {
                var evidence = await action();
                steps.Add(new(name, true, timer.ElapsedMilliseconds, evidence, null));
                Console.WriteLine($"PASS {name} ({timer.ElapsedMilliseconds} ms)");
                await runLog.WriteLineAsync($"PASS {name} ({timer.ElapsedMilliseconds} ms)");
            }
            catch (Exception exception)
            {
                var error = SafeError(exception);
                steps.Add(new(name, false, timer.ElapsedMilliseconds, null, error));
                Console.WriteLine($"FAIL {name}: {error}");
                await runLog.WriteLineAsync($"FAIL {name}: {error}");
                throw;
            }
        }

        // Name, successful creation, original database id and exact file paths are checked before either ALTER.
        async Task AlterOwnedDatabase(string state)
        {
            Check(databaseCreated && databaseId.HasValue && masterSettings is not null,
                "No successfully created database is owned by this recovery run.");
            VerificationSettings.ValidateDatabaseName(databaseName);
            Check(state is "OFFLINE" or "ONLINE", "Invalid recovery database state.");
            await using var master = new SqlConnection(masterSettings!.ConnectionString);
            await master.OpenAsync();
            await using var owned = master.CreateCommand();
            owned.CommandTimeout = 10;
            owned.CommandText = "SELECT COUNT(*) FROM sys.databases d WHERE d.name=@name AND d.database_id=@id AND EXISTS (SELECT 1 FROM sys.master_files f WHERE f.database_id=d.database_id AND f.physical_name=@data) AND EXISTS (SELECT 1 FROM sys.master_files f WHERE f.database_id=d.database_id AND f.physical_name=@log);";
            owned.Parameters.AddWithValue("@name", databaseName);
            owned.Parameters.AddWithValue("@id", databaseId!.Value);
            owned.Parameters.AddWithValue("@data", dataPath!);
            owned.Parameters.AddWithValue("@log", logPath!);
            Check(Convert.ToInt32(await owned.ExecuteScalarAsync()) == 1, "Database ownership changed; refusing ALTER DATABASE.");
            await using var alter = master.CreateCommand();
            alter.CommandTimeout = 15;
            alter.CommandText = $"ALTER DATABASE [{databaseName}] SET {state}" + (state == "OFFLINE" ? " WITH ROLLBACK IMMEDIATE;" : ";");
            await alter.ExecuteNonQueryAsync();
            if (state == "ONLINE")
            {
                await using var verify = master.CreateCommand();
                verify.CommandText = "SELECT state_desc FROM sys.databases WHERE name=@name AND database_id=@id;";
                verify.Parameters.AddWithValue("@name", databaseName);
                verify.Parameters.AddWithValue("@id", databaseId.Value);
                Check(string.Equals(await verify.ExecuteScalarAsync() as string, "ONLINE", StringComparison.Ordinal), "Database did not return ONLINE.");
                onlineRestored = true;
            }
        }

        try
        {
            masterSettings = VerificationSettings.ParseConnection(Environment.GetEnvironmentVariable(VerificationSettings.ConnectionVariable));
            masterSettings.ConnectTimeout = 3;
            masterSettings.ConnectRetryCount = 0;
            var dataDirectory = VerificationSettings.ExistingDataDirectory(Environment.GetEnvironmentVariable(VerificationSettings.DataDirectoryVariable));
            dataPath = Path.Combine(dataDirectory, databaseName + ".mdf");
            logPath = Path.Combine(dataDirectory, databaseName + "_log.ldf");
            await Step("create-isolated-database", async () =>
            {
                await using var master = new SqlConnection(masterSettings.ConnectionString);
                await master.OpenAsync(cancelled.Token);
                await using var exists = master.CreateCommand();
                exists.CommandText = "SELECT DB_ID(@name);";
                exists.Parameters.AddWithValue("@name", databaseName);
                Check(await exists.ExecuteScalarAsync(cancelled.Token) is DBNull, "Generated database already exists; refusing reuse.");
                Check(!File.Exists(dataPath) && !File.Exists(logPath), "Generated SQL files already exist; refusing reuse.");
                creationAttempted = true;
                await using var create = master.CreateCommand();
                create.CommandTimeout = 60;
                create.CommandText = $"CREATE DATABASE [{databaseName}] ON PRIMARY (NAME=N'{databaseName}', FILENAME=N'{SqlLiteral(dataPath)}') LOG ON (NAME=N'{databaseName}_log', FILENAME=N'{SqlLiteral(logPath)}');";
                await create.ExecuteNonQueryAsync(cancelled.Token);
                databaseCreated = true;
                databaseId = Convert.ToInt32(await exists.ExecuteScalarAsync(cancelled.Token));
                connection = new SqlConnectionStringBuilder(masterSettings.ConnectionString) { InitialCatalog = databaseName }.ConnectionString;
                return new { databaseName, databaseId, dataPath, logPath };
            });

            await Step("start-owned-web-process", () =>
            {
                var port = UnusedLoopbackPort();
                var url = $"http://127.0.0.1:{port}";
                var start = new ProcessStartInfo("dotnet")
                {
                    WorkingDirectory = Path.GetDirectoryName(assembly)!, UseShellExecute = false,
                    CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
                };
                start.ArgumentList.Add(assembly);
                start.ArgumentList.Add("--urls");
                start.ArgumentList.Add(url);
                start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
                start.Environment["DOTNET_ENVIRONMENT"] = "Development";
                start.Environment["ASPNETCORE_URLS"] = url;
                start.Environment["ConnectionStrings__DefaultConnection"] = connection;
                start.Environment["DataProtection__KeyDirectory"] = Path.Combine(output, "keys");
                start.Environment["DOTNET_CLI_HOME"] = Path.Combine(output, "dotnet-home");
                start.Environment["TEMP"] = start.Environment["TMP"] = Path.Combine(output, "temp");
                foreach (var directory in new[] { "keys", "dotnet-home", "temp" }) Directory.CreateDirectory(Path.Combine(output, directory));
                stdout = new StreamWriter(Path.Combine(output, "web-stdout.log")) { AutoFlush = true };
                stderr = new StreamWriter(Path.Combine(output, "web-stderr.log")) { AutoFlush = true };
                child = new Process { StartInfo = start };
                Check(child.Start(), "Could not start the isolated web process.");
                processId = child.Id;
                var secrets = new[] { masterSettings.ConnectionString, connection!, masterSettings.Password };
                stdoutPump = PumpLog(child.StandardOutput, stdout, secrets);
                stderrPump = PumpLog(child.StandardError, stderr, secrets);
                client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(url), Timeout = Timeout.InfiniteTimeSpan };
                return Task.FromResult<object>(new { processId, url, assembly, environment = "Development", connectTimeoutSeconds = 3 });
            });

            await Step("healthy-http", async () =>
            {
                using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancelled.Token);
                budget.CancelAfter(TimeSpan.FromSeconds(60));
                while (true)
                {
                    Check(child is { HasExited: false }, "Isolated web process exited before readiness.");
                    var ready = await Probe(client!, "/health/ready", budget.Token);
                    startupProbes.Add(ready);
                    if (ready.Status == 200) break;
                    await Task.Delay(500, budget.Token);
                }
                var live = await Probe(client!, "/health/live", cancelled.Token);
                var catalog = await Probe(client!, "/Products", cancelled.Token);
                healthyProbes.AddRange([live, catalog]);
                Check(live.Status == 200 && catalog.Status == 200, "Healthy liveness and catalog must both return HTTP 200.");
                return new { readinessAttempts = startupProbes.Count, live, catalog };
            });
            await Step("baseline-data", async () =>
            {
                before = await Snapshot(connection!);
                Check(before.ProductCount > 0, "Published application did not seed the isolated catalog.");
                return before;
            });
            await Step("take-owned-database-offline", async () =>
            {
                offlineAttempted = true; // Also restore if SQL executed but the client did not receive its acknowledgement.
                await AlterOwnedDatabase("OFFLINE");
                return new { databaseName, rollbackImmediate = true };
            });
            await Step("outage-http", async () =>
            {
                var live = await Probe(client!, "/health/live", cancelled.Token);
                var ready = await Probe(client!, "/health/ready", cancelled.Token);
                var catalog = await Probe(client!, "/Products", cancelled.Token);
                outageProbes.AddRange([live, ready, catalog]);
                Check(live.Status == 200, "Liveness must stay HTTP 200 during the database outage.");
                Check(ready.Status == 503, "Readiness must return HTTP 503 during the database outage.");
                Check(catalog.Status == 503 && catalog.RetryAfterSeconds > 0 && !catalog.ContainsSqlDetails,
                    "Catalog outage must return HTTP 503, Retry-After and no SQL exception details.");
                return new { live, ready, catalog };
            });
        }
        catch (Exception exception) { failure = SafeError(exception); }
        finally
        {
            if (databaseCreated && offlineAttempted)
            {
                try
                {
                    // Cleanup deliberately ignores Ctrl+C: the owned database must be brought back online first.
                    await Step("restore-owned-database-online", async () =>
                    {
                        await AlterOwnedDatabase("ONLINE");
                        return new { databaseName, onlineRestored };
                    });
                    await Step("recovered-http", async () =>
                    {
                        var timer = Stopwatch.StartNew();
                        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                        while (true)
                        {
                            Check(child is { HasExited: false }, "Isolated web process exited before recovery.");
                            var ready = await Probe(client!, "/health/ready", budget.Token);
                            var catalog = await Probe(client!, "/Products", budget.Token);
                            recoveryProbes.Add(ready);
                            recoveryProbes.Add(catalog);
                            if (ready.Status == 200 && catalog.Status == 200)
                            {
                                recoveryMilliseconds = timer.ElapsedMilliseconds;
                                return new { recoveryMilliseconds, localTargetMilliseconds = 20000, ready, catalog };
                            }
                            await Task.Delay(500, budget.Token);
                        }
                    });
                }
                catch (Exception exception) { failure ??= SafeError(exception); }
                if (onlineRestored && before is not null)
                {
                    try
                    {
                        await Step("stable-data-preserved", async () =>
                        {
                            after = await Snapshot(connection!);
                            Check(JsonSerializer.Serialize(before) == JsonSerializer.Serialize(after),
                                "Stable product, inventory or order data changed across the outage.");
                            return new { before, after };
                        });
                    }
                    catch (Exception exception) { failure ??= SafeError(exception); }
                }
            }
            if (child is not null && processId.HasValue)
            {
                try
                {
                    await Step("stop-owned-web-process", async () =>
                    {
                        if (!child.HasExited) child.Kill();
                        await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                        await Task.WhenAll(stdoutPump, stderrPump).WaitAsync(TimeSpan.FromSeconds(10));
                        return new { processId, exited = child.HasExited, exitCode = child.ExitCode };
                    });
                }
                catch (Exception exception) { failure ??= SafeError(exception); }
            }
            try
            {
                if (stdout is not null) await stdout.DisposeAsync();
                if (stderr is not null) await stderr.DisposeAsync();
            }
            catch (Exception exception) { failure ??= SafeError(exception); }
            child?.Dispose();
            client?.Dispose();
            Console.CancelKeyPress -= cancelHandler;
        }

        var passed = failure is null && RequiredSteps.All(name => steps.Any(step => step.Name == name && step.Passed));
        var report = new
        {
            startedAtUtc, completedAtUtc = DateTime.UtcNow, passed, failure, cancelled = cancelled.IsCancellationRequested,
            database = new { name = databaseName, databaseId, dataPath, logPath, creationAttempted, databaseCreated, offlineAttempted, onlineRestored, retained = true },
            processId, publishedAssembly = assembly, recoveryMilliseconds, localRecoveryTargetMilliseconds = 20000,
            startupProbes, healthyProbes, outageProbes, recoveryProbes,
            dataComparison = new { before, after, equal = before is not null && after is not null && JsonSerializer.Serialize(before) == JsonSerializer.Serialize(after) },
            steps,
            limitations = new[]
            {
                "This is a single local SQL Server outage experiment, not an availability SLA or a load benchmark.",
                "Recovery time is measured after ALTER DATABASE ONLINE completes; the local target is 20 seconds.",
                "The 3-second readiness cancellation budget is cooperative, not an absolute provider deadline.",
                "Before startup migration and seeding finish, Kestrel is not listening and liveness is unavailable.",
                "Only GET requests are polled; no business write is sent or retried. Separate unit tests cover single write invocation.",
                "Order statuses and receipt timestamps are excluded because the background confirmation service can legitimately update them.",
                "Ctrl+C runs cleanup. Forced process termination, OS shutdown or SQL service failure can prevent ONLINE cleanup; inspect the exact retained database in the report.",
                "A loopback ephemeral port is selected before process launch; a competing bind causes the run to fail safely.",
                "Only the child process started by this run is stopped. Generated SQL data and logs remain for inspection."
            }
        };
        await File.WriteAllTextAsync(Path.Combine(output, "recovery-verification.json"), JsonSerializer.Serialize(report, JsonOptions));
        Console.WriteLine(passed ? "PASS recovery verification; report and retained database are available for inspection." : "FAIL recovery verification; inspect recovery-verification.json, including the ONLINE cleanup result.");
        return passed ? 0 : 1;
    }

    private static async Task<HttpProbe> Probe(HttpClient client, string path, CancellationToken token)
    {
        var timer = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Accept.ParseAdd("text/html");
            using var response = await client.SendAsync(request, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            var leaks = new[] { "SqlException", "Microsoft.Data.SqlClient", "Server=", "Password=", " at OnlineStore." }
                .Any(marker => body.Contains(marker, StringComparison.OrdinalIgnoreCase));
            return new(path, (int)response.StatusCode, timer.ElapsedMilliseconds,
                response.Headers.RetryAfter?.Delta?.TotalSeconds, response.Content.Headers.ContentType?.MediaType, leaks, null);
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            return new(path, null, timer.ElapsedMilliseconds, null, null, false, exception.GetType().Name);
        }
    }

    private static async Task<DatabaseSnapshot> Snapshot(string connection)
    {
        await using var sql = new SqlConnection(connection);
        await sql.OpenAsync();
        await using var command = sql.CreateCommand();
        command.CommandTimeout = 10;
        command.CommandText = "SELECT (SELECT COUNT_BIG(*) FROM dbo.Products), (SELECT COUNT_BIG(*) FROM dbo.Orders), (SELECT COUNT_BIG(*) FROM dbo.OrderItems), COALESCE((SELECT SUM(Total) FROM dbo.Orders),0), COALESCE((SELECT SUM(CONVERT(bigint,Quantity)) FROM dbo.OrderItems),0); SELECT Id, Stock FROM dbo.Products ORDER BY Id; SELECT Id, Stock FROM dbo.ProductSkus ORDER BY Id;";
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        var products = reader.GetInt64(0);
        var orders = reader.GetInt64(1);
        var items = reader.GetInt64(2);
        // Order.Total uses HasConversion<double>(); SQL SUM(float) is returned as Double, not Decimal.
        var orderTotal = reader.GetDouble(3);
        var quantity = reader.GetInt64(4);
        var productStock = new List<StockValue>();
        var skuStock = new List<StockValue>();
        await reader.NextResultAsync();
        while (await reader.ReadAsync()) productStock.Add(new(reader.GetInt32(0), reader.GetInt32(1)));
        await reader.NextResultAsync();
        while (await reader.ReadAsync()) skuStock.Add(new(reader.GetInt32(0), reader.GetInt32(1)));
        return new(products, orders, items, orderTotal, quantity, productStock, skuStock);
    }

    private static async Task PumpLog(StreamReader input, StreamWriter output, string[] secrets)
    {
        while (await input.ReadLineAsync() is { } line)
        {
            foreach (var secret in secrets.Where(secret => !string.IsNullOrEmpty(secret)))
                line = line.Replace(secret, "[redacted]", StringComparison.Ordinal);
            line = Regex.Replace(line, """(?i)(password|pwd)\s*=\s*("[^"]*"|'[^']*'|[^;\s]*)""", "$1=[redacted]");
            await output.WriteLineAsync(line);
        }
    }

    private static int UnusedLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try { return ((IPEndPoint)listener.LocalEndpoint).Port; }
        finally { listener.Stop(); }
    }

    private static string SqlLiteral(string value) => value.Replace("'", "''");
    private static void Check(bool condition, string message) { if (!condition) throw new VerificationCheckException(message); }
    private static string SafeError(Exception exception) => exception is VerificationSafetyException or VerificationCheckException
        ? exception.Message : exception is SqlException sql ? $"SQL Server error {sql.Number}; connection details omitted."
        : $"{exception.GetType().Name}; details omitted to protect credentials.";
    private sealed record StepResult(string Name, bool Passed, long ElapsedMilliseconds, object? Evidence, string? Error);
    private sealed record HttpProbe(string Path, int? Status, long ElapsedMilliseconds, double? RetryAfterSeconds, string? ContentType, bool ContainsSqlDetails, string? Error);
    private sealed record StockValue(int Id, int Stock);
    private sealed record DatabaseSnapshot(long ProductCount, long OrderCount, long OrderItemCount, double OrderTotal, long OrderItemQuantity, List<StockValue> ProductStock, List<StockValue> SkuStock);
}
