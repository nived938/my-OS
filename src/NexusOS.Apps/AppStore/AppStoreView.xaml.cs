using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NexusOS.Core.Packages;
using ShapePath = System.Windows.Shapes.Path;

namespace NexusOS.Apps.AppStore;

public partial class AppStoreView : UserControl
{
    private readonly IPackageManager _packageManager;
    private List<PackageManifest> _catalog = new();
    private string _selectedCategory = "All";

    public event Action<string>? OpenAppRequested;

    public AppStoreView(IPackageManager packageManager)
    {
        InitializeComponent();
        _packageManager = packageManager;
        Loaded += AppStoreView_Loaded;
    }

    private async void AppStoreView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadCatalogAsync();
    }

    public async Task LoadCatalogAsync()
    {
        _catalog = await _packageManager.GetCatalogAsync();
        FilterAndRender();
    }

    private void FilterAndRender()
    {
        AppCatalogWrapPanel.Children.Clear();

        var query = SearchBox.Text?.Trim() ?? string.Empty;
        var items = _catalog.AsEnumerable();

        if (_selectedCategory != "All")
        {
            items = items.Where(i => i.Category.Equals(_selectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(query))
        {
            items = items.Where(i =>
                i.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                i.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var pkg in items)
        {
            var card = CreatePackageCard(pkg);
            AppCatalogWrapPanel.Children.Add(card);
        }
    }

    private Border CreatePackageCard(PackageManifest pkg)
    {
        var card = new Border
        {
            Width = 248,
            Height = 148,
            Margin = new Thickness(6),
            Background = TryFindResource("NexusSurfaceBrush") as Brush ?? Brushes.DarkSlateGray,
            BorderBrush = TryFindResource("NexusBorderSubtleBrush") as Brush ?? Brushes.DimGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12)
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header: Icon + Name & Publisher
        var header = new DockPanel();
        var icon = new ShapePath
        {
            Data = TryFindResource(GetIconResourceKey(pkg.IconKey)) as Geometry ?? TryFindResource("IconFolder") as Geometry,
            Fill = TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue,
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(0, 0, 10, 0)
        };
        DockPanel.SetDock(icon, Dock.Left);

        var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var nameText = new TextBlock
        {
            Text = pkg.Name,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var pubText = new TextBlock
        {
            Text = $"{pkg.Publisher} • v{pkg.Version}",
            FontSize = 11,
            Foreground = TryFindResource("NexusTextMutedBrush") as Brush ?? Brushes.Gray
        };
        titleStack.Children.Add(nameText);
        titleStack.Children.Add(pubText);
        header.Children.Add(icon);
        header.Children.Add(titleStack);
        Grid.SetRow(header, 0);

        // Description
        var descText = new TextBlock
        {
            Text = pkg.Description,
            FontSize = 11.5,
            Foreground = TryFindResource("NexusTextSecondaryBrush") as Brush ?? Brushes.Silver,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 8, 0, 8),
            MaxHeight = 44
        };
        Grid.SetRow(descText, 1);

        // Footer: Size/Category + Action Button
        var footer = new DockPanel();
        var infoText = new TextBlock
        {
            Text = $"{pkg.Category} • {pkg.SizeMb:F1} MB",
            FontSize = 11,
            Foreground = TryFindResource("NexusTextMutedBrush") as Brush ?? Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center
        };
        DockPanel.SetDock(infoText, Dock.Left);

        var actionBtn = new Button
        {
            Content = pkg.IsInstalled ? "Open" : "Install",
            Style = TryFindResource(pkg.IsInstalled ? "NexusButtonGhost" : "NexusButtonPrimary") as Style,
            Padding = new Thickness(14, 4, 14, 4),
            FontSize = 11.5,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        actionBtn.Click += async (s, e) =>
        {
            if (pkg.IsInstalled)
            {
                OpenAppRequested?.Invoke(pkg.Id);
            }
            else
            {
                actionBtn.IsEnabled = false;
                actionBtn.Content = "Installing...";

                var progress = new Progress<double>(p =>
                {
                    actionBtn.Content = $"{p * 100:0}%";
                });

                var success = await _packageManager.InstallPackageAsync(pkg, progress);
                if (success)
                {
                    actionBtn.Content = "Open";
                    actionBtn.Style = TryFindResource("NexusButtonGhost") as Style;
                    actionBtn.IsEnabled = true;
                }
            }
        };

        DockPanel.SetDock(actionBtn, Dock.Right);
        footer.Children.Add(actionBtn);
        footer.Children.Add(infoText);
        Grid.SetRow(footer, 2);

        grid.Children.Add(header);
        grid.Children.Add(descText);
        grid.Children.Add(footer);
        card.Child = grid;

        return card;
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

    private void Category_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string cat)
        {
            _selectedCategory = cat;

            // Update pill styling
            foreach (var child in CategoryPillsContainer.Children)
            {
                if (child is Button b)
                {
                    b.Style = TryFindResource(b.Tag?.ToString() == cat ? "NexusButtonPrimary" : "NexusButtonGhost") as Style;
                }
            }

            FilterAndRender();
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterAndRender();
    }
}
