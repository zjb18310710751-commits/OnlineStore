# Verification and evidence

This repository has two verification levels: repeatable CI checks without a database, and explicitly invoked local checks against real SQL Server. Passing one level does not establish that the other passed.

## CI and routine regression checks

`.github/workflows/ci.yml` runs on pushes and pull requests targeting `Ethan-Yang`, `HUZHANHAO`, and `main`, and can be started manually. Its separate jobs build and test .NET 8 and run the frontend Node test suite. It compiles the SQL verification executable and runs its safety guards without opening SQL connections. No SQL credentials, Azure credentials, deployment, or external database are required. The existing Azure deployment workflow remains separate.

Equivalent commands from the repository root:

```powershell
dotnet restore OnlineStore.sln
dotnet build OnlineStore.sln --configuration Release --no-restore
dotnet test OnlineStore.sln --configuration Release --no-build --no-restore --logger "trx;LogFileName=dotnet-tests.trx" --results-directory .artifacts/verification/dotnet
node --test OnlineStore.Tests/Frontend/*.test.cjs
dotnet build tools/verification/SqlChecks/SqlChecks.csproj --configuration Release
dotnet run --project tools/verification/SqlChecks/SqlChecks.csproj --configuration Release --no-build --no-restore -- --safety-checks
```

CI uploads .NET TRX and frontend TAP reports, including when tests fail. The frontend pipeline uses Bash `pipefail`, so a failing Node test still fails its job. Workflow configuration is not evidence of a completed GitHub Actions run; only an actual run can establish the remote result.

## Real SQL Server verification

The source is in `tools/verification/SqlChecks`; `tools/verification/run-sql-checks.ps1` is the normal entry point. It reuses the eight SQL regression scenarios from the 2026-10-29 coursework audit and adds an independent old-schema migration upgrade scenario.

Prerequisites:

- Windows, a checkout on E:, .NET 8 SDK, and a running **local** SQL Server instance. This entry point deliberately does not support a remote server or a SQL Server Linux container.
- A SQL login with permission to create verification databases and their schema. Windows integrated authentication is supported. The connection must explicitly name `master`; the runner never reads `appsettings`, user secrets, or the application connection variable.
- An existing directory on E: that the SQL Server service account can write. The runner does not change folder ACLs or use SQL Server's default data directory. Create this dedicated directory and grant only the relevant SQL service account access through normal local administration before running.

Example for a local SQL Express instance using Windows authentication; replace the data directory with your prepared directory:

```powershell
$env:ONLINESTORE_SQL_TEST_CONNECTION = 'Server=localhost\SQLEXPRESS;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;'
$env:ONLINESTORE_SQL_TEST_DATA_DIRECTORY = 'E:\OnlineStoreVerification\sql-data'
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verification\run-sql-checks.ps1
```

`TrustServerCertificate=True` in this example is for a local development instance using its self-signed certificate. Configure certificate trust appropriately if your local instance already has a trusted certificate. If SQL authentication is required, supply the connection through the process environment without committing it or putting a password into documentation or reports.

The wrapper builds by default. After explicitly building the tool, `-NoBuild` skips restore/build. `-OutputDirectory 'E:\...\new-run'` selects a new or empty E: evidence directory; otherwise a unique directory is created under `.artifacts/verification/`. The wrapper places its SDK state, downloaded packages, and temporary files on E: and restores the process environment afterwards.

### Isolation and retention

The executable generates two new names of the form `OnlineStore_Verification_<UTC date>_<UTC time>_<32-character GUID>`. Callers cannot supply a database name. Existing names are refused. The only master-database operations are checking whether the generated name exists and creating it. All EF migrations, seeding, and service writes use the generated connection; no daily application database, including `OnlineStore`, is read or written.

The connection guard rejects a missing or non-`master` catalog, remote host, failover partner, attached database file, SQL user instance, and read-only intent. Live execution rejects non-Windows hosts and non-E: data/output paths. Database creation names explicit E: `.mdf` and `.ldf` files; it cannot silently fall back to a C: default data directory. SQL credentials and complete connection strings are never written to the reports. Unexpected exception details are deliberately omitted because SQL errors can include sensitive connection values.

The runner **retains** generated databases for inspection on success or failure and performs no automatic drop. Its JSON lists every attempted database name and file path; a failed setup may therefore list a database whose creation did not finish. After reviewing the report, use SSMS to remove only the exact generated verification databases that you no longer need. Do not reuse the daily application database as a verification target. Each subsequent run creates fresh databases.

### Scenarios and expected observations

| Scenario | Evidence required for a pass |
| --- | --- |
| Fresh database and all migrations | EF migration history equals all repository migrations in order; `FailedAttempts` exists and is non-nullable. |
| Upgrade from old migration | A separate database stops at `20261028231500_AddReturnShipmentTracking`; it has no `FailedAttempts` column; a row inserted in that old schema survives upgrade with the default `0` and can be consumed once. |
| Development seeding | Products, the development administrator, and demo buyer exist in the isolated database. |
| Concurrent wrong verification codes | Twelve requests all fail; persisted attempts reach exactly five; the correct code is rejected after exhaustion. |
| Concurrent correct verification code | Of eight requests exactly one succeeds, and the consumed code no longer exists. |
| Product CRUD, filtering and paging | Three products are created; combined filtering returns the expected second page; updates and deletion persist. |
| Order stock, cancellation and refund | Checkout deducts stock; first cancellation/refund restores it; repeated operations are rejected without further stock changes. |
| Competing orders for one item | Exactly one order succeeds; stock becomes zero; exactly one associated order item persists. |
| Long bilingual shipping snapshots | Six bounded snapshot fields persist in SQL at or below their model limits and end with the truncation marker. |

These calls use the SQL Server EF provider and the actual application services, so they exercise relational persistence, constraints, concurrency handling, and service transaction paths. They do not substitute EF InMemory behavior for SQL Server. They are focused regressions, not sustained-load benchmarks or exhaustive transaction-failure injection tests.

## Reading and recording the evidence

A SQL run writes `sql-verification.json` and `sql-verification.log` to its artifact directory. JSON includes UTC times, provider, expected/completed check counts, each result and duration, generated database names/paths, setup failure status, and an overall `passed` flag. Exit code `0` requires all nine checks to complete with zero failures. Missing prerequisites, setup failure, test failure, or an incomplete run must not be described as a pass. A missing JSON report also causes the wrapper to fail.

The `--safety-checks` mode exercises accepted and rejected connection/path inputs and generated database names without reading connection environment values, creating files, or connecting to SQL. It checks the runner's isolation boundaries; it does **not** prove that SQL Server is available or that the live scenarios pass.

Keep raw run artifacts locally under E: and use commit identity plus report paths when recording an observed result. Retained development databases contain test users and fixture data, not production verification evidence. A result from before a code change is historical evidence and must not be presented as verification of the changed code.

| Quality concern | What these checks contribute | Limit |
| --- | --- | --- |
| Availability | SQL persistence invariants complement the separate HTTP outage/recovery exercise. | SQL scenario duration is not a service availability or recovery SLA. |
| Modifiability | Shipping policy changes retain the order persistence and stock regression scenarios. | New charging inputs still require contract and integration changes. |
| Testability | Shared repository commands, CI, and machine-readable evidence make regressions repeatable. | A configured workflow still needs an actual remote run; CI does not run live SQL. |
| Usability | Node tests check selection, validation focus, and failure/recovery feedback. | Controlled DOM tests do not replace real browser or participant testing. |
| Security | Attempt limits and verification-tool database isolation have executable checks. | Does not establish a complete application security audit. |
