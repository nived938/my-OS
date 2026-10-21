using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using NexusOS.Core.Apps;
using NexusOS.Core.Windowing;
using ShapePath = System.Windows.Shapes.Path;

namespace NexusOS.UI.Controls;

public partial class TaskbarControl : UserControl
{
    private IWindowManager? _windowManager;
    private IAppRegistry? _appRegistry;
    private readonly DispatcherTimer _clockTimer;

    public event Action? StartClicked;
    public event Action? SearchClicked;
    public event Action? QuickSettingsClicked;
    public event Action? NotificationsClicked;
    public event Action? ShowDesktopClicked;
    public event Action<string>? LaunchAppRequested;

    public TaskbarControl()
    {
        InitializeComponent();

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += ClockTimer_Tick;
        _clockTimer.Start();
        UpdateClockDisplay();
    }

    public void Initialize(IWindowManager windowManager, IAppRegistry appRegistry)
    {
        _windowManager = windowManager;
        _appRegistry = appRegistry;

        _windowManager.WindowOpened += OnWindowListChanged;
        _windowManager.WindowClosed += OnWindowListChanged;
        _windowManager.WindowActivated += OnWindowListChanged;
        _windowManager.WindowStateChanged += OnWindowListChanged;

        RefreshRunningApps();
    }

    private void ClockTimer_Tick(object? sender, EventArgs e)
    {
        UpdateClockDisplay();
    }

    private void UpdateClockDisplay()
    {
        var now = DateTime.Now;
        TimeTextBlock.Text = now.ToString("t");
        DateTextBlock.Text = now.ToString("yyyy-MM-dd");
    }

    private void OnWindowListChanged(IWindowInstance window)
    {
        Dispatcher.Invoke(RefreshRunningApps);
    }

    public void RefreshRunningApps()
    {
        RunningAppsContainer.Children.Clear();
        if (_windowManager == null) return;

        foreach (var win in _windowManager.OpenWindows)
        {
            var btn = CreateTaskbarAppButton(win);
            RunningAppsContainer.Children.Add(btn);
        }
    }

    private Button CreateTaskbarAppButton(IWindowInstance window)
    {
        var btn = new Button
        {
            Width = 42,
            Height = 40,
            Margin = new Thickness(2, 0, 2, 0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = window.Title
        };

        var grid = new Grid();

        // Icon
        var iconPath = new ShapePath
        {
            Data = TryFindResource(GetIconResourceKey(window.IconKey)) as Geometry ?? TryFindResource("IconFolder") as Geometry,
            Fill = TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue,
            Width = 18,
            Height = 18,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        grid.Children.Add(iconPath);

        // Active/Running Indicator Pill
        var indicator = new Border
        {
            Height = 3,
            Width = window.IsActive ? 16 : 6,
            CornerRadius = new CornerRadius(1.5),
            Background = window.IsActive
                ? (TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue)
                : (TryFindResource("NexusTextMutedBrush") as Brush ?? Brushes.Gray),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 2)
        };
        grid.Children.Add(indicator);

        btn.Content = grid;

        btn.Click += (s, e) =>
        {
            if (window.IsActive && window.State != WindowDisplayState.Minimized)
            {
                _windowManager?.MinimizeWindow(window.Id);
            }
            else
            {
                _windowManager?.ActivateWindow(window.Id);
            }
        };

        return btn;
    }

    private string GetIconResourceKey(string key)
    {
        return key switch
        {
            "Globe" or "Browser" => "IconGlobe",
            "Terminal" => "IconTerminal",
            "Settings" => "IconSettings",
            "Store" => "IconStore",
            "Gamepad" or "Cards" or "Bomb" or "Rocket" or "Grid" => "IconGamepad",
            "Folder" => "IconFolder",
            _ => "IconFile"
        };
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        StartClicked?.Invoke();
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        SearchClicked?.Invoke();
    }

    private void QuickSettings_Click(object sender, RoutedEventArgs e)
    {
        QuickSettingsClicked?.Invoke();
    }

    private void ClockButton_Click(object sender, RoutedEventArgs e)
    {
        NotificationsClicked?.Invoke();
    }

    private void NotificationCenterButton_Click(object sender, RoutedEventArgs e)
    {
        NotificationsClicked?.Invoke();
    }

    private void ShowDesktopButton_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ShowDesktopClicked?.Invoke();
    }
}
