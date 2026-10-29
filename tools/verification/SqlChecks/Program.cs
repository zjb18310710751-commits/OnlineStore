using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

// Original eight regression scenarios: coursework SQL audit, 2026-10-29.
// The executable itself owns database creation; no caller can supply a target database name.
if (args.Length == 3 && args[0] == "--recovery")
{
    Environment.ExitCode = await RecoveryDemo.RunAsync(args[1], args[2]);
    return;
}
if (args.Length == 1 && args[0] == "--safety-checks")
{
    Environment.ExitCode = SafetyChecks.Run();
    return;
}
if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: SqlChecks <absolute-E-drive-artifact-directory> | --safety-checks | --recovery <absolute-E-output-directory> <absolute-E-published-OnlineStore.dll>");
    Environment.ExitCode = 2;
    return;
}
string output;
try
{
    if (!OperatingSystem.IsWindows())
        throw new VerificationSafetyException("Live SQL verification currently requires Windows and an explicit local SQL Server instance.");
    VerificationSettings.RequireEDrivePath(args[0], "Artifact directory");
    output = Path.GetFullPath(args[0]);
    Directory.CreateDirectory(output);
    var reportPath = Path.Combine(output, "sql-verification.json");
    if (File.Exists(reportPath))
        throw new VerificationSafetyException("An existing verification report must not be overwritten. Choose a fresh output directory.");
    using var reservation = new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception is VerificationSafetyException ? exception.Message : "Cannot create the verification artifact directory.");
    Environment.ExitCode = 2;
    return;
}

var startedAtUtc = DateTime.UtcNow;
var connection = string.Empty;
var results = new List<object>();
var databases = new List<object>();
var failures = 0;
string? setupFailure = null;
ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
void Check(bool condition, string message) { if (!condition) throw new VerificationCheckException(message); }
string SafeError(Exception exception) => exception is VerificationSafetyException or VerificationCheckException
    ? exception.Message
    : exception is SqlException sql
        ? $"SQL Server error {sql.Number}; inspect the local SQL Server log (connection values are omitted)."
        : $"{exception.GetType().Name}; exception details omitted to protect connection credentials.";
async Task Run(string name, Func<Task<object>> action)
{
    var timer = Stopwatch.StartNew();
    try
    {
        var evidence = await action();
        results.Add(new { name, passed = true, elapsedMilliseconds = timer.ElapsedMilliseconds, evidence });
        Console.WriteLine($"PASS {name}: {JsonSerializer.Serialize(evidence)}");
    }
    catch (Exception exception)
    {
        failures++;
        var error = SafeError(exception);
        results.Add(new { name, passed = false, elapsedMilliseconds = timer.ElapsedMilliseconds, error });
        Console.WriteLine($"FAIL {name}: {error}");
    }
}
async Task<string> CreateDatabase(SqlConnectionStringBuilder masterSettings, string dataDirectory, string purpose)
{
    var name = VerificationSettings.NewDatabaseName();
    VerificationSettings.ValidateDatabaseName(name);
    var dataPath = Path.Combine(dataDirectory, name + ".mdf");
    var logPath = Path.Combine(dataDirectory, name + "_log.ldf");
    await using var master = new SqlConnection(masterSettings.ConnectionString);
    await master.OpenAsync();
    await using (var exists = master.CreateCommand())
    {
        exists.CommandText = "SELECT DB_ID(@name);";
        exists.Parameters.AddWithValue("@name", name);
        Check(await exists.ExecuteScalarAsync() is DBNull, "Generated database already exists; refusing to reuse it.");
    }
    // Identifiers come only from NewDatabaseName + strict validation; paths are SQL string literals.
    string Literal(string value) => value.Replace("'", "''");
    databases.Add(new { purpose, name, dataPath, logPath, creationAttempted = true });
    await using (var create = master.CreateCommand())
    {
        create.CommandTimeout = 60;
        create.CommandText = $"CREATE DATABASE [{name}] ON PRIMARY (NAME=N'{name}', FILENAME=N'{Literal(dataPath)}') LOG ON (NAME=N'{name}_log', FILENAME=N'{Literal(logPath)}');";
        await create.ExecuteNonQueryAsync();
    }
    Console.WriteLine($"Created isolated database for {purpose}: {name}");
    var target = new SqlConnectionStringBuilder(masterSettings.ConnectionString) { InitialCatalog = name };
    return target.ConnectionString;
}
string Id(string prefix) => prefix + Guid.NewGuid().ToString("N")[..16];
Order OrderFor(Product product, int quantity = 1) => new()
{
    OrderNo = Id("QA-"), UserId = "qa-isolated-buyer", UserName = "SQL audit buyer",
    Address = "Test address", Items = new List<OrderItem>
    {
        new() { ProductId = product.Id, ProductName = product.Name, Price = product.Price, Quantity = quantity }
    }
};
async Task<Product> AddProduct(string label, int stock = 10, decimal price = 10m)
{
    await using var context = Context();
    var shop = new Shop { Name = Id("QA shop ") };
    context.Shops.Add(shop);
    var product = new Product { Name = Id(label), Price = price, Stock = stock, Shop = shop, IsActive = true };
    await new ProductService(context).CreateAsync(product);
    return product;
}

try
{
var masterSettings = VerificationSettings.ParseConnection(Environment.GetEnvironmentVariable(VerificationSettings.ConnectionVariable));
var dataDirectory = VerificationSettings.ExistingDataDirectory(Environment.GetEnvironmentVariable(VerificationSettings.DataDirectoryVariable));
connection = await CreateDatabase(masterSettings, dataDirectory, "fresh-schema-and-service-checks");

await Run("all-migrations-and-verification-attempt-column", async () =>
{
    await using var context = Context();
    await context.Database.MigrateAsync();
    var migrations = context.Database.GetMigrations().ToArray();
    var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
    Check(migrations.SequenceEqual(applied), "Not all migrations were applied in order.");
    Check(applied.Contains("20261029140000_LimitVerificationCodeAttempts"), "Attempt limit migration missing.");
    await using var sql = new SqlConnection(connection);
    await sql.OpenAsync();
    await using var command = sql.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.EmailVerificationCodes') AND name=N'FailedAttempts' AND is_nullable=0;";
    Check(Convert.ToInt32(await command.ExecuteScalarAsync()) == 1, "FailedAttempts column missing or nullable.");
    return new { provider = context.Database.ProviderName, migrations = applied, failedAttemptsColumn = true };
});
if (failures > 0) goto Complete;

await Run("old-migration-upgrade-preserves-existing-verification-code", async () =>
{
    const string previousMigration = "20261028231500_AddReturnShipmentTracking";
    var upgradeConnection = await CreateDatabase(masterSettings, dataDirectory, "old-schema-upgrade");
    await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(upgradeConnection).Options);
    await context.GetService<IMigrator>().MigrateAsync(previousMigration);
    await using (var sql = new SqlConnection(upgradeConnection))
    {
        await sql.OpenAsync();
        await using var command = sql.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.EmailVerificationCodes') AND name=N'FailedAttempts';";
        Check(Convert.ToInt32(await command.ExecuteScalarAsync()) == 0, "Upgrade fixture already contains the new column.");
        command.CommandText = "INSERT INTO dbo.EmailVerificationCodes (Email, Code, ExpiresAt, CreatedAt) VALUES (@email, N'123456', DATEADD(minute, 10, SYSUTCDATETIME()), SYSUTCDATETIME());";
        command.Parameters.AddWithValue("@email", "legacy-upgrade@store.example");
        await command.ExecuteNonQueryAsync();
    }
    await context.Database.MigrateAsync();
    var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
    Check(context.Database.GetMigrations().SequenceEqual(applied), "Old database did not reach all current migrations.");
    var legacy = await context.EmailVerificationCodes.SingleAsync(code => code.Email == "legacy-upgrade@store.example");
    Check(legacy.Code == "123456" && legacy.FailedAttempts == 0, "Migration did not preserve the existing code with zero failed attempts.");
    Check(await new VerificationCodeService(context).VerifyAsync(legacy.Email, legacy.Code), "Existing code cannot be consumed after migration.");
    Check(!await context.EmailVerificationCodes.AnyAsync(code => code.Email == legacy.Email), "Upgraded code was not removed after consumption.");
    return new { previousMigration, applied, oldRowPreserved = true, failedAttemptsDefault = 0, codeConsumed = true };
});

await Run("development-seeding-isolated-database", async () =>
{
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddDataProtection().UseEphemeralDataProtectionProvider();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddSingleton<IHostEnvironment>(new TestEnvironment());
    services.AddDbContext<ApplicationDbContext>(builder => builder.UseSqlServer(connection));
    services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
    await using var provider = services.BuildServiceProvider();
    await DbInitializer.InitializeAsync(provider);
    await using var context = Context();
    var productCount = await context.Products.CountAsync();
    Check(productCount > 0, "Development product seeding failed.");
    Check(await context.Users.AnyAsync(user => user.Email == "admin@store.com"), "Development admin missing.");
    Check(await context.Users.AnyAsync(user => user.Email == "buyer@store.example"), "Development demo buyer missing.");
    return new { products = productCount, orders = await context.Orders.CountAsync(), accounts = await context.Users.CountAsync() };
});

await Run("verification-concurrent-wrong-attempts-capped-at-five", async () =>
{
    var target = Id("attempts-") + "@store.example";
    await using (var seed = Context())
    {
        seed.EmailVerificationCodes.Add(new EmailVerificationCode { Email = target, Code = "123456", ExpiresAt = DateTime.UtcNow.AddMinutes(10) });
        await seed.SaveChangesAsync();
    }
    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var tasks = Enumerable.Range(0, 12).Select(async _ =>
    {
        await using var request = Context();
        await gate.Task;
        return await new VerificationCodeService(request).VerifyAsync(target, "000000");
    }).ToArray();
    gate.SetResult();
    var decisions = await Task.WhenAll(tasks);
    await using var verify = Context();
    var record = await verify.EmailVerificationCodes.SingleAsync(code => code.Email == target);
    Check(decisions.All(value => !value), "Wrong code unexpectedly accepted.");
    Check(record.FailedAttempts == 5, $"Expected five reserved attempts; observed {record.FailedAttempts}.");
    Check(!await new VerificationCodeService(verify).VerifyAsync(target, "123456"), "Exhausted code was accepted.");
    return new { requests = decisions.Length, failedAttempts = record.FailedAttempts, correctCodeAfterLimitRejected = true };
});

await Run("verification-correct-code-single-consumption", async () =>
{
    var target = Id("consume-") + "@store.example";
    await using (var seed = Context())
    {
        seed.EmailVerificationCodes.Add(new EmailVerificationCode { Email = target, Code = "123456", ExpiresAt = DateTime.UtcNow.AddMinutes(10) });
        await seed.SaveChangesAsync();
    }
    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var tasks = Enumerable.Range(0, 8).Select(async _ =>
    {
        await using var request = Context();
        await gate.Task;
        return await new VerificationCodeService(request).VerifyAsync(target, "123456");
    }).ToArray();
    gate.SetResult();
    var decisions = await Task.WhenAll(tasks);
    await using var verify = Context();
    Check(decisions.Count(value => value) == 1, $"Expected exactly one successful verification; observed {decisions.Count(value => value)}.");
    Check(!await verify.EmailVerificationCodes.AnyAsync(code => code.Email == target), "Consumed code still exists.");
    return new { requests = decisions.Length, accepted = decisions.Count(value => value), codeRemoved = true };
});

await Run("product-crud-filtering-pagination", async () =>
{
    var label = Id("QA-Catalog-");
    await using var context = Context();
    var shop = new Shop { Name = Id("QA catalog shop ") };
    var category = new Category { Name = Id("QA category ") };
    context.AddRange(shop, category);
    await context.SaveChangesAsync();
    var service = new ProductService(context);
    var created = new List<Product>();
    for (var index = 1; index <= 3; index++)
    {
        var product = new Product { Name = label + index, Price = 10m * index, Stock = 4, ShopId = shop.Id, Categories = new List<Category> { category } };
        await service.CreateAsync(product);
        created.Add(product);
    }
    var page = await service.GetPagedAsync(new ProductQuery { SearchTerm = label, CategoryId = category.Id, MinPrice = 15m, Page = 2, PageSize = 1, SortBy = "price_asc" });
    Check(page.TotalItems == 2 && page.Items.Count == 1 && page.Items[0].Price == 30m, "Filtered second page incorrect.");
    var edited = created[0];
    edited.Name += "-edited";
    await service.UpdateAsync(edited);
    Check((await service.GetByIdAsync(edited.Id))!.Name.EndsWith("-edited"), "Product update not saved.");
    await service.DeleteAsync(created[2].Id);
    Check(await service.GetByIdAsync(created[2].Id) is null, "Product deletion failed.");
    return new { created = created.Count, filteredTotal = page.TotalItems, pagination = true, updated = true, deleted = true };
});

await Run("order-stock-cancellation-refund-restored-once", async () =>
{
    var product = await AddProduct("QA-Inventory-");
    var order = OrderFor(product, 2);
    await using (var context = Context()) { await new OrderService(context).CreateAsync(order); }
    await using (var context = Context()) { Check((await context.Products.FindAsync(product.Id))!.Stock == 8, "Checkout stock deduction incorrect."); }
    await using (var context = Context()) { Check(await new OrderService(context).CancelAsync(order.Id), "Cancellation failed."); }
    await using (var context = Context()) { Check(!await new OrderService(context).CancelAsync(order.Id), "Second cancellation succeeded."); }
    await using (var context = Context()) { Check((await context.Products.FindAsync(product.Id))!.Stock == 10, "Cancellation stock restored incorrectly."); }
    var refundOrder = OrderFor(product, 3);
    await using (var context = Context())
    {
        await new OrderService(context).CreateAsync(refundOrder);
        refundOrder.Status = OrderStatus.Completed;
        await context.SaveChangesAsync();
    }
    await using (var context = Context()) { Check(await new OrderService(context).RefundAsync(refundOrder.Id), "Refund failed."); }
    await using (var context = Context()) { Check(!await new OrderService(context).RefundAsync(refundOrder.Id), "Second refund succeeded."); }
    await using (var context = Context()) { Check((await context.Products.FindAsync(product.Id))!.Stock == 10, "Refund stock restored more than once."); }
    return new { initialStock = 10, afterCancellation = 10, afterRefund = 10, duplicateOperationsRejected = true };
});

await Run("competing-orders-cannot-oversell-last-item", async () =>
{
    var product = await AddProduct("QA-LastItem-", stock: 1);
    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var orders = new[] { OrderFor(product), OrderFor(product) };
    var tasks = orders.Select(async order =>
    {
        await using var context = Context();
        await gate.Task;
        try { await new OrderService(context).CreateAsync(order); return true; }
        catch (OrderValidationException) { return false; }
    }).ToArray();
    gate.SetResult();
    var accepted = await Task.WhenAll(tasks);
    await using var verify = Context();
    var finalStock = (await verify.Products.FindAsync(product.Id))!.Stock;
    var persisted = await verify.OrderItems.CountAsync(item => item.ProductId == product.Id);
    Check(accepted.Count(value => value) == 1 && finalStock == 0 && persisted == 1, "Competing checkout oversold or lost the last stock item.");
    return new { accepted = accepted.Count(value => value), finalStock, orderItems = persisted };
});

await Run("long-multilingual-shipping-snapshot-persists-in-sql", async () =>
{
    var first = await AddProduct("QA-Shipping-A-");
    var second = await AddProduct("QA-Shipping-B-");
    await using (var context = Context())
    {
        foreach (var pair in new[] { (first.Id, '甲', 'A'), (second.Id, '乙', 'B') })
        {
            var product = (await context.Products.FindAsync(pair.Id))!;
            product.ShippingFrom = new string(pair.Item2, 100); product.ShippingFromEn = new string(pair.Item3, 100);
            product.ShippingFee = new string(pair.Item2, 50); product.ShippingFeeEn = new string(pair.Item3, 50);
            product.DeliveryTime = "24" + new string(pair.Item2, 98); product.DeliveryTimeEn = "24" + new string(pair.Item3, 98);
        }
        await context.SaveChangesAsync();
    }
    var order = OrderFor(first);
    order.Items.Add(OrderFor(second).Items.Single());
    await using (var context = Context()) { await new OrderService(context).CreateAsync(order); }
    await using var verify = Context();
    var saved = (await verify.Orders.FindAsync(order.Id))!;
    var lengths = new Dictionary<string, int>();
    foreach (var name in new[] { "ShippingOrigin", "ShippingOriginEn", "ShippingFeeText", "ShippingFeeTextEn", "DispatchTimeText", "DispatchTimeTextEn" })
    {
        var property = typeof(Order).GetProperty(name)!;
        var value = (string)property.GetValue(saved)!;
        var maximum = property.GetCustomAttribute<StringLengthAttribute>()!.MaximumLength;
        Check(value.Length <= maximum && value.EndsWith('…'), $"Invalid persisted {name} snapshot.");
        lengths[name] = value.Length;
    }
    return new { persisted = true, lengths };
});

Complete:
;
}
catch (Exception exception)
{
    failures++;
    setupFailure = SafeError(exception);
    Console.Error.WriteLine($"SETUP FAIL: {setupFailure}");
}
var report = new
{
    schemaVersion = 1,
    provider = "Microsoft.EntityFrameworkCore.SqlServer",
    startedAtUtc,
    completedAtUtc = DateTime.UtcNow,
    expectedChecks = 9,
    completedChecks = results.Count,
    failures,
    setupFailure,
    passed = failures == 0 && results.Count == 9,
    databasePolicy = "New generated databases only; master is used for creation; no application database is read or written; no automatic drop.",
    databases,
    results
};
await File.WriteAllTextAsync(Path.Combine(output, "sql-verification.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"SQL verification: {results.Count} of 9 checks completed, {failures} failures. Report: {Path.Combine(output, "sql-verification.json")}");
Console.WriteLine("Generated databases are retained on E: for inspection. Use the report to identify them before manual cleanup.");
Environment.ExitCode = report.passed ? 0 : 1;

sealed class TestEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "OnlineStore.SqlChecks";
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
