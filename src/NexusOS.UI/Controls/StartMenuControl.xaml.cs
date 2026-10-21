using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NexusOS.Core.Apps;
using ShapePath = System.Windows.Shapes.Path;

namespace NexusOS.UI.Controls;

public partial class StartMenuControl : UserControl
{
    private IAppRegistry? _appRegistry;

    public event Action<string>? AppLaunched;
    public event Action? SettingsRequested;
    public event Action? LockRequested;
    public event Action? RestartNexusRequested;
    public event Action? ExitNexusRequested;
    public event Action? HostShutdownRequested;

    public StartMenuControl()
    {
        InitializeComponent();
    }

    public void Initialize(IAppRegistry appRegistry)
    {
        _appRegistry = appRegistry;
        PopulateApps();
        PopulateRecentItems();
    }

    public void FocusSearch()
    {
        SearchTextBox.Focus();
    }

    public void PopulateApps(string filter = "")
    {
        AppsWrapPanel.Children.Clear();
        if (_appRegistry == null) return;

        var apps = string.IsNullOrWhiteSpace(filter)
            ? _appRegistry.Apps
            : _appRegistry.Search(filter);

        foreach (var app in apps)
        {
            var btn = CreateAppTileButton(app);
            AppsWrapPanel.Children.Add(btn);
        }
    }

    private Button CreateAppTileButton(IApp app)
    {
        var btn = new Button
        {
            Width = 80,
            Height = 80,
            Margin = new Thickness(2),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = $"{app.DisplayName}\n{app.Description}"
        };

        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var iconPath = new ShapePath
        {
            Data = TryFindResource(GetIconResourceKey(app.IconKey)) as Geometry ?? TryFindResource("IconFolder") as Geometry,
            Fill = TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue,
            Width = 28,
            Height = 28,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        };

        var label = new TextBlock
        {
            Text = app.DisplayName,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = TryFindResource("NexusTextPrimaryBrush") as Brush ?? Brushes.White,
            MaxHeight = 28
        };

        panel.Children.Add(iconPath);
        panel.Children.Add(label);
        btn.Content = panel;

        btn.Click += (s, e) =>
        {
            AppLaunched?.Invoke(app.Id);
        };

        return btn;
    }

    private void PopulateRecentItems()
    {
        RecentFilesContainer.Children.Clear();

        var recentItems = new[]
        {
            ("Welcome to NEXUS OS.txt", "Desktop", "IconFile"),
            ("Nexus OS Documentation", "Documents", "IconFolder"),
            ("Downloads Workspace", "Downloads", "IconFolder")
        };

        foreach (var (name, loc, icon) in recentItems)
        {
            var itemPanel = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };
            
            var path = new ShapePath
            {
                Data = TryFindResource(icon) as Geometry ?? TryFindResource("IconFile") as Geometry,
                Fill = TryFindResource("NexusTextSecondaryBrush") as Brush ?? Brushes.SlateGray,
                Width = 14,
                Height = 14,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 8, 0)
            };
            DockPanel.SetDock(path, Dock.Left);

            var titleBlock = new TextBlock { Text = name, FontSize = 12, FontWeight = FontWeights.Medium };
            var locBlock = new TextBlock
            {
                Text = loc,
                FontSize = 11,
                Foreground = TryFindResource("NexusTextMutedBrush") as Brush ?? Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(locBlock, Dock.Right);

            itemPanel.Children.Add(path);
            itemPanel.Children.Add(locBlock);
            itemPanel.Children.Add(titleBlock);

            RecentFilesContainer.Children.Add(itemPanel);
        }
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

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var text = SearchTextBox.Text;
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
        PopulateApps(text);
    }

    private void AllAppsButton_Click(object sender, RoutedEventArgs e)
    {
        PopulateApps();
    }

    private void SettingsShortcutButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsRequested?.Invoke();
    }

    private void PowerButton_Click(object sender, RoutedEventArgs e)
    {
        PowerPopup.IsOpen = !PowerPopup.IsOpen;
    }

    private void LockNexus_Click(object sender, RoutedEventArgs e)
    {
        PowerPopup.IsOpen = false;
        LockRequested?.Invoke();
    }

    private void RestartNexus_Click(object sender, RoutedEventArgs e)
    {
        PowerPopup.IsOpen = false;
        RestartNexusRequested?.Invoke();
    }

    private void ExitNexus_Click(object sender, RoutedEventArgs e)
    {
        PowerPopup.IsOpen = false;
        ExitNexusRequested?.Invoke();
    }

    private void HostShutdown_Click(object sender, RoutedEventArgs e)
    {
        PowerPopup.IsOpen = false;

        // Strict requirement: Host shutdown must be explicit, clearly labeled, with confirmation dialog!
        var result = MessageBox.Show(
            "WARNING: This will shut down your host Windows computer.\n\n" +
            "Any unsaved work on your physical PC will be lost.\n\n" +
            "Do you really want to shut down your host PC?",
            "Confirm Host Windows PC Shutdown",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            HostShutdownRequested?.Invoke();
        }
    }
}
