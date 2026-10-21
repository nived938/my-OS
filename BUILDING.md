# Building NEXUS OS from Source

## Prerequisites

1. **Operating System**: Windows 10 (version 1809 or higher) or Windows 11.
2. **.NET SDK**: .NET 10.0 SDK (or .NET 8.0 SDK with WindowsDesktop workload).
3. **Hardware**: Any modern x86-64 CPU, 4 GB RAM minimum (8 GB recommended), DirectX 9+ compatible GPU.

---

## Command Line Build Instructions

### 1. Clone or Open the Repository
```powershell
cd G:\Coding\.My-OS
```

### 2. Restore Dependencies
```powershell
dotnet restore NexusOS.slnx
```

### 3. Build the Solution (Debug Mode)
```powershell
dotnet build NexusOS.slnx
```

### 4. Run the Automated Test Suite
```powershell
dotnet test tests\NexusOS.Tests\NexusOS.Tests.csproj
```

### 5. Publish Release Binaries
```powershell
dotnet publish src\NexusOS.Shell\NexusOS.Shell.csproj -c Release -o publish
```

---

## Launching the Application

After building, launch NEXUS OS using any of the following:

- **Batch File**: Double click `NexusOS.bat`
- **PowerShell**: Run `.\Launch-NexusOS.ps1`
- **Executable**: Run `publish\NexusOS.Shell.exe`
