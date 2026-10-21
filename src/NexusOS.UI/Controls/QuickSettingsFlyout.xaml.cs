using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NexusOS.Core.Settings;

namespace NexusOS.UI.Controls;

public partial class QuickSettingsFlyout : UserControl
{
    private ISettingsService? _settingsService;

    public event Action? OpenSettingsRequested;
    public event Action<bool>? NightLightToggled;
    public event Action<bool>? LowResourceModeToggled;
    public event Action<double>? VolumeChanged;

    public QuickSettingsFlyout()
    {
        InitializeComponent();
        DetectHostBattery();
    }

    public void Initialize(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        VolumeSlider.Value = _settingsService.Settings.SystemVolume * 100.0;
        LowResourceToggle.IsChecked = _settingsService.Settings.LowResourceMode;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

    private void DetectHostBattery()
    {
        try
        {
            if (GetSystemPowerStatus(out var status))
            {
                var percent = status.BatteryLifePercent;
                var isPluggedIn = status.ACLineStatus == 1;

                if (percent == 255 || status.BatteryFlag == 128)
                {
                    BatteryStatusText.Text = "AC Power Connected (No battery)";
                }
                else
                {
                    var statusStr = isPluggedIn ? $"{percent}% (Plugged in)" : $"{percent}% (On battery)";
                    BatteryStatusText.Text = statusStr;
                }
                return;
            }
        }
        catch
        {
        }
        BatteryStatusText.Text = "AC Power Connected";
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsRequested?.Invoke();
    }

    private void NightLightToggle_Click(object sender, RoutedEventArgs e)
    {
        var isChecked = NightLightToggle.IsChecked == true;
        NightLightToggled?.Invoke(isChecked);
    }

    private void LowResourceToggle_Click(object sender, RoutedEventArgs e)
    {
        var isChecked = LowResourceToggle.IsChecked == true;
        if (_settingsService != null)
        {
            _settingsService.Settings.LowResourceMode = isChecked;
            _ = _settingsService.SaveSettingsAsync();
        }
        LowResourceModeToggled?.Invoke(isChecked);
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        VolumeChanged?.Invoke(e.NewValue / 100.0);
        if (_settingsService != null)
        {
            _settingsService.Settings.SystemVolume = e.NewValue / 100.0;
        }
    }
}
