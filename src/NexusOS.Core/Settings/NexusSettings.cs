namespace NexusOS.Core.Settings;

public enum WallpaperScaleMode
{
    Fill,
    Fit,
    Stretch,
    Tile,
    Center
}

public enum TaskbarAlignmentMode
{
    Center,
    Left
}

public class NexusSettings
{
    // Personalization
    public bool DarkMode { get; set; } = true;
    public string AccentColor { get; set; } = "#2563EB";
    public double WindowTransparency { get; set; } = 0.88;
    public bool EnableBlur { get; set; } = true;
    public double WindowCornerRadius { get; set; } = 10.0;
    public bool ReducedMotion { get; set; } = false;
    public double AnimationSpeed { get; set; } = 1.0;
    
    // Wallpaper
    public string WallpaperName { get; set; } = "Nexus Aurora";
    public string WallpaperPath { get; set; } = "";
    public WallpaperScaleMode WallpaperScale { get; set; } = WallpaperScaleMode.Fill;
    public bool IsWallpaperSlideshow { get; set; } = false;
    public int WallpaperSlideshowIntervalMinutes { get; set; } = 15;
    public string FallbackColor { get; set; } = "#0F172A";

    // Taskbar & Desktop
    public TaskbarAlignmentMode TaskbarAlignment { get; set; } = TaskbarAlignmentMode.Center;
    public bool TaskbarAutoHide { get; set; } = false;
    public bool ShowDesktopIcons { get; set; } = true;
    public int DesktopIconSize { get; set; } = 48; // px
    public int VirtualDesktopCount { get; set; } = 3;
    public int CurrentVirtualDesktopIndex { get; set; } = 0;

    // Storage
    public string WorkspacePath { get; set; } = "";
    public string DownloadsPath { get; set; } = "";
    public List<string> PermittedHostFolders { get; set; } = new();

    // Security & Execution
    public bool PromptBeforeHostExecution { get; set; } = true;
    public bool VerifyAuthenticode { get; set; } = true;
    public bool LowResourceMode { get; set; } = false;

    // Audio
    public double SystemVolume { get; set; } = 0.75;
    public bool IsMuted { get; set; } = false;
}

public interface ISettingsService
{
    NexusSettings Settings { get; }
    Task LoadSettingsAsync();
    Task SaveSettingsAsync();
    Task ResetToDefaultsAsync();
    event Action<NexusSettings>? SettingsChanged;
}
