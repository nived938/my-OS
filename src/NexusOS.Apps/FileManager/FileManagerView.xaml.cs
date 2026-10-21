using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NexusOS.Core.Security;
using NexusOS.Core.Storage;
using ShapePath = System.Windows.Shapes.Path;

namespace NexusOS.Apps.FileManager;

public class ExplorerTab
{
    public string Title { get; set; } = "Home";
    public string Path { get; set; } = "";
}

public partial class FileManagerView : UserControl
{
    private readonly IVirtualStorageService _storageService;
    private readonly ISecurityService? _securityService;

    private string _currentDirectory = "";
    private readonly Stack<string> _backStack = new();
    private readonly Stack<string> _forwardStack = new();
    private readonly List<ExplorerTab> _tabs = new();
    private int _activeTabIndex = 0;
    private List<NexusStorageItem> _currentItems = new();

    public event Action<string>? OpenFileRequested;
    public event Action<string>? InspectExecutableRequested;

    public FileManagerView(IVirtualStorageService storageService, ISecurityService? securityService = null)
    {
        InitializeComponent();
        _storageService = storageService;
        _securityService = securityService;

        _storageService.Initialize();
        _currentDirectory = _storageService.WorkspaceRoot;

        // Initialize first tab
        _tabs.Add(new ExplorerTab { Title = "Home", Path = _currentDirectory });
        RenderTabs();
        NavigateTo(_currentDirectory, addToHistory: false);
    }

    public void NavigateTo(string path, bool addToHistory = true)
    {
        if (string.IsNullOrWhiteSpace(path) || !_storageService.Exists(path))
        {
            path = _storageService.WorkspaceRoot;
        }

        if (addToHistory && !string.IsNullOrEmpty(_currentDirectory) && _currentDirectory != path)
        {
            _backStack.Push(_currentDirectory);
            _forwardStack.Clear();
        }

        _currentDirectory = path;
        AddressTextBox.Text = path;
        UpdateHistoryButtons();

        // Update tab title
        if (_activeTabIndex >= 0 && _activeTabIndex < _tabs.Count)
        {
            _tabs[_activeTabIndex].Path = path;
            _tabs[_activeTabIndex].Title = Path.GetFileName(path);
            if (string.IsNullOrEmpty(_tabs[_activeTabIndex].Title))
            {
                _tabs[_activeTabIndex].Title = "Home";
            }
            RenderTabs();
        }

        RefreshCurrentFolder();
    }

    public void RefreshCurrentFolder()
    {
        try
        {
            _currentItems = _storageService.GetItems(_currentDirectory).ToList();
            ApplyFilter(SearchBox.Text);
        }
        catch (Exception ex)
        {
            StatusMessageTextBlock.Text = $"Error reading directory: {ex.Message}";
        }
    }

    private void ApplyFilter(string filter)
    {
        var items = string.IsNullOrWhiteSpace(filter)
            ? _currentItems
            : _currentItems.Where(i => i.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        // Populate Grid View
        FilesWrapPanel.Children.Clear();
        foreach (var item in items)
        {
            var tile = CreateFileTile(item);
            FilesWrapPanel.Children.Add(tile);
        }

        // Populate Details View
        FilesListView.ItemsSource = items;

        ItemCountTextBlock.Text = $"{items.Count} items";
        StatusMessageTextBlock.Text = "Ready. Drag files from Windows Explorer to import.";
    }

    private Button CreateFileTile(NexusStorageItem item)
    {
        var btn = new Button
        {
            Width = 90,
            Height = 90,
            Margin = new Thickness(3),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            ToolTip = $"{item.Name}\nModified: {item.DateModified}\nSize: {item.FormattedSize}"
        };

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var iconResource = item.IsDirectory ? "IconFolder" : "IconFile";
        var fillBrush = item.IsDirectory
            ? (TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue)
            : (TryFindResource("NexusTextSecondaryBrush") as Brush ?? Brushes.Silver);

        var icon = new ShapePath
        {
            Data = TryFindResource(iconResource) as Geometry ?? TryFindResource("IconFile") as Geometry,
            Fill = fillBrush,
            Width = 32,
            Height = 32,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        };

        var label = new TextBlock
        {
            Text = item.Name,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxHeight = 30,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = TryFindResource("NexusTextPrimaryBrush") as Brush ?? Brushes.White
        };

        stack.Children.Add(icon);
        stack.Children.Add(label);
        btn.Content = stack;

        // Double click handler
        btn.MouseDoubleClick += (s, e) =>
        {
            HandleItemActivation(item);
            e.Handled = true;
        };

        return btn;
    }

    private void HandleItemActivation(NexusStorageItem item)
    {
        if (item.IsDirectory)
        {
            NavigateTo(item.FullPath);
        }
        else
        {
            var ext = item.Extension.ToLowerInvariant();
            if (ext == ".exe" || ext == ".msi")
            {
                InspectExecutableRequested?.Invoke(item.FullPath);
            }
            else
            {
                OpenFileRequested?.Invoke(item.FullPath);
            }
        }
    }

    #region Drag and Drop Integration (Windows Host -> Nexus OS)

    private void FileManager_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            DropBanner.Visibility = Visibility.Visible;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void FileManager_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            DropBanner.Visibility = Visibility.Visible;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private async void FileManager_Drop(object sender, DragEventArgs e)
    {
        DropBanner.Visibility = Visibility.Collapsed;

        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var droppedPaths = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (droppedPaths == null || droppedPaths.Length == 0)
            return;

        StatusMessageTextBlock.Text = $"Importing {droppedPaths.Length} item(s) from Windows...";

        int importedCount = 0;
        foreach (var hostPath in droppedPaths)
        {
            try
            {
                await _storageService.ImportFromHostAsync(hostPath, _currentDirectory);
                importedCount++;
            }
            catch (Exception ex)
            {
                StatusMessageTextBlock.Text = $"Import failed for '{Path.GetFileName(hostPath)}': {ex.Message}";
            }
        }

        StatusMessageTextBlock.Text = $"Successfully imported {importedCount} item(s) into Nexus OS!";
        RefreshCurrentFolder();
    }

    #endregion

    #region Navigation & Tabs

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_backStack.Count > 0)
        {
            _forwardStack.Push(_currentDirectory);
            var prev = _backStack.Pop();
            NavigateTo(prev, addToHistory: false);
        }
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (_forwardStack.Count > 0)
        {
            _backStack.Push(_currentDirectory);
            var next = _forwardStack.Pop();
            NavigateTo(next, addToHistory: false);
        }
    }

    private void UpButton_Click(object sender, RoutedEventArgs e)
    {
        var parent = Directory.GetParent(_currentDirectory);
        if (parent != null && parent.FullName.StartsWith(_storageService.WorkspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            NavigateTo(parent.FullName);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshCurrentFolder();
    }

    private void AddressTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NavigateTo(AddressTextBox.Text);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter(SearchBox.Text);
    }

    private void UpdateHistoryButtons()
    {
        BackButton.IsEnabled = _backStack.Count > 0;
        ForwardButton.IsEnabled = _forwardStack.Count > 0;
    }

    private void RenderTabs()
    {
        TabsContainer.Children.Clear();
        for (int i = 0; i < _tabs.Count; i++)
        {
            var tab = _tabs[i];
            var isSelected = (i == _activeTabIndex);

            var tabBtn = new Button
            {
                Content = tab.Title,
                Style = TryFindResource(isSelected ? "NexusButtonPrimary" : "NexusButtonGhost") as Style,
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(2, 0, 2, 0),
                FontSize = 12
            };

            var index = i;
            tabBtn.Click += (s, e) =>
            {
                _activeTabIndex = index;
                RenderTabs();
                NavigateTo(_tabs[index].Path, addToHistory: false);
            };

            TabsContainer.Children.Add(tabBtn);
        }
    }

    private void NewTabButton_Click(object sender, RoutedEventArgs e)
    {
        _tabs.Add(new ExplorerTab { Title = "Home", Path = _storageService.WorkspaceRoot });
        _activeTabIndex = _tabs.Count - 1;
        RenderTabs();
        NavigateTo(_storageService.WorkspaceRoot, addToHistory: false);
    }

    private void Sidebar_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is ListBoxItem item && item.Tag is string tag)
        {
            switch (tag)
            {
                case "Home":
                    NavigateTo(_storageService.WorkspaceRoot);
                    break;
                case "Desktop":
                    NavigateTo(_storageService.DesktopRoot);
                    break;
                case "Documents":
                    NavigateTo(_storageService.DocumentsRoot);
                    break;
                case "Downloads":
                    NavigateTo(_storageService.DownloadsRoot);
                    break;
                case "Pictures":
                    NavigateTo(Path.Combine(_storageService.WorkspaceRoot, "Pictures"));
                    break;
                case "Music":
                    NavigateTo(Path.Combine(_storageService.WorkspaceRoot, "Music"));
                    break;
                case "Videos":
                    NavigateTo(Path.Combine(_storageService.WorkspaceRoot, "Videos"));
                    break;
                case "HostDrop":
                    NavigateTo(_storageService.WorkspaceRoot);
                    StatusMessageTextBlock.Text = "Drop zone ready. Drag and drop any file from Windows here.";
                    break;
            }
        }
    }

    private async void NewFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var folderName = "New Folder";
        int suffix = 1;
        while (_storageService.Exists(Path.Combine(_currentDirectory, folderName)))
        {
            folderName = $"New Folder ({suffix++})";
        }

        await _storageService.CreateFolderAsync(_currentDirectory, folderName);
        RefreshCurrentFolder();
    }

    private void ViewGridButton_Click(object sender, RoutedEventArgs e)
    {
        FilesGridScrollViewer.Visibility = Visibility.Visible;
        FilesListView.Visibility = Visibility.Collapsed;
    }

    private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        FilesGridScrollViewer.Visibility = Visibility.Collapsed;
        FilesListView.Visibility = Visibility.Visible;
    }

    private void FilesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FilesListView.SelectedItem is NexusStorageItem item)
        {
            HandleItemActivation(item);
        }
    }

    private void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilesListView.SelectedItem is NexusStorageItem item)
        {
            StatusMessageTextBlock.Text = $"{item.Name} - {item.FormattedSize} (Modified {item.DateModified:g})";
        }
    }

    #endregion
}
