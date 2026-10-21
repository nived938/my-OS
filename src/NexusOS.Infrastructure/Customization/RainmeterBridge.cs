using System.Diagnostics;

namespace NexusOS.Infrastructure.Customization;

public class RainmeterStatus
{
    public bool IsInstalledOnHost { get; set; }
    public string ExecutablePath { get; set; } = string.Empty;
    public string SkinsDirectory { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
    public string ArchitectureExplanation { get; set; } = string.Empty;
}

public class RainmeterBridge
{
    public RainmeterStatus CheckRainmeterStatus()
    {
        var status = new RainmeterStatus();

        var possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rainmeter", "Rainmeter.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Rainmeter", "Rainmeter.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Rainmeter", "Rainmeter.exe")
        };

        foreach (var p in possiblePaths)
        {
            if (File.Exists(p))
            {
                status.IsInstalledOnHost = true;
                status.ExecutablePath = p;
                break;
            }
        }

        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var skinsDir = Path.Combine(docs, "Rainmeter", "Skins");
        if (Directory.Exists(skinsDir))
        {
            status.SkinsDirectory = skinsDir;
        }

        status.IsRunning = Process.GetProcessesByName("Rainmeter").Length > 0;

        status.ArchitectureExplanation = 
            "Rainmeter Architecture & Nexus OS Integration:\n\n" +
            "1. Real Windows Desktop: Native Rainmeter hooks into the Windows host desktop window (Progman/WorkerW) via Direct2D.\n" +
            "2. Nexus OS Desktop: Nexus OS runs as an isolated desktop environment inside a high-DPI WPF window. Native Rainmeter skins cannot render directly into a child application window without skin parsing or desktop layering.\n" +
            "3. Nexus Widget Engine: Nexus OS provides its own native widget engine (CPU, RAM, Clock, Storage, Notes) with direct hardware acceleration, sandboxed security, and draggable layouts.\n" +
            "4. Host Bridge: Nexus OS can launch or restart Rainmeter on your host desktop upon request.";

        return status;
    }

    public bool LaunchHostRainmeter()
    {
        var status = CheckRainmeterStatus();
        if (!status.IsInstalledOnHost)
            return false;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = status.ExecutablePath,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
