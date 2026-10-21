using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NexusOS.Infrastructure.Customization;

namespace NexusOS.Apps.Widgets;

public partial class WidgetsView : UserControl
{
    private readonly RainmeterBridge _rainmeterBridge = new();
    private readonly DispatcherTimer _timer = new();

    public WidgetsView()
    {
        InitializeComponent();

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;
        _timer.Start();

        UpdateStats();
        DisplayRainmeterInfo();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateStats();
    }

    private void UpdateStats()
    {
        var now = DateTime.Now;
        BigClockText.Text = now.ToString("HH:mm:ss");
        BigDateText.Text = now.ToString("dddd, MMMM d, yyyy");

        try
        {
            var proc = Process.GetCurrentProcess();
            var memoryMb = proc.WorkingSet64 / (1024.0 * 1024.0);
            RamProgressBar.Value = Math.Min(100, (memoryMb / 1024.0) * 100);
            RamText.Text = $"NEXUS Process Memory: {memoryMb:F1} MB";

            var totalCores = Environment.ProcessorCount;
            CpuText.Text = $"System Ready • {totalCores} Logical CPU Cores";
        }
        catch
        {
        }
    }

    private void DisplayRainmeterInfo()
    {
        var status = _rainmeterBridge.CheckRainmeterStatus();
        RainmeterExplanationText.Text =
            $"{status.ArchitectureExplanation}\n\n" +
            $"Status on Host PC:\n" +
            $"• Installed: {(status.IsInstalledOnHost ? "YES (" + status.ExecutablePath + ")" : "NOT DETECTED")}\n" +
            $"• Running on Host Desktop: {(status.IsRunning ? "YES" : "NO")}";
    }

    private void LaunchRainmeter_Click(object sender, RoutedEventArgs e)
    {
        var launched = _rainmeterBridge.LaunchHostRainmeter();
        if (launched)
        {
            MessageBox.Show("Rainmeter was triggered on your host Windows desktop.", "Rainmeter Bridge", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(
                "Rainmeter executable not found at standard paths.\n\n" +
                "To use Rainmeter on your real Windows desktop, install it from rainmeter.net.",
                "Rainmeter Bridge",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
