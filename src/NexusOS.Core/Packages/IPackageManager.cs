namespace NexusOS.Core.Packages;

public enum PackageInstallType
{
    BuiltInNexusApp,
    ManagedExecutable,
    WebExtension,
    CustomTheme,
    DesktopWidget
}

public class PackageManifest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Publisher { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Utilities";
    public string IconKey { get; set; } = "App";
    public double SizeMb { get; set; } = 1.0;
    public string ChecksumSha256 { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public PackageInstallType InstallType { get; set; } = PackageInstallType.BuiltInNexusApp;
    public string ExecutableTarget { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public string ReleaseNotes { get; set; } = string.Empty;
    public bool IsInstalled { get; set; }
}

public interface IPackageManager
{
    Task<List<PackageManifest>> GetCatalogAsync();
    Task<List<PackageManifest>> GetInstalledPackagesAsync();
    Task<bool> InstallPackageAsync(PackageManifest package, IProgress<double>? progress = null);
    Task<bool> UninstallPackageAsync(string packageId);
    bool VerifyChecksum(string filePath, string expectedSha256);
}
