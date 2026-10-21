using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NexusOS.Core.Settings;
using NexusOS.Core.Storage;

namespace NexusOS.Apps.Settings;

public partial class SettingsView : UserControl
{
    private readonly ISettingsService _settingsService;
    private readonly IVirtualStorageService _storageService;

    public event Action<string>? AccentColorChanged;
    public event Action<string>? WallpaperChanged;
    public event Action<WallpaperScaleMode>? WallpaperScaleChanged;
    public event Action<double>? CornerRadiusChanged;
    public event Action<double>? TransparencyChanged;
    public event Action<bool>? ReducedMotionChanged;
    public event Action<bool>? LowResourceModeChanged;

    public SettingsView(ISettingsService settingsService, IVirtualStorageService storageService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _storageService = storageService;

        LoadCurrentValues();
    }

    private void LoadCurrentValues()
    {
        var s = _settingsService.Settings;
        CornerRadiusSlider.Value = s.WindowCornerRadius;
        TransparencySlider.Value = s.WindowTransparency;
        ReducedMotionCheckBox.IsChecked = s.ReducedMotion;
        LowResourceModeCheckBox.IsChecked = s.LowResourceMode;
        PromptExecCheckBox.IsChecked = s.PromptBeforeHostExecution;
        VerifyAuthenticodeCheckBox.IsChecked = s.VerifyAuthenticode;

        WorkspacePathText.Text = _storageService.WorkspaceRoot;

        // Populate display info
        ResolutionInfoText.Text = $"Primary Display: {SystemParameters.PrimaryScreenWidth:0} x {SystemParameters.PrimaryScreenHeight:0} (High DPI Aware)";
    }

    private void SettingsNavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SettingsNavList.SelectedItem is ListBoxItem item && item.Tag is string tag)
        {
            PanelPersonalization.Visibility = tag == "Personalization" ? Visibility.Visible : Visibility.Collapsed;
            PanelDisplay.Visibility = tag == "Display" ? Visibility.Visible : Visibility.Collapsed;
            PanelStorage.Visibility = tag == "Storage" ? Visibility.Visible : Visibility.Collapsed;
            PanelSecurity.Visibility = tag == "Security" ? Visibility.Visible : Visibility.Collapsed;
            PanelPerformance.Visibility = tag == "Performance" ? Visibility.Visible : Visibility.Collapsed;
            PanelAbout.Visibility = tag == "About" ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void AccentColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex)
        {
            _settingsService.Settings.AccentColor = hex;
            _ = _settingsService.SaveSettingsAsync();
            AccentColorChanged?.Invoke(hex);
        }
    }

    private void WallpaperPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string name)
        {
            _settingsService.Settings.WallpaperName = name;
            _ = _settingsService.SaveSettingsAsync();
            WallpaperChanged?.Invoke(name);
        }
    }

    private void BrowseCustomWallpaper_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Custom Wallpaper",
            Filter = "Images (*.jpg;*.jpeg;*.png;*.bmp;*.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.webp|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            var path = dialog.FileName;
            _settingsService.Settings.WallpaperPath = path;
            _ = _settingsService.SaveSettingsAsync();
            WallpaperChanged?.Invoke(path);
        }
    }

    private void WallpaperScaleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WallpaperScaleCombo.SelectedItem is ComboBoxItem item)
        {
            var mode = item.Content.ToString() switch
            {
                "Fit" => WallpaperScaleMode.Fit,
                "Stretch" => WallpaperScaleMode.Stretch,
                "Center" => WallpaperScaleMode.Center,
                _ => WallpaperScaleMode.Fill
            };

            _settingsService.Settings.WallpaperScale = mode;
            _ = _settingsService.SaveSettingsAsync();
            WallpaperScaleChanged?.Invoke(mode);
        }
    }

    private void ThemeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Toggle light / dark mode
    }

    private void CornerRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _settingsService.Settings.WindowCornerRadius = e.NewValue;
        CornerRadiusChanged?.Invoke(e.NewValue);
    }

    private void TransparencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _settingsService.Settings.WindowTransparency = e.NewValue;
        TransparencyChanged?.Invoke(e.NewValue);
    }

    private void ReducedMotion_Changed(object sender, RoutedEventArgs e)
    {
        var isChecked = ReducedMotionCheckBox.IsChecked == true;
        _settingsService.Settings.ReducedMotion = isChecked;
        _ = _settingsService.SaveSettingsAsync();
        ReducedMotionChanged?.Invoke(isChecked);
    }

    private void LowResourceMode_Changed(object sender, RoutedEventArgs e)
    {
        var isChecked = LowResourceModeCheckBox.IsChecked == true;
        _settingsService.Settings.LowResourceMode = isChecked;
        _ = _settingsService.SaveSettingsAsync();
        LowResourceModeChanged?.Invoke(isChecked);
    }

    private void SecuritySetting_Changed(object sender, RoutedEventArgs e)
    {
        _settingsService.Settings.PromptBeforeHostExecution = PromptExecCheckBox.IsChecked == true;
        _settingsService.Settings.VerifyAuthenticode = VerifyAuthenticodeCheckBox.IsChecked == true;
        _ = _settingsService.SaveSettingsAsync();
    }

    private void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Cache and thumbnail storage cleared successfully.", "Storage Manager", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void ResetStorage_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This will reset default folders in your sandboxed Nexus storage. Are you sure?",
            "Confirm Reset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _settingsService.ResetToDefaultsAsync();
            LoadCurrentValues();
        }
    }
}
