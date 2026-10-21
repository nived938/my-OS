using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using NexusOS.Apps.AppStore;
using NexusOS.Apps.Browser;
using NexusOS.Apps.FileManager;
using NexusOS.Apps.Games;
using NexusOS.Apps.Settings;
using NexusOS.Apps.Terminal;
using NexusOS.Apps.Virtualization;
using NexusOS.Apps.Widgets;
using NexusOS.Core.Apps;
using NexusOS.Core.Notifications;
using NexusOS.Core.Packages;
using NexusOS.Core.Security;
using NexusOS.Core.Settings;
using NexusOS.Core.Storage;
using NexusOS.Core.Virtualization;
using NexusOS.Core.Windowing;
using NexusOS.Infrastructure.Packages;
using NexusOS.Infrastructure.Security;
using NexusOS.Infrastructure.Services;
using NexusOS.Infrastructure.Storage;
using NexusOS.Infrastructure.Virtualization;
using NexusOS.UI.Controls;
using ShapePath = System.Windows.Shapes.Path;

namespace NexusOS.Shell;

public partial class MainWindow : Window
{
    // Core Subsystems
    private readonly ISettingsService _settingsService;
    private readonly IVirtualStorageService _storageService;
    private readonly ISecurityService _securityService;
    private readonly IPackageManager _packageManager;
    private readonly IVirtualizationService _virtService;
    private readonly INotificationService _notificationService;
    private readonly IWindowManager _windowManager;
    private readonly IAppRegistry _appRegistry;
    private readonly HostExecutionBridge _hostExecutionBridge;

    // Track active window controls mapped to WindowInstance IDs
    private readonly Dictionary<string, WindowHostControl> _windowControls = new();

    // Security Modal State
    private string _pendingExecutablePath = string.Empty;

    public MainWindow()
    {
        InitializeComponent();

        // 1. Initialize Infrastructure
        _settingsService = new SettingsService();
        _storageService = new VirtualStorageService();
        _storageService.Initialize();

        _securityService = new SecurityService();
        _packageManager = new PackageManager(_securityService);
        _virtService = new VirtualizationService();
        _notificationService = new NotificationService();
        _windowManager = new WindowManager();
        _appRegistry = new AppRegistry();
        _hostExecutionBridge = new HostExecutionBridge(_securityService);

        // 2. Register Built-in Applications
        RegisterApplications();

        // 3. Initialize Shell UI Controls
        Taskbar.Initialize(_windowManager, _appRegistry);
        StartMenuFlyout.Initialize(_appRegistry);
        QuickSettingsFlyout.Initialize(_settingsService);
        ActionCenterFlyout.Initialize(_notificationService);

        // 4. Hook Window Manager Events
        _windowManager.WindowOpened += OnWindowOpened;
        _windowManager.WindowClosed += OnWindowClosed;
        _windowManager.WindowActivated += OnWindowActivated;
        _windowManager.WindowStateChanged += OnWindowStateChanged;

        // 5. Hook Shell Events
        HookShellEvents();

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _settingsService.LoadSettingsAsync();
        ApplySettingsToShell(_settingsService.Settings);

        // Render desktop icons
        RefreshDesktopIcons();

        // Send Welcome Notification
        _notificationService.Send(
            "Welcome to NEXUS OS",
            "NEXUS OS environment is running. Drag files from Windows into this window to import them.",
            "Nexus System",
            "Info");

        // Boot screen smooth fade-out
        await Task.Delay(1000);
        var fadeAnim = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(500));
        fadeAnim.Completed += (s, args) => BootScreenGrid.Visibility = Visibility.Collapsed;
        BootScreenGrid.BeginAnimation(OpacityProperty, fadeAnim);
    }

    private void RegisterApplications()
    {
        _appRegistry.Register(new NexusAppEntry("nexus.explorer", "Explorer", "Nexus Explorer", "File manager with host drag-and-drop", "Folder", AppCategory.System, () =>
        {
            var view = new FileManagerView(_storageService, _securityService);
            view.InspectExecutableRequested += PromptSecurityVerification;
            return view;
        }));

        _appRegistry.Register(new NexusAppEntry("nexus.browser", "Navigator", "Nexus Navigator", "Chromium web browser with download manager", "Globe", AppCategory.Internet, () =>
        {
            var view = new BrowserView(_storageService, _securityService);
            view.OpenInExplorerRequested += path => LaunchApp("nexus.explorer");
            view.InspectExecutableRequested += PromptSecurityVerification;
            return view;
        }));

        _appRegistry.Register(new NexusAppEntry("nexus.store", "Store", "Nexus Store", "App catalog & package manager", "Store", AppCategory.System, () =>
        {
            var view = new AppStoreView(_packageManager);
            view.OpenAppRequested += appId => LaunchApp(appId);
            return view;
        }));

        _appRegistry.Register(new NexusAppEntry("nexus.settings", "Settings", "Nexus Settings", "Themes, wallpapers, storage & privacy", "Settings", AppCategory.System, () =>
        {
            var view = new SettingsView(_settingsService, _storageService);
            view.AccentColorChanged += hex => ApplyAccentColor(hex);
            view.WallpaperChanged += nameOrPath => ApplyWallpaper(nameOrPath);
            view.ReducedMotionChanged += enabled => _settingsService.Settings.ReducedMotion = enabled;
            view.CornerRadiusChanged += r => ApplyCornerRadius(r);
            return view;
        }));

        _appRegistry.Register(new NexusAppEntry("nexus.terminal", "Terminal", "Nexus Terminal", "Command line with safe PowerShell bridge", "Terminal", AppCategory.Development, () =>
        {
            return new TerminalView(_storageService);
        }));

        _appRegistry.Register(new NexusAppEntry("nexus.games", "Arcade Games", "Nexus Arcade", "Minesweeper, Snake, Sudoku, 2D Runner", "Gamepad", AppCategory.Games, () =>
        {
            return new GamesView();
        }));

        _appRegistry.Register(new NexusAppEntry("nexus.widgets", "Widgets", "Nexus Widgets & Rainmeter", "Live CPU/RAM monitors and Rainmeter guide", "Activity", AppCategory.Customization, () =>
        {
            return new WidgetsView();
        }));

        _appRegistry.Register(new NexusAppEntry("nexus.virtualization", "Virtualization", "Nexus Virtualization", "Mode B QEMU & Hyper-V guest manager", "Shield", AppCategory.System, () =>
        {
            return new VirtualizationView(_virtService);
        }));
    }

    private void HookShellEvents()
    {
        // Taskbar interactions
        Taskbar.StartClicked += ToggleStartMenu;
        Taskbar.SearchClicked += () =>
        {
            StartMenuFlyout.Visibility = Visibility.Visible;
            StartMenuFlyout.FocusSearch();
        };
        Taskbar.QuickSettingsClicked += ToggleQuickSettings;
        Taskbar.NotificationsClicked += ToggleActionCenter;
        Taskbar.ShowDesktopClicked += () => _windowManager.MinimizeAll();

        // Start Menu actions
        StartMenuFlyout.AppLaunched += appId =>
        {
            StartMenuFlyout.Visibility = Visibility.Collapsed;
            LaunchApp(appId);
        };
        StartMenuFlyout.SettingsRequested += () =>
        {
            StartMenuFlyout.Visibility = Visibility.Collapsed;
            LaunchApp("nexus.settings");
        };
        StartMenuFlyout.ExitNexusRequested += () => Close();
        StartMenuFlyout.RestartNexusRequested += () =>
        {
            var currentProc = Environment.ProcessPath;
            if (currentProc != null)
            {
                System.Diagnostics.Process.Start(currentProc);
            }
            Close();
        };
        StartMenuFlyout.HostShutdownRequested += () =>
        {
            // Host Windows Shutdown: executed only after explicit confirmation in StartMenuControl!
            System.Diagnostics.Process.Start("shutdown.exe", "/s /t 60");
            _notificationService.Send("Host Shutdown", "Windows PC will shut down in 60 seconds. Run 'shutdown /a' in CMD to cancel.", "Nexus System", "Warning");
        };

        // Quick Settings actions
        QuickSettingsFlyout.OpenSettingsRequested += () =>
        {
            QuickSettingsFlyout.Visibility = Visibility.Collapsed;
            LaunchApp("nexus.settings");
        };
        QuickSettingsFlyout.NightLightToggled += enabled =>
        {
            RootShellGrid.Background = enabled ? new SolidColorBrush(Color.FromArgb(20, 255, 140, 0)) : Brushes.Transparent;
        };

        // Alt+Tab
        AltTabSwitcher.WindowSelected += win =>
        {
            _windowManager.ActivateWindow(win.Id);
        };
    }

    #region Window Launching & Management

    public void LaunchApp(string appId, string[]? args = null)
    {
        // Check if an existing window for this app is already open
        var existing = _windowManager.OpenWindows.FirstOrDefault(w => w.AppId == appId);
        if (existing != null)
        {
            _windowManager.ActivateWindow(existing.Id);
            return;
        }

        var app = _appRegistry.FindById(appId);
        if (app == null) return;

        // Position cascaded
        var openCount = _windowManager.OpenWindows.Count;
        var startLeft = 100 + (openCount * 30) % 200;
        var startTop = 70 + (openCount * 25) % 150;

        var windowInstance = new WindowInstance(app.Id, app.DisplayName, app.IconKey, width: 880, height: 580)
        {
            Left = startLeft,
            Top = startTop,
            Content = app.CreateView()
        };

        _windowManager.RegisterWindow(windowInstance);
    }

    private void OnWindowOpened(IWindowInstance window)
    {
        var host = new WindowHostControl();
        host.Initialize(window, _windowManager);

        _windowControls[window.Id] = host;
        WindowsCanvas.Children.Add(host);
        host.UpdateZIndexAndPosition();
    }

    private void OnWindowClosed(IWindowInstance window)
    {
        if (_windowControls.TryGetValue(window.Id, out var host))
        {
            WindowsCanvas.Children.Remove(host);
            _windowControls.Remove(window.Id);
        }
    }

    private void OnWindowActivated(IWindowInstance window)
    {
        if (_windowControls.TryGetValue(window.Id, out var host))
        {
            host.UpdateZIndexAndPosition();
            host.UpdateStateVisuals();
        }
    }

    private void OnWindowStateChanged(IWindowInstance window)
    {
        if (_windowControls.TryGetValue(window.Id, out var host))
        {
            host.UpdateZIndexAndPosition();
            host.UpdateStateVisuals();
        }
    }

    #endregion

    #region Desktop Icons & Host Drag-and-Drop

    public void RefreshDesktopIcons()
    {
        DesktopCanvas.Children.Clear();

        var desktopApps = new[]
        {
            ("nexus.explorer", "Explorer", "IconFolder"),
            ("nexus.browser", "Navigator", "IconGlobe"),
            ("nexus.store", "Store", "IconStore"),
            ("nexus.settings", "Settings", "IconSettings"),
            ("nexus.terminal", "Terminal", "IconTerminal"),
            ("nexus.games", "Arcade Games", "IconGamepad"),
            ("nexus.widgets", "Widgets", "IconSettings"),
            ("nexus.virtualization", "Virtualization", "IconShield")
        };

        double x = 20;
        double y = 20;
        const double itemWidth = 84;
        const double itemHeight = 90;

        foreach (var (appId, label, icon) in desktopApps)
        {
            var iconBtn = CreateDesktopIconElement(label, icon, () => LaunchApp(appId));
            Canvas.SetLeft(iconBtn, x);
            Canvas.SetTop(iconBtn, y);
            DesktopCanvas.Children.Add(iconBtn);

            y += itemHeight;
            if (y > ActualHeight - 120 && ActualHeight > 200)
            {
                y = 20;
                x += itemWidth;
            }
        }

        // Also display files from Nexus Desktop storage
        try
        {
            var desktopFiles = _storageService.GetItems(_storageService.DesktopRoot);
            foreach (var file in desktopFiles)
            {
                var iconBtn = CreateDesktopIconElement(file.Name, file.IconKey == "Folder" ? "IconFolder" : "IconFile", () =>
                {
                    if (file.Extension == ".exe" || file.Extension == ".msi")
                    {
                        PromptSecurityVerification(file.FullPath);
                    }
                    else
                    {
                        LaunchApp("nexus.explorer");
                    }
                });

                Canvas.SetLeft(iconBtn, x);
                Canvas.SetTop(iconBtn, y);
                DesktopCanvas.Children.Add(iconBtn);

                y += itemHeight;
                if (y > ActualHeight - 120 && ActualHeight > 200)
                {
                    y = 20;
                    x += itemWidth;
                }
            }
        }
        catch
        {
        }
    }

    private Button CreateDesktopIconElement(string text, string iconResourceKey, Action onDoubleClick)
    {
        var btn = new Button
        {
            Width = 78,
            Height = 84,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            ToolTip = text
        };

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var path = new ShapePath
        {
            Data = TryFindResource(iconResourceKey) as Geometry ?? TryFindResource("IconFile") as Geometry,
            Fill = TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue,
            Width = 34,
            Height = 34,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        };

        var label = new TextBlock
        {
            Text = text,
            FontSize = 11,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxHeight = 30
        };

        stack.Children.Add(path);
        stack.Children.Add(label);
        btn.Content = stack;

        btn.MouseDoubleClick += (s, e) =>
        {
            onDoubleClick();
            e.Handled = true;
        };

        // Enable dragging desktop icons to reposition them on the desktop!
        Point startPoint = default;
        bool isDraggingIcon = false;

        btn.PreviewMouseLeftButtonDown += (s, e) =>
        {
            startPoint = e.GetPosition(DesktopCanvas);
            isDraggingIcon = true;
        };

        btn.PreviewMouseMove += (s, e) =>
        {
            if (isDraggingIcon && e.LeftButton == MouseButtonState.Pressed)
            {
                var cur = e.GetPosition(DesktopCanvas);
                if (Math.Abs(cur.X - startPoint.X) > 4 || Math.Abs(cur.Y - startPoint.Y) > 4)
                {
                    Canvas.SetLeft(btn, cur.X - 35);
                    Canvas.SetTop(btn, cur.Y - 35);
                }
            }
        };

        btn.PreviewMouseLeftButtonUp += (s, e) =>
        {
            isDraggingIcon = false;
        };

        return btn;
    }

    private void DesktopCanvas_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void DesktopCanvas_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        e.Handled = true;
    }

    private async void DesktopCanvas_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (files == null || files.Length == 0) return;

        int count = 0;
        foreach (var file in files)
        {
            try
            {
                await _storageService.ImportFromHostAsync(file, _storageService.DesktopRoot);
                count++;
            }
            catch (Exception ex)
            {
                _notificationService.Send("Import Error", ex.Message, "Storage", "Warning");
            }
        }

        RefreshDesktopIcons();
        _notificationService.Send("Files Imported", $"Successfully imported {count} file(s) into your Nexus Desktop workspace.", "Nexus Storage", "Folder");
    }

    #endregion

    #region Authenticode & Security Verification Workflow

    public void PromptSecurityVerification(string filePath)
    {
        _pendingExecutablePath = filePath;
        var report = _securityService.InspectExecutable(filePath);

        SecFileNameText.Text = $"File: {report.FileName} ({report.FileSizeBytes / 1024.0:F1} KB)";
        SecSignerText.Text = $"Signer: {report.SignerSubject ?? "Unsigned / Unknown Author"}";
        SecHashText.Text = $"SHA-256: {report.Sha256Hash}";
        SecRiskText.Text = $"Risk Rating: {report.RiskLevel}";

        SecRiskText.Foreground = report.RiskLevel switch
        {
            SecurityRiskLevel.VerifiedSafe => TryFindResource("NexusSuccessBrush") as Brush ?? Brushes.LightGreen,
            SecurityRiskLevel.CautionUnsigned => TryFindResource("NexusWarningBrush") as Brush ?? Brushes.Orange,
            _ => TryFindResource("NexusDangerBrush") as Brush ?? Brushes.Red
        };

        SecurityModalGrid.Visibility = Visibility.Visible;
    }

    private void CancelSecurityModal_Click(object sender, RoutedEventArgs e)
    {
        SecurityModalGrid.Visibility = Visibility.Collapsed;
        _pendingExecutablePath = string.Empty;
    }

    private async void AuthorizeSecurityLaunch_Click(object sender, RoutedEventArgs e)
    {
        SecurityModalGrid.Visibility = Visibility.Collapsed;
        if (!string.IsNullOrEmpty(_pendingExecutablePath))
        {
            var result = await _hostExecutionBridge.LaunchExecutableAsync(_pendingExecutablePath);
            _notificationService.Send("Host Execution", result.Message, "Security Manager", result.Success ? "Info" : "Warning");
            _pendingExecutablePath = string.Empty;
        }
    }

    #endregion

    #region Personalization & Wallpapers

    private void ApplySettingsToShell(NexusSettings settings)
    {
        ApplyAccentColor(settings.AccentColor);
        ApplyWallpaper(settings.WallpaperPath);
        ApplyCornerRadius(settings.WindowCornerRadius);
    }

    private void ApplyAccentColor(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            Resources["NexusAccentBrush"] = new SolidColorBrush(color);
        }
        catch
        {
        }
    }

    private void ApplyCornerRadius(double radius)
    {
        Resources["WindowCornerRadius"] = new CornerRadius(radius);
    }

    private void ApplyWallpaper(string nameOrPath)
    {
        // Zero-black-screen guarantee:
        // Always preserve base gradient or previous working background if image is invalid
        if (string.IsNullOrWhiteSpace(nameOrPath))
        {
            WallpaperImage.Visibility = Visibility.Collapsed;
            return;
        }

        if (File.Exists(nameOrPath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(nameOrPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                WallpaperImage.Source = bitmap;
                WallpaperImage.Visibility = Visibility.Visible;
                return;
            }
            catch
            {
                // Preserve fallback
                WallpaperImage.Visibility = Visibility.Collapsed;
                return;
            }
        }

        // Gradient preset styles
        WallpaperImage.Visibility = Visibility.Collapsed;
        DesktopBackgroundBorder.Background = nameOrPath switch
        {
            "Cyber Horizon" => new LinearGradientBrush(Color.FromRgb(26, 16, 60), Color.FromRgb(5, 8, 22), 45),
            "Nordic Dark" => new LinearGradientBrush(Color.FromRgb(15, 23, 42), Color.FromRgb(2, 6, 23), 90),
            "Sunset Dunes" => new LinearGradientBrush(Color.FromRgb(45, 20, 44), Color.FromRgb(12, 10, 28), 135),
            _ => new RadialGradientBrush(Color.FromRgb(30, 41, 59), Color.FromRgb(8, 12, 20))
        };
    }

    #endregion

    #region Flyouts Toggle & Shell Controls

    private void ToggleStartMenu()
    {
        StartMenuFlyout.Visibility = StartMenuFlyout.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

        QuickSettingsFlyout.Visibility = Visibility.Collapsed;
        ActionCenterFlyout.Visibility = Visibility.Collapsed;
    }

    private void ToggleQuickSettings()
    {
        QuickSettingsFlyout.Visibility = QuickSettingsFlyout.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

        StartMenuFlyout.Visibility = Visibility.Collapsed;
        ActionCenterFlyout.Visibility = Visibility.Collapsed;
    }

    private void ToggleActionCenter()
    {
        ActionCenterFlyout.Visibility = ActionCenterFlyout.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

        StartMenuFlyout.Visibility = Visibility.Collapsed;
        QuickSettingsFlyout.Visibility = Visibility.Collapsed;
    }

    private void DesktopCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Dismiss flyouts when user clicks on empty desktop
        StartMenuFlyout.Visibility = Visibility.Collapsed;
        QuickSettingsFlyout.Visibility = Visibility.Collapsed;
        ActionCenterFlyout.Visibility = Visibility.Collapsed;
    }

    private void DesktopCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Desktop context menu is triggered automatically by WPF ContextMenu
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // Alt + Tab handler
        if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
        {
            if (AltTabSwitcher.Visibility == Visibility.Visible)
            {
                AltTabSwitcher.CycleNext();
            }
            else
            {
                AltTabSwitcher.ShowSwitcher(_windowManager);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.LWin || e.Key == Key.RWin)
        {
            ToggleStartMenu();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            if (AltTabSwitcher.Visibility == Visibility.Visible)
            {
                AltTabSwitcher.ConfirmSelection();
            }
            StartMenuFlyout.Visibility = Visibility.Collapsed;
            QuickSettingsFlyout.Visibility = Visibility.Collapsed;
            ActionCenterFlyout.Visibility = Visibility.Collapsed;
        }
    }

    private void HostMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void HostClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void DesktopIconSize_Click(object sender, RoutedEventArgs e)
    {
        RefreshDesktopIcons();
    }

    private void RefreshDesktop_Click(object sender, RoutedEventArgs e)
    {
        RefreshDesktopIcons();
    }

    private void OpenExplorer_Click(object sender, RoutedEventArgs e)
    {
        LaunchApp("nexus.explorer");
    }

    private void OpenTerminal_Click(object sender, RoutedEventArgs e)
    {
        LaunchApp("nexus.terminal");
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        LaunchApp("nexus.settings");
    }

    #endregion
}

public class NexusAppEntry : IApp
{
    private readonly Func<object> _viewFactory;

    public string Id { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string Version { get; } = "1.0.0";
    public string Publisher { get; } = "Nexus System";
    public AppCategory Category { get; }
    public string IconKey { get; }
    public bool IsInstalled { get; set; } = true;
    public bool IsPinnedToTaskbar { get; set; } = true;
    public bool IsPinnedToStart { get; set; } = true;

    public NexusAppEntry(string id, string name, string displayName, string description, string iconKey, AppCategory category, Func<object> viewFactory)
    {
        Id = id;
        Name = name;
        DisplayName = displayName;
        Description = description;
        IconKey = iconKey;
        Category = category;
        _viewFactory = viewFactory;
    }

    public object CreateView() => _viewFactory();
}
