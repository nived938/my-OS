# NEXUS OS

**A Complete, Customizable, Windows-Like Desktop Operating System That Runs Inside Windows**

NEXUS OS is a full desktop operating system environment engineered in modern C# (.NET 10) and Windows Presentation Foundation (WPF). It runs directly on top of your existing Windows installation without requiring dual-booting, partition alterations, or BIOS/UEFI changes.

---

## Highlights & Capabilities

- **Desktop Shell & Window Manager**:
  - Movable, resizable (8-directional edge/corner resizing), minimizable, maximizable, and closable windows.
  - Windows 11 style snap layouts (Snap Left, Snap Right, Corners) with live translucent snap overlay previews.
  - Alt+Tab application switcher overlay with live window previews.
  - Smooth DoubleAnimations with easing and configurable Reduced Motion mode.
  - Window Z-ordering, focus management, active window glow, and customizable title bars.

- **Taskbar, Start Menu & System Tray**:
  - Centered or left-aligned taskbar with pinned and running app pills.
  - Windows 11 style Start Menu with pinned apps, recommended recent items, search filter, user profile, and power options.
  - System tray with live time and date, network connectivity indicator, battery monitor via native Win32 `GetSystemPowerStatus`, volume control, and Action Center flyout.
  - Quick Settings flyout: Wi-Fi, Bluetooth, Night Light, Focus / DND, Eco Mode, Battery Saver, volume and brightness sliders.
  - Safe host power workflow: Explicit confirmation required before shutting down the host Windows PC.

- **Nexus Explorer & Native Host Drag-and-Drop**:
  - Sandboxed workspace under `%USERPROFILE%\NexusOS\Workspace`.
  - Multi-tab file manager with address bar, search, folder creation, deletion, renaming, and Grid / Details views.
  - **Direct Drag-and-Drop from Windows**: Drag any file or folder from real Windows File Explorer or Desktop directly into Nexus Explorer or onto the Nexus Desktop canvas to automatically import it with real progress and size validation.

- **Nexus Navigator (Web Browser)**:
  - Tabbed web browser powered by Microsoft WebView2 (Chromium engine) with offline fallback.
  - URL and web search bar, bookmarks, history, and downloads manager.
  - Safe Executable Workflow: When `.exe` files are downloaded, Nexus OS verifies their Authenticode digital signatures, SHA-256 hashes, and prompts for explicit user confirmation before host execution.

- **Nexus App Store & Local Package Manager**:
  - Curated software catalog covering System Tools, Development, Games, Productivity, Utilities, and Customization.
  - Real installation progress, version tracking, dependency verification, and uninstall support.

- **Built-in Games Suite**:
  - **Nexus Minesweeper**: Classic 9x9 grid, mine count, timer, flagging, smiley face reset, cascade reveals.
  - **Cyber Snake**: Retro arcade snake with keyboard controls (Arrow keys / WASD), collision physics, food powerups, score tracking.
  - **2D Cyber Runner**: Real-time physics platformer with jump mechanics, obstacles, and distance scoring.
  - **Nexus Sudoku**: Interactive 9x9 board with digit keypad, conflict highlighting, and new game generator.

- **Widgets & Rainmeter Integration**:
  - Draggable desktop widgets: Live CPU & RAM monitor, Clock & Calendar, Quick Sticky Notes.
  - Rainmeter bridge explaining the technical boundaries between the host desktop (Progman/WorkerW), Nexus OS child windows, and virtual machines. Allows launching Rainmeter on the host.

- **Virtualization Manager (Mode B)**:
  - Hyper-V and QEMU detection and configuration.
  - VM manager interface for RAM, CPU cores, virtual disks, and guest ISO configuration.

---

## Quick Launch

### 1. From Windows Explorer
Simply double-click `NexusOS.bat` in this directory.

### 2. From PowerShell
```powershell
.\Launch-NexusOS.ps1
```

### 3. Create Desktop Shortcut
```powershell
powershell -ExecutionPolicy Bypass -File .\CreateDesktopShortcut.ps1
```

### 4. Build from Source
```powershell
dotnet build NexusOS.slnx
```

---

## Project Structure

```
├── src/
│   ├── NexusOS.Core/             # Subsystem contracts, windowing, settings, event aggregator
│   ├── NexusOS.Infrastructure/   # SQLite, Storage, Authenticode verification, Host Execution Bridge
│   ├── NexusOS.UI/               # Fluent design system, Window Chrome, Taskbar, Flyouts
│   ├── NexusOS.Apps/             # Explorer, Browser, Store, Settings, Arcade Games, Terminal, Widgets
│   └── NexusOS.Shell/            # Main WPF shell application & Desktop Canvas
├── tests/
│   └── NexusOS.Tests/            # Automated unit and integration test suite
├── publish/                      # Production Release binaries (NexusOS.Shell.exe)
├── Launch-NexusOS.ps1            # One-click PowerShell launcher
├── NexusOS.bat                   # Double-click Windows batch launcher
├── CreateDesktopShortcut.ps1     # Creates Windows desktop shortcut
└── docs/                         # Architecture, Security, Building, User Guides
```
