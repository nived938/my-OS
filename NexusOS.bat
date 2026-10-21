@echo off
setlocal
cd /d "%~dp0"

echo ====================================================
echo   LAUNCHING NEXUS OS
echo   Desktop Operating System for Windows
echo ====================================================

if exist "publish\NexusOS.Shell.exe" (
    start "" "publish\NexusOS.Shell.exe"
    exit /b 0
)

if exist "src\NexusOS.Shell\bin\Debug\net10.0-windows\NexusOS.Shell.exe" (
    start "" "src\NexusOS.Shell\bin\Debug\net10.0-windows\NexusOS.Shell.exe"
    exit /b 0
)

echo Compiling NEXUS OS...
dotnet publish src\NexusOS.Shell\NexusOS.Shell.csproj -c Release -o publish
if exist "publish\NexusOS.Shell.exe" (
    start "" "publish\NexusOS.Shell.exe"
) else (
    echo [ERROR] Failed to compile NEXUS OS. Please ensure .NET 10 or .NET 8 SDK is installed.
    pause
)
