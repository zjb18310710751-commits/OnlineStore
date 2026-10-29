[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if ($repositoryRoot -notmatch '^[eE]:[\\/]') {
    throw 'Run this local SQL verification tool from a checkout on E:.'
}
if ([string]::IsNullOrWhiteSpace($env:ONLINESTORE_SQL_TEST_CONNECTION)) {
    throw 'Set ONLINESTORE_SQL_TEST_CONNECTION to a local SQL Server master connection. Application settings are never used.'
}
if ([string]::IsNullOrWhiteSpace($env:ONLINESTORE_SQL_TEST_DATA_DIRECTORY)) {
    throw 'Set ONLINESTORE_SQL_TEST_DATA_DIRECTORY to an existing E: directory writable by the SQL Server service account.'
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $runId = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $OutputDirectory = Join-Path $repositoryRoot ".artifacts/verification/$runId"
}
if ($OutputDirectory -notmatch '^[eE]:[\\/]') {
    throw 'OutputDirectory must be an absolute E: path.'
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $OutputDirectory) -and @(Get-ChildItem -LiteralPath $OutputDirectory -Force).Count -gt 0) {
    throw 'Use a new or empty output directory so previous evidence is preserved.'
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

# Keep tool-owned SDK state, package downloads and temporary files on E:.
$runtimeRoot = Join-Path $repositoryRoot '.artifacts/verification-runtime'
$environmentNames = @('DOTNET_CLI_HOME', 'DOTNET_CLI_TELEMETRY_OPTOUT', 'NUGET_PACKAGES', 'TEMP', 'TMP')
$previousEnvironment = @{}
foreach ($name in $environmentNames) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}
$exitCode = 1
try {
    $env:DOTNET_CLI_HOME = Join-Path $runtimeRoot 'dotnet-home'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    if ([string]::IsNullOrWhiteSpace($env:NUGET_PACKAGES) -or $env:NUGET_PACKAGES -notmatch '^[eE]:[\\/]') {
        $env:NUGET_PACKAGES = Join-Path $runtimeRoot 'nuget-packages'
    }
    $env:TEMP = Join-Path $runtimeRoot 'temp'
    $env:TMP = $env:TEMP
    foreach ($directory in @($env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES, $env:TEMP)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $arguments = @('run', '--project', (Join-Path $PSScriptRoot 'SqlChecks/SqlChecks.csproj'), '--configuration', 'Release', '--no-launch-profile')
    if ($NoBuild) { $arguments += @('--no-build', '--no-restore') }
    $arguments += @('--', $OutputDirectory)
    $ErrorActionPreference = 'Continue'
    & dotnet @arguments 2>&1 | Tee-Object -FilePath (Join-Path $OutputDirectory 'sql-verification.log')
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    if ($exitCode -eq 0 -and -not (Test-Path -LiteralPath (Join-Path $OutputDirectory 'sql-verification.json'))) {
        throw 'The runner did not create its JSON report.'
    }
    Write-Output "Verification artifacts: $OutputDirectory"
}
finally {
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
    }
}
exit $exitCode
