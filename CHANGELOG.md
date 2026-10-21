# Changelog

All notable changes to the NEXUS OS project will be documented in this file.

## [1.0.0] - 2026-10-21

### Added
- **Core Architecture**: Solution organization with `NexusOS.Core`, `NexusOS.Infrastructure`, `NexusOS.UI`, `NexusOS.Apps`, `NexusOS.Shell`, and `NexusOS.Tests`.
- **Window Manager**: Complete window host with dragging, 8-directional edge/corner resizing, minimize, maximize, restore, Alt+Tab overlay, and window snapping.
- **Taskbar & Shell**: Windows 11 style centered taskbar, Start Menu with search filter, system tray with clock and battery status, Quick Settings flyout, and Action Center.
- **Nexus Explorer**: Multi-tab sandboxed file explorer with OLE drag-and-drop support directly from host Windows File Explorer and Desktop.
- **Nexus Navigator**: Chromium-powered web browser with WebView2, tab management, bookmarks, and download security inspector.
- **Authenticode Security Pipeline**: Digital certificate verification for Windows executables, SHA-256 hash generation, and safe execution workflow.
- **Arcade Games**: Fully playable Minesweeper, Cyber Snake, 2D Cyber Runner, and Sudoku.
- **Widgets Platform**: Real-time CPU and RAM monitors, live clock, sticky notes, and Rainmeter compatibility bridge.
- **Nexus Settings**: Personalization (themes, accent colors, wallpaper scaling, corner radius, reduced motion), storage manager, security options, and system diagnostics.
- **Terminal**: Tabbed command console with built-in commands and safe host PowerShell execution.
- **Virtualization Engine**: Mode B hypervisor integration for Hyper-V and QEMU virtual machines.
- **Packaging & Launchers**: PowerShell launcher `Launch-NexusOS.ps1`, batch launcher `NexusOS.bat`, and desktop shortcut creator.
