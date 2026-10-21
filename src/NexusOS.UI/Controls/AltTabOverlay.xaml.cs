using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NexusOS.Core.Windowing;
using ShapePath = System.Windows.Shapes.Path;

namespace NexusOS.UI.Controls;

public partial class AltTabOverlay : UserControl
{
    private IWindowManager? _windowManager;
    private int _selectedIndex = 0;
    private List<IWindowInstance> _currentList = new();

    public event Action<IWindowInstance>? WindowSelected;

    public AltTabOverlay()
    {
        InitializeComponent();
    }

    public void ShowSwitcher(IWindowManager windowManager)
    {
        _windowManager = windowManager;
        _currentList = _windowManager.OpenWindows
            .Where(w => w.State != WindowDisplayState.Minimized)
            .OrderByDescending(w => w.ZIndex)
            .ToList();

        if (_currentList.Count == 0) return;

        // Default to second item if multiple windows, or first if only one
        _selectedIndex = _currentList.Count > 1 ? 1 : 0;
        RenderItems();
        Visibility = Visibility.Visible;
    }

    public void CycleNext()
    {
        if (_currentList.Count == 0) return;
        _selectedIndex = (_selectedIndex + 1) % _currentList.Count;
        RenderItems();
    }

    public void CyclePrevious()
    {
        if (_currentList.Count == 0) return;
        _selectedIndex = (_selectedIndex - 1 + _currentList.Count) % _currentList.Count;
        RenderItems();
    }

    public void ConfirmSelection()
    {
        Visibility = Visibility.Collapsed;
        if (_selectedIndex >= 0 && _selectedIndex < _currentList.Count)
        {
            var target = _currentList[_selectedIndex];
            _windowManager?.ActivateWindow(target.Id);
            WindowSelected?.Invoke(target);
        }
    }

    private void RenderItems()
    {
        WindowsContainer.Children.Clear();

        for (int i = 0; i < _currentList.Count; i++)
        {
            var win = _currentList[i];
            var isSelected = (i == _selectedIndex);

            var border = new Border
            {
                Width = 110,
                Height = 110,
                Margin = new Thickness(8, 0, 8, 0),
                CornerRadius = new CornerRadius(10),
                Background = isSelected 
                    ? (TryFindResource("NexusSurfaceHoverBrush") as Brush ?? Brushes.SlateGray)
                    : (TryFindResource("NexusSurfaceBrush") as Brush ?? Brushes.DarkSlateGray),
                BorderBrush = isSelected
                    ? (TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue)
                    : (TryFindResource("NexusBorderSubtleBrush") as Brush ?? Brushes.DimGray),
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                Padding = new Thickness(8)
            };

            var stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var icon = new ShapePath
            {
                Data = TryFindResource(GetIconResourceKey(win.IconKey)) as Geometry ?? TryFindResource("IconFolder") as Geometry,
                Fill = TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue,
                Width = 36,
                Height = 36,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var label = new TextBlock
            {
                Text = win.Title,
                FontSize = 11.5,
                FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 95
            };

            stack.Children.Add(icon);
            stack.Children.Add(label);
            border.Child = stack;

            WindowsContainer.Children.Add(border);
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
}
