using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusOS.Core.Packages;
using NexusOS.Infrastructure.Packages;
using NexusOS.Infrastructure.Security;
using NexusOS.Infrastructure.Services;

namespace NexusOS.Tests;

[TestClass]
public class SettingsAndPackageTests
{
    private string _testDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "NexusOS_SettingsPkgTests_" + Guid.NewGuid().ToString("N"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, recursive: true); } catch { }
        }
    }

    [TestMethod]
    public async Task SettingsService_SavesAndLoadsPersistently()
    {
        var service1 = new SettingsService(_testDir);
        await service1.LoadSettingsAsync();

        service1.Settings.AccentColor = "#10B981";
        service1.Settings.WindowCornerRadius = 14.0;
        service1.Settings.ReducedMotion = true;
        await service1.SaveSettingsAsync();

        // Fresh instance reading same directory
        var service2 = new SettingsService(_testDir);
        await service2.LoadSettingsAsync();

        Assert.AreEqual("#10B981", service2.Settings.AccentColor);
        Assert.AreEqual(14.0, service2.Settings.WindowCornerRadius);
        Assert.IsTrue(service2.Settings.ReducedMotion);
    }

    [TestMethod]
    public async Task SettingsService_RecoversGracefullyFromCorruptJson()
    {
        Directory.CreateDirectory(_testDir);
        var settingsFile = Path.Combine(_testDir, "settings.json");
        await File.WriteAllTextAsync(settingsFile, "{ this is not valid json! *#&$ }");

        var service = new SettingsService(_testDir);
        await service.LoadSettingsAsync();

        // Must recover safely with valid defaults without crashing
        Assert.IsNotNull(service.Settings);
        Assert.IsFalse(string.IsNullOrEmpty(service.Settings.AccentColor));
    }

    [TestMethod]
    public async Task PackageManager_ReturnsCatalog_AndTracksInstallState()
    {
        var sec = new SecurityService(_testDir);
        var pkgMgr = new PackageManager(sec, _testDir);

        var catalog = await pkgMgr.GetCatalogAsync();
        Assert.IsTrue(catalog.Count >= 8);

        var explorerPkg = catalog.FirstOrDefault(c => c.Id == "nexus.explorer");
        Assert.IsNotNull(explorerPkg);
        Assert.IsTrue(explorerPkg.IsInstalled);

        // Test installing a new package
        var testPkg = new PackageManifest
        {
            Id = "test.custom.tool",
            Name = "Custom Test Tool",
            Version = "1.0",
            Publisher = "Test Publisher",
            IsInstalled = false
        };

        var installSuccess = await pkgMgr.InstallPackageAsync(testPkg);
        Assert.IsTrue(installSuccess);

        var installed = await pkgMgr.GetInstalledPackagesAsync();
        Assert.IsTrue(installed.Any(p => p.Id == "test.custom.tool"));

        // Test uninstall
        var uninstallSuccess = await pkgMgr.UninstallPackageAsync("test.custom.tool");
        Assert.IsTrue(uninstallSuccess);
    }
}
