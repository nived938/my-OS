$WshShell = New-Object -ComObject WScript.Shell
$DesktopPath = [System.Environment]::GetFolderPath('Desktop')
$ShortcutPath = Join-Path $DesktopPath "NEXUS OS.lnk"
$TargetExe = Join-Path $PSScriptRoot "publish\NexusOS.Shell.exe"

if (-not (Test-Path $TargetExe)) {
    $TargetExe = Join-Path $PSScriptRoot "NexusOS.bat"
}

$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = $TargetExe
$Shortcut.WorkingDirectory = $PSScriptRoot
$Shortcut.Description = "NEXUS OS - Modern Desktop Operating System"
$Shortcut.Save()

Write-Host "[+] Desktop shortcut successfully created: $ShortcutPath" -ForegroundColor Green
