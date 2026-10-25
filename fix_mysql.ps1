

$ErrorActionPreference = 'Continue'
$mysqldName = 'mysqld'
$serviceName = 'MySQL80'
$port = 3306
$taskkill = Join-Path $env:SystemRoot 'System32\taskkill.exe'

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[错误] 请以管理员身份运行本脚本（右键 PowerShell -> 以管理员身份运行）。" -ForegroundColor Red
    exit 1
}

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class SeDbg {
    [StructLayout(LayoutKind.Sequential)] public struct LUID { public uint Low; public int High; }
    [StructLayout(LayoutKind.Sequential, Pack=1)] public struct LUID_AND_ATTRIBUTES { public LUID Luid; public uint Attributes; }
    [StructLayout(LayoutKind.Sequential, Pack=1)] public struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public LUID_AND_ATTRIBUTES Privileges; }
    [DllImport("advapi32.dll", SetLastError=true)] public static extern bool OpenProcessToken(IntPtr h, uint a, out IntPtr t);
    [DllImport("advapi32.dll", SetLastError=true)] public static extern bool LookupPrivilegeValue(string s, string n, out LUID l);
    [DllImport("advapi32.dll", SetLastError=true)] public static extern bool AdjustTokenPrivileges(IntPtr t, bool d, ref TOKEN_PRIVILEGES p, int len, IntPtr prev, IntPtr rl);
    [DllImport("kernel32.dll")] public static extern IntPtr GetCurrentProcess();
    public static void Enable() {
        IntPtr t;
        OpenProcessToken(GetCurrentProcess(), 0x28, out t);
        LUID l;
        LookupPrivilegeValue(null, "SeDebugPrivilege", out l);
        TOKEN_PRIVILEGES p = new TOKEN_PRIVILEGES();
        p.PrivilegeCount = 1; p.Privileges.Luid = l; p.Privileges.Attributes = 0x2;
        AdjustTokenPrivileges(t, false, ref p, 0, IntPtr.Zero, IntPtr.Zero);
    }
}
"@
[SeDbg]::Enable()

Write-Host "==> 确保服务 $serviceName 已停止 ..." -ForegroundColor Cyan
$svc = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($svc -and $svc.Status -ne 'Stopped') {
    try { Stop-Service -Name $serviceName -Force -ErrorAction Stop; Write-Host "    已停止服务。" -ForegroundColor Green }
    catch { Write-Host "    停止服务失败: $($_.Exception.Message)" -ForegroundColor Yellow }
} else {
    Write-Host "    服务已处于停止状态。" -ForegroundColor Green
}

Write-Host "==> 清理孤儿 mysqld 进程 ..." -ForegroundColor Cyan
$deadline = (Get-Date).AddSeconds(60)
do {
    $procs = Get-Process -Name $mysqldName -ErrorAction SilentlyContinue
    if (-not $procs) { break }
    & $taskkill /F /T /IM "$mysqldName.exe" 2>&1 | Out-Null
    Start-Sleep -Seconds 2
} while ((Get-Date) -lt $deadline)

$left = Get-Process -Name $mysqldName -ErrorAction SilentlyContinue
if ($left) {
    Write-Host "[错误] 仍有残留 mysqld 进程: $($left.Id -join ',')" -ForegroundColor Red
    Write-Host "       若为『Access is denied』，可能是安全软件保护了该进程，建议重启电脑后再运行本脚本。" -ForegroundColor Yellow
    exit 1
}
Write-Host "    孤儿进程已全部清除。" -ForegroundColor Green

Write-Host "==> 启动服务 $serviceName ..." -ForegroundColor Cyan
try {
    Start-Service -Name $serviceName -ErrorAction Stop
} catch {
    Write-Host "[错误] 启动服务失败: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "       请查看错误日志: C:\ProgramData\MySQL\MySQL Server 8.0\Data\mysql.err" -ForegroundColor Yellow
    exit 1
}

$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    $svc = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if ($svc -and $svc.Status -eq 'Running') { $ready = $true; break }
}
if (-not $ready) {
    Write-Host "[错误] 服务未能在 30 秒内进入 Running。" -ForegroundColor Red
    Write-Host "       请查看错误日志: C:\ProgramData\MySQL\MySQL Server 8.0\Data\mysql.err" -ForegroundColor Yellow
    exit 1
}
Write-Host "    服务已进入 Running。" -ForegroundColor Green

Start-Sleep -Seconds 2
$listen = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
if ($listen) {
    Write-Host "[成功] 端口 $port 已监听，MySQL 可用。" -ForegroundColor Green
} else {
    Write-Host "[警告] 端口 $port 仍未监听，请检查错误日志。" -ForegroundColor Yellow
}

Write-Host "`n完成。现在可在 OnlineStore 目录运行: dotnet run" -ForegroundColor Cyan
