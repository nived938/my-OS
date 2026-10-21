# NEXUS OS: System Architecture Document

## 1. Overview

NEXUS OS is architected as a modular, layered desktop environment running natively on Windows 10/11 using modern .NET 10 and WPF. It bridges the familiarity of a Windows-style operating system with a sandboxed, customizable desktop layer.

---

## 2. Layered Architecture

```
+-------------------------------------------------------------------+
|                        NEXUS OS SHELL                             |
|  - MainWindow (Desktop Canvas, Wallpaper Engine, Host Controls)   |
|  - Window Manager Canvas & Snap Preview Overlay                   |
|  - Taskbar, Start Menu, Quick Settings, Action Center Flyouts     |
+-------------------------------------------------------------------+
                                |
                                v
+-------------------------------------------------------------------+
|                        NEXUS APPS SUITE                           |
|  - Nexus Explorer (Multi-tab, OLE Drag & Drop, Views)             |
|  - Nexus Navigator (WebView2 Chromium, Download Manager)          |
|  - Nexus App Store (Catalog, Package State Tracker)               |
|  - Nexus Settings (Personalization, Themes, Storage, Security)    |
|  - Nexus Arcade (Minesweeper, Snake, Cyber Runner, Sudoku)        |
|  - Nexus Terminal (Shell commands, Safe Host PowerShell)          |
|  - Nexus Widgets (CPU/RAM meters, Notes, Rainmeter Bridge)        |
|  - Nexus Virtualization (Hyper-V / QEMU Guest Manager)            |
+-------------------------------------------------------------------+
                                |
                                v
+-------------------------------------------------------------------+
|                         NEXUS OS UI                               |
|  - WindowHostControl (Dragging, 8-way resize, snapping)           |
|  - TaskbarControl, StartMenuControl, QuickSettingsFlyout          |
|  - AltTabOverlay (Keyboard cycling, active focus)                 |
|  - Themes (Colors, Brushes, Geometries, Typography)              |
+-------------------------------------------------------------------+
                                |
                                v
+-------------------------------------------------------------------+
|                   NEXUS INFRASTRUCTURE SERVICES                   |
|  - VirtualStorageService (Sandboxed workspace, Host import)       |
|  - SecurityService (Authenticode, SHA-256, Audit logs)            |
|  - HostExecutionBridge (Safe process execution & monitoring)      |
|  - PackageManager (Curated catalog, manifest verification)        |
|  - SettingsService (Atomic JSON & SQLite persistence)             |
|  - VirtualizationService (Hypervisor probing & VM specs)          |
|  - RainmeterBridge (Host desktop interop guide & launcher)        |
+-------------------------------------------------------------------+
                                |
                                v
+-------------------------------------------------------------------+
|                         NEXUS OS CORE                             |
|  - Contracts & Interfaces (IWindowManager, IAppRegistry, etc.)    |
|  - Models (WindowInstance, PackageManifest, NexusSettings)        |
|  - Event Aggregator (Typed in-memory message bus)                 |
+-------------------------------------------------------------------+
```

---

## 3. Core Subsystems

### 3.1 Window Manager (`NexusOS.Core.Windowing`)
- **State Machine**: Supports `Normal`, `Minimized`, `Maximized`, `SnappedLeft`, `SnappedRight`, and corner snaps.
- **Bound Preservation**: When maximized or snapped, normal window coordinates (`_savedLeft`, `_savedTop`, `_savedWidth`, `_savedHeight`) are preserved. Restoring returns the window to its exact previous coordinates.
- **Z-Order Management**: Increments an internal Z-index counter whenever a window is clicked or activated, dynamically updating WPF `Panel.ZIndex`.
- **Snapping**: Detects window release within threshold distance (5px) of canvas edges and applies half/quarter screen layouts.

### 3.2 Virtual Storage & Windows Drag-and-Drop (`NexusOS.Infrastructure.Storage`)
- **Isolation**: Default storage is rooted at `%USERPROFILE%\NexusOS\Workspace`. Standard user folders (`Desktop`, `Documents`, `Downloads`, `Pictures`, `Music`, `Videos`) are mapped inside this root.
- **Host Drag-and-Drop**: WPF `AllowDrop="True"` events intercept `DataFormats.FileDrop` payloads. `ImportFromHostAsync` streams bytes from the host filesystem into the virtual workspace using chunks, computing file sizes, updating UI progress, and refreshing directory indexes.

### 3.3 Security & Execution Pipeline (`NexusOS.Infrastructure.Security`)
- **Authenticode Verification**: Uses `X509Certificate2` to inspect the embedded PKCS #7 signature of `.exe` and `.msi` files, validating signer subject, issuer, and certificate validity period.
- **Hash Integrity**: Automatically computes SHA-256 hashes of all downloaded binaries.
- **Execution Workflow**: Downloaded executables are never executed automatically. Nexus OS presents a security verification modal with publisher and risk ratings (`VerifiedSafe`, `CautionUnsigned`, `Suspicious`). Launching on the host requires explicit user confirmation and relies on standard Windows User Account Control (UAC).
