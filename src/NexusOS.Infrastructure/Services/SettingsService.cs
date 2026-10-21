using System.Text.Json;
using NexusOS.Core.Settings;

namespace NexusOS.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly string _configDirectory;
    private readonly string _configFilePath;
    private NexusSettings _settings = new();

    public NexusSettings Settings => _settings;

    public event Action<NexusSettings>? SettingsChanged;

    public SettingsService(string? customBasePath = null)
    {
        var basePath = customBasePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NexusOS");
        _configDirectory = basePath;
        _configFilePath = Path.Combine(_configDirectory, "settings.json");

        EnsureDefaults();
    }

    private void EnsureDefaults()
    {
        if (string.IsNullOrWhiteSpace(_settings.WorkspacePath))
        {
            _settings.WorkspacePath = Path.Combine(_configDirectory, "Workspace");
        }
        if (string.IsNullOrWhiteSpace(_settings.DownloadsPath))
        {
            _settings.DownloadsPath = Path.Combine(_settings.WorkspacePath, "Downloads");
        }
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }

            if (File.Exists(_configFilePath))
            {
                var json = await File.ReadAllTextAsync(_configFilePath);
                var loaded = JsonSerializer.Deserialize<NexusSettings>(json);
                if (loaded != null)
                {
                    _settings = loaded;
                }
            }
            else
            {
                await SaveSettingsAsync();
            }

            EnsureDefaults();
            SettingsChanged?.Invoke(_settings);
        }
        catch (Exception ex)
        {
            // Corrupt file recovery: re-initialize with safe defaults
            _settings = new NexusSettings();
            EnsureDefaults();
            SettingsChanged?.Invoke(_settings);
            System.Diagnostics.Debug.WriteLine($"Failed to load settings, recovered with defaults: {ex.Message}");
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_settings, options);
            
            // Atomic write: write to temp file then move
            var tempPath = _configFilePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, _configFilePath, overwrite: true);

            SettingsChanged?.Invoke(_settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    public async Task ResetToDefaultsAsync()
    {
        _settings = new NexusSettings();
        EnsureDefaults();
        await SaveSettingsAsync();
    }
}
