<#
.SYNOPSIS
    Launcher script for NEXUS OS.
.DESCRIPTION
    Runs the published or debug version of NEXUS OS as a desktop application on Windows.
#>

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$PublishExe = Join-Path $ScriptDir "publish\NexusOS.Shell.exe"
$DebugExe = Join-Path $ScriptDir "src\NexusOS.Shell\bin\Debug\net10.0-windows\NexusOS.Shell.exe"

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "  LAUNCHING NEXUS OS                                 " -ForegroundColor Cyan
Write-Host "  Desktop Operating System for Windows               " -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

if (Test-Path $PublishExe) {
    Write-Host "[+] Launching Release Build: $PublishExe" -ForegroundColor Green
    Start-Process -FilePath $PublishExe
} elseif (Test-Path $DebugExe) {
    Write-Host "[+] Launching Debug Build: $DebugExe" -ForegroundColor Yellow
    Start-Process -FilePath $DebugExe
} else {
    Write-Host "[*] Binary not found. Building solution..." -ForegroundColor Yellow
    dotnet build (Join-Path $ScriptDir "NexusOS.slnx") -c Release -o (Join-Path $ScriptDir "publish")
    if (Test-Path $PublishExe) {
        Write-Host "[+] Build complete. Launching NEXUS OS..." -ForegroundColor Green
        Start-Process -FilePath $PublishExe
    } else {
        Write-Error "Failed to build NEXUS OS executable."
    }
}
