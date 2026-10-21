# NEXUS OS Troubleshooting Guide

### 1. WebView2 Runtime Error
- **Symptom**: Nexus Navigator shows the fallback landing page instead of live web content.
- **Cause**: Microsoft Edge WebView2 Evergreen Runtime is not installed on the host.
- **Solution**: Install the free Microsoft WebView2 Runtime from [Microsoft's official website](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) or ensure Microsoft Edge is updated.

### 2. File Drag and Drop Does Not Trigger
- **Symptom**: Dragging a file from Windows File Explorer does not import into Nexus OS.
- **Cause**: Windows User Account Control (UAC) boundary prevents drag-and-drop between processes running at different privilege levels (e.g. Explorer running as standard user while Nexus OS is elevated, or vice-versa).
- **Solution**: Ensure NEXUS OS and Windows Explorer are both running at standard user privileges (do not run NEXUS OS as Administrator).

### 3. Hypervisor / Mode B Virtualization Unavailable
- **Symptom**: Nexus Virtualization shows "No active hypervisor detected".
- **Cause**: Windows Hyper-V optional feature or QEMU emulator is not installed on the host PC.
- **Solution**:
  - To enable Hyper-V: Open PowerShell as Administrator and run `Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All`.
  - Alternatively, install QEMU for Windows.

### 4. Custom Wallpaper Does Not Load
- **Symptom**: Selected wallpaper reverts to gradient.
- **Cause**: Image path is inaccessible, corrupted, or in an unsupported format.
- **Protection**: NEXUS OS includes zero-black-screen protection that preserves the fallback gradient if an image fails to load.
