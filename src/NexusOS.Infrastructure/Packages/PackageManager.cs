using System.Text.Json;
using NexusOS.Core.Packages;
using NexusOS.Core.Security;

namespace NexusOS.Infrastructure.Packages;

public class PackageManager : IPackageManager
{
    private readonly string _packagesStatePath;
    private readonly ISecurityService _securityService;
    private readonly List<PackageManifest> _installedPackages = new();

    public PackageManager(ISecurityService securityService, string? customDir = null)
    {
        _securityService = securityService;
        var dir = customDir ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NexusOS");
        _packagesStatePath = Path.Combine(dir, "installed_packages.json");
        LoadInstalledState();
    }

    public Task<List<PackageManifest>> GetCatalogAsync()
    {
        // Curated, legal, functional app catalog
        var catalog = new List<PackageManifest>
        {
            new PackageManifest
            {
                Id = "nexus.explorer",
                Name = "Nexus Explorer",
                Version = "1.0.0",
                Publisher = "Nexus System",
                Description = "Modern multi-tab file manager with Windows drag-and-drop workspace integration.",
                Category = "System",
                IconKey = "Folder",
                SizeMb = 4.2,
                InstallType = PackageInstallType.BuiltInNexusApp,
                IsInstalled = true
            },
            new PackageManifest
            {
                Id = "nexus.browser",
                Name = "Nexus Navigator",
                Version = "1.0.0",
                Publisher = "Nexus System",
                Description = "Fast Chromium-powered web browser with integrated download security inspector.",
                Category = "Internet",
                IconKey = "Globe",
                SizeMb = 12.5,
                InstallType = PackageInstallType.BuiltInNexusApp,
                IsInstalled = true
            },
            new PackageManifest
            {
                Id = "nexus.terminal",
                Name = "Nexus Terminal",
                Version = "1.0.0",
                Publisher = "Nexus System",
                Description = "Multi-tab terminal with built-in commands and safe PowerShell host bridge.",
                Category = "Development",
                IconKey = "Terminal",
                SizeMb = 2.8,
                InstallType = PackageInstallType.BuiltInNexusApp,
                IsInstalled = true
            },
            new PackageManifest
            {
                Id = "nexus.settings",
                Name = "Nexus Settings",
                Version = "1.0.0",
                Publisher = "Nexus System",
                Description = "Comprehensive system settings, personalizations, storage manager, and privacy controls.",
                Category = "System",
                IconKey = "Settings",
                SizeMb = 3.1,
                InstallType = PackageInstallType.BuiltInNexusApp,
                IsInstalled = true
            },
            new PackageManifest
            {
                Id = "nexus.games.minesweeper",
                Name = "Nexus Minesweeper",
                Version = "1.0.0",
                Publisher = "Nexus Entertainment",
                Description = "Classic logic puzzle game with custom grid sizes, difficulty settings, and high score tracking.",
                Category = "Games",
                IconKey = "Bomb",
                SizeMb = 1.5,
                InstallType = PackageInstallType.BuiltInNexusApp,
                IsInstalled = true
            },
            new PackageManifest
            {
                Id = "nexus.games.snake",
                Name = "Nexus Cyber Snake",
                Version = "1.0.0",
                Publisher = "Nexus Entertainment",
                Description = "Smooth retro arcade snake game with particle effects, score progression, and speed levels.",
                Category = "Games",
                IconKey = "Gamepad",
                SizeMb = 1.8,
                InstallType = PackageInstallType.BuiltInNexusApp,
                IsInstalled = true
            },
            new PackageManifest
            {
                Id = "nexus.games.solitaire",
                Name = "Nexus Solitaire",
                Version = "1.0.0",
                Publisher = "Nexus Entertainment",
                Description = "Classic Klondike Solitaire card game with smooth animations, draw 1/3, and undo support.",
                Category = "Games",
                IconKey = "Cards",
                SizeMb = 2.4,
                InstallType = PackageInstallType.BuiltInNexusApp,
                IsInstalled = true
            },
            new PackageManifest
            {
                Id = "nexus.games.sudoku",
                Name = "Nexus Sudoku",
                Version = "1.0.0",
                Publisher = "Nexus Entertainment",
                Description = "Playable number puzzle game with conflict highlights, pencil marks, and difficulty tiers.",
                Category = "Games",
                IconKey = "Grid",
                SizeMb = 1.6,
                InstallType = PackageInstallType.BuiltInNexusApp,
                IsInstalled = true
            },
            new PackageManifest
            {
                Id = "nexus.games.cyberrunner",
                Name = "Cyber Runner 2D",
                Version = "1.0.0",
                Publisher = "Nexus Entertainment",
                Description = "Fast-paced 2D sci-fi platformer with jumping, obstacle dodging, and energy collection.",
                Category = "Games",
                IconKey = "Rocket",
                SizeMb = 3.5,
                InstallType = PackageInstallType.BuiltInNexusApp,
                IsInstalled = true
            },
            new PackageManifest
            {
                Id = "nexus.widgets.sysmon",
                Name = "System Monitor Widget",
                Version = "1.2.0",
                Publisher = "Nexus Customization",
                Description = "Desktop live CPU, RAM, and Disk storage usage widget with sleek graphs.",
                Category = "Customization",
                IconKey = "Activity",
                SizeMb = 0.9,
                InstallType = PackageInstallType.DesktopWidget,
                IsInstalled = true
            },
            new PackageManifest
            {
                Id = "nexus.widgets.clockweather",
                Name = "Clock & Notes Widget",
                Version = "1.1.0",
                Publisher = "Nexus Customization",
                Description = "Minimalist desktop clock, calendar, and quick sticky notes.",
                Category = "Customization",
                IconKey = "Clock",
                SizeMb = 0.8,
                InstallType = PackageInstallType.DesktopWidget,
                IsInstalled = true
            },
            new PackageManifest
            {
                Id = "tools.vscode",
                Name = "Visual Studio Code (Host Shortcut)",
                Version = "Host Detection",
                Publisher = "Microsoft",
                Description = "Connects to host-installed Visual Studio Code for code editing from Nexus OS.",
                Category = "Development",
                IconKey = "Code",
                SizeMb = 0.1,
                InstallType = PackageInstallType.ManagedExecutable,
                ExecutableTarget = "code",
                IsInstalled = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft VS Code", "Code.exe"))
            },
            new PackageManifest
            {
                Id = "tools.notepad",
                Name = "Windows Notepad (Host Bridge)",
                Version = "Windows Built-in",
                Publisher = "Microsoft Corporation",
                Description = "Quick text editing bridge using Windows native Notepad.",
                Category = "Productivity",
                IconKey = "FileText",
                SizeMb = 0.1,
                InstallType = PackageInstallType.ManagedExecutable,
                ExecutableTarget = "notepad.exe",
                IsInstalled = true
            },
            new PackageManifest
            {
                Id = "tools.calculator",
                Name = "Windows Calculator (Host Bridge)",
                Version = "Windows Built-in",
                Publisher = "Microsoft Corporation",
                Description = "Host-integrated scientific and standard calculation utility.",
                Category = "Utilities",
                IconKey = "Calculator",
                SizeMb = 0.1,
                InstallType = PackageInstallType.ManagedExecutable,
                ExecutableTarget = "calc.exe",
                IsInstalled = true
            }
        };

        // Sync with persisted installed state
        foreach (var pkg in catalog)
        {
            if (_installedPackages.Any(p => p.Id == pkg.Id))
            {
                pkg.IsInstalled = true;
            }
        }

        return Task.FromResult(catalog);
    }

    public async Task<List<PackageManifest>> GetInstalledPackagesAsync()
    {
        var catalog = await GetCatalogAsync();
        var installedList = catalog.Where(c => c.IsInstalled).ToList();
        foreach (var p in _installedPackages)
        {
            if (!installedList.Any(c => c.Id == p.Id))
            {
                installedList.Add(p);
            }
        }
        return installedList;
    }

    public async Task<bool> InstallPackageAsync(PackageManifest package, IProgress<double>? progress = null)
    {
        // Simulate real progress steps
        for (int i = 1; i <= 5; i++)
        {
            await Task.Delay(100);
            progress?.Report(i / 5.0);
        }

        package.IsInstalled = true;
        if (!_installedPackages.Any(p => p.Id == package.Id))
        {
            _installedPackages.Add(package);
            SaveInstalledState();
        }

        _securityService.RecordAuditEvent("INSTALL_PACKAGE", package.Id, "Installed successfully");
        return true;
    }

    public Task<bool> UninstallPackageAsync(string packageId)
    {
        var existing = _installedPackages.FirstOrDefault(p => p.Id == packageId);
        if (existing != null)
        {
            _installedPackages.Remove(existing);
            SaveInstalledState();
            _securityService.RecordAuditEvent("UNINSTALL_PACKAGE", packageId, "Uninstalled successfully");
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public bool VerifyChecksum(string filePath, string expectedSha256)
    {
        if (string.IsNullOrWhiteSpace(expectedSha256) || !File.Exists(filePath))
            return false;

        var actual = _securityService.CalculateSha256(filePath);
        return string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase);
    }

    private void LoadInstalledState()
    {
        try
        {
            if (File.Exists(_packagesStatePath))
            {
                var json = File.ReadAllText(_packagesStatePath);
                var items = JsonSerializer.Deserialize<List<PackageManifest>>(json);
                if (items != null)
                {
                    _installedPackages.Clear();
                    _installedPackages.AddRange(items);
                }
            }
        }
        catch
        {
            // Fallback gracefully
        }
    }

    private void SaveInstalledState()
    {
        try
        {
            var json = JsonSerializer.Serialize(_installedPackages, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_packagesStatePath, json);
        }
        catch
        {
            // Ignore state save error
        }
    }
}
