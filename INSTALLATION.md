# NEXUS OS Installation Guide

NEXUS OS does not alter your Windows Master Boot Record (MBR), UEFI partitions, or bootloader. It is designed to run directly as a high-performance Windows desktop application.

---

## Installation Steps

### Option 1: Standalone Portable Installation (Recommended)
1. Extract or clone NEXUS OS into any directory on your PC (e.g. `C:\NexusOS` or `G:\Coding\.My-OS`).
2. Run `publish\NexusOS.Shell.exe` or double-click `NexusOS.bat`.
3. To add a shortcut to your physical Windows desktop:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\CreateDesktopShortcut.ps1
   ```

### Option 2: Building and Packaging
1. Ensure the .NET 10 or .NET 8 SDK is installed (`dotnet --version`).
2. Run `dotnet publish src\NexusOS.Shell\NexusOS.Shell.csproj -c Release -o publish`.
3. The release binary `publish\NexusOS.Shell.exe` is completely self-contained and ready to execute.

---

## Directory Locations & User Data

- **Settings & Config**: `%USERPROFILE%\NexusOS\settings.json`
- **Application Workspace**: `%USERPROFILE%\NexusOS\Workspace\`
- **Installed Packages**: `%USERPROFILE%\NexusOS\installed_packages.json`
- **Security Audit Logs**: `%USERPROFILE%\NexusOS\Logs\security_audit.log`
- **Browser Profiles**: `%USERPROFILE%\NexusOS\Workspace\.browser_data\`

---

## Uninstallation

To uninstall NEXUS OS:
1. Delete the application folder.
2. If you wish to delete your saved workspace files, remove `%USERPROFILE%\NexusOS\`.
3. Delete the desktop shortcut from your Windows Desktop.
No registry keys or driver hooks remain on your system.
