using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NexusOS.Core.Security;
using NexusOS.Core.Storage;
using NexusOS.Infrastructure.Security;

namespace NexusOS.Apps.Browser;

public class DownloadItem
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Status { get; set; } = "Completed";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public partial class BrowserView : UserControl
{
    private readonly IVirtualStorageService _storageService;
    private readonly ISecurityService _securityService;
    private readonly HostExecutionBridge _hostExecutionBridge;

    private readonly List<string> _tabs = new();
    private int _currentTabIndex = 0;
    private readonly List<DownloadItem> _downloads = new();
    private bool _isWebViewInitialized = false;

    public event Action<string>? OpenInExplorerRequested;
    public event Action<string>? InspectExecutableRequested;

    public BrowserView(IVirtualStorageService storageService, ISecurityService securityService)
    {
        InitializeComponent();
        _storageService = storageService;
        _securityService = securityService;
        _hostExecutionBridge = new HostExecutionBridge(_securityService);

        _tabs.Add("https://duckduckgo.com");
        RenderTabs();

        Loaded += BrowserView_Loaded;
    }

    private async void BrowserView_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            // Configure sandboxed WebView2 user data folder inside Nexus OS
            var userDataFolder = Path.Combine(_storageService.WorkspaceRoot, ".browser_data");
            var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            await CoreWebView.EnsureCoreWebView2Async(env);

            _isWebViewInitialized = true;
            CoreWebView.CoreWebView2.SourceChanged += (s, e) =>
            {
                UrlTextBox.Text = CoreWebView.Source.ToString();
            };

            // Intercept downloads
            CoreWebView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;

            NavigateTo(_tabs[_currentTabIndex]);
        }
        catch (Exception ex)
        {
            // WebView2 runtime missing or initialization error -> display safe fallback
            FallbackWebPage.Visibility = Visibility.Visible;
            CoreWebView.Visibility = Visibility.Collapsed;
            System.Diagnostics.Debug.WriteLine($"WebView2 initialization: {ex.Message}");
        }
    }

    private void CoreWebView2_DownloadStarting(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2DownloadStartingEventArgs e)
    {
        var targetFile = Path.Combine(_storageService.DownloadsRoot, Path.GetFileName(e.ResultFilePath));
        e.ResultFilePath = targetFile;

        var downloadItem = new DownloadItem
        {
            FileName = Path.GetFileName(targetFile),
            FilePath = targetFile,
            Status = "Downloading..."
        };

        _downloads.Insert(0, downloadItem);
        RenderDownloadsList();

        e.DownloadOperation.StateChanged += (s, args) =>
        {
            if (e.DownloadOperation.State == Microsoft.Web.WebView2.Core.CoreWebView2DownloadState.Completed)
            {
                downloadItem.Status = "Completed";
                if (File.Exists(targetFile))
                {
                    downloadItem.FileSizeBytes = new FileInfo(targetFile).Length;
                }
                Dispatcher.Invoke(RenderDownloadsList);
            }
            else if (e.DownloadOperation.State == Microsoft.Web.WebView2.Core.CoreWebView2DownloadState.Interrupted)
            {
                downloadItem.Status = "Interrupted";
                Dispatcher.Invoke(RenderDownloadsList);
            }
        };
    }

    public void NavigateTo(string urlOrQuery)
    {
        if (string.IsNullOrWhiteSpace(urlOrQuery)) return;

        string finalUrl;
        if (Uri.TryCreate(urlOrQuery, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            finalUrl = uri.ToString();
        }
        else if (urlOrQuery.Contains('.') && !urlOrQuery.Contains(' '))
        {
            finalUrl = "https://" + urlOrQuery;
        }
        else
        {
            // Search query
            finalUrl = "https://duckduckgo.com/?q=" + Uri.EscapeDataString(urlOrQuery);
        }

        UrlTextBox.Text = finalUrl;
        if (_isWebViewInitialized && CoreWebView.CoreWebView2 != null)
        {
            CoreWebView.CoreWebView2.Navigate(finalUrl);
        }
    }

    private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NavigateTo(UrlTextBox.Text);
        }
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_isWebViewInitialized && CoreWebView.CanGoBack) CoreWebView.GoBack();
    }

    private void ForwardBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_isWebViewInitialized && CoreWebView.CanGoForward) CoreWebView.GoForward();
    }

    private void ReloadBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_isWebViewInitialized) CoreWebView.Reload();
    }

    private void HomeBtn_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("https://duckduckgo.com");
    }

    private void Bookmark_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string url)
        {
            NavigateTo(url);
        }
    }

    private void BookmarkCurrentBtn_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Bookmarked: {UrlTextBox.Text}", "Nexus Navigator", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DownloadsBtn_Click(object sender, RoutedEventArgs e)
    {
        DownloadsDrawer.Visibility = DownloadsDrawer.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void CloseDownloads_Click(object sender, RoutedEventArgs e)
    {
        DownloadsDrawer.Visibility = Visibility.Collapsed;
    }

    private void RenderTabs()
    {
        BrowserTabsContainer.Children.Clear();
        for (int i = 0; i < _tabs.Count; i++)
        {
            var isSelected = (i == _currentTabIndex);
            var tabBtn = new Button
            {
                Content = $"Tab {i + 1}",
                Style = TryFindResource(isSelected ? "NexusButtonPrimary" : "NexusButtonGhost") as Style,
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(2, 0, 2, 0),
                FontSize = 11.5
            };

            var index = i;
            tabBtn.Click += (s, e) =>
            {
                _currentTabIndex = index;
                RenderTabs();
                NavigateTo(_tabs[index]);
            };

            BrowserTabsContainer.Children.Add(tabBtn);
        }
    }

    private void NewTabBtn_Click(object sender, RoutedEventArgs e)
    {
        _tabs.Add("https://duckduckgo.com");
        _currentTabIndex = _tabs.Count - 1;
        RenderTabs();
        NavigateTo("https://duckduckgo.com");
    }

    #region Download Inspection & Safe Execution Workflow

    private void DownloadSampleExe_Click(object sender, RoutedEventArgs e)
    {
        SimulateExeDownload_Click(sender, e);
    }

    private async void SimulateExeDownload_Click(object sender, RoutedEventArgs e)
    {
        DownloadsDrawer.Visibility = Visibility.Visible;

        var sampleFile = Path.Combine(_storageService.DownloadsRoot, "nexus-tools-setup.exe");
        
        // Write a valid PE-style executable or copy a standard tool so it is genuinely inspectable
        if (!File.Exists(sampleFile))
        {
            // Use Windows notepad.exe as a real, authentic test binary signed by Microsoft!
            var hostNotepad = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "notepad.exe");
            if (File.Exists(hostNotepad))
            {
                File.Copy(hostNotepad, sampleFile, overwrite: true);
            }
            else
            {
                await File.WriteAllBytesAsync(sampleFile, new byte[1024]);
            }
        }

        var item = new DownloadItem
        {
            FileName = Path.GetFileName(sampleFile),
            FilePath = sampleFile,
            FileSizeBytes = new FileInfo(sampleFile).Length,
            Status = "Completed"
        };

        _downloads.Insert(0, item);
        RenderDownloadsList();
    }

    private void RenderDownloadsList()
    {
        DownloadsListContainer.Children.Clear();

        if (_downloads.Count == 0)
        {
            DownloadsListContainer.Children.Add(new TextBlock
            {
                Text = "No recent downloads",
                Foreground = TryFindResource("NexusTextMutedBrush") as Brush ?? Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            });
            return;
        }

        foreach (var d in _downloads)
        {
            var card = new Border
            {
                Background = TryFindResource("NexusSurfaceBrush") as Brush ?? Brushes.DarkSlateGray,
                BorderBrush = TryFindResource("NexusBorderSubtleBrush") as Brush ?? Brushes.DimGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var stack = new StackPanel();
            var title = new TextBlock { Text = d.FileName, FontWeight = FontWeights.SemiBold, FontSize = 12 };
            var info = new TextBlock
            {
                Text = $"{d.Status} • {d.FileSizeBytes / 1024.0:F1} KB",
                FontSize = 11,
                Foreground = TryFindResource("NexusTextSecondaryBrush") as Brush ?? Brushes.Silver,
                Margin = new Thickness(0, 2, 0, 8)
            };

            var actions = new StackPanel { Orientation = Orientation.Horizontal };

            var openFolderBtn = new Button
            {
                Content = "Open Folder",
                Style = TryFindResource("NexusButtonGhost") as Style,
                Padding = new Thickness(8, 2, 8, 2),
                FontSize = 11,
                Margin = new Thickness(0, 0, 6, 0)
            };
            openFolderBtn.Click += (s, e) => OpenInExplorerRequested?.Invoke(_storageService.DownloadsRoot);

            var inspectBtn = new Button
            {
                Content = "Inspect Security",
                Style = TryFindResource("NexusButtonGhost") as Style,
                Padding = new Thickness(8, 2, 8, 2),
                FontSize = 11,
                Margin = new Thickness(0, 0, 6, 0)
            };
            inspectBtn.Click += (s, e) =>
            {
                InspectExecutableRequested?.Invoke(d.FilePath);
            };

            var runBtn = new Button
            {
                Content = "Run on Host...",
                Style = TryFindResource("NexusButtonPrimary") as Style,
                Padding = new Thickness(8, 2, 8, 2),
                FontSize = 11
            };
            runBtn.Click += async (s, e) =>
            {
                var report = _securityService.InspectExecutable(d.FilePath);
                var confirm = MessageBox.Show(
                    $"AUTHENTICODE SECURITY VERIFICATION:\n\n" +
                    $"File: {d.FileName}\n" +
                    $"Signed: {(report.IsSigned ? "YES (Authenticode)" : "NO (Unsigned)")}\n" +
                    $"Publisher: {report.SignerSubject ?? "Unknown / Unsigned"}\n" +
                    $"SHA-256: {report.Sha256Hash}\n" +
                    $"Risk Rating: {report.RiskLevel}\n\n" +
                    $"Do you explicitly authorize running this executable on your host Windows system?",
                    "Authorize Host Application Execution",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    var result = await _hostExecutionBridge.LaunchExecutableAsync(d.FilePath);
                    MessageBox.Show(result.Message, "Host Execution Status", MessageBoxButton.OK,
                        result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
                }
            };

            actions.Children.Add(openFolderBtn);
            actions.Children.Add(inspectBtn);
            actions.Children.Add(runBtn);

            stack.Children.Add(title);
            stack.Children.Add(info);
            stack.Children.Add(actions);
            card.Child = stack;

            DownloadsListContainer.Children.Add(card);
        }
    }

    #endregion
}
