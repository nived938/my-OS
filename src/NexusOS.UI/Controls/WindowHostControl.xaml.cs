using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NexusOS.Core.Windowing;

namespace NexusOS.UI.Controls;

public partial class WindowHostControl : UserControl
{
    private IWindowInstance? _window;
    private IWindowManager? _windowManager;
    private Canvas? _parentCanvas;

    private bool _isDragging = false;
    private Point _dragStartPoint;
    private Point _windowStartLocation;

    private bool _isResizing = false;
    private string _resizeDirection = string.Empty;
    private Point _resizeStartPoint;
    private Rect _resizeStartBounds;

    public WindowHostControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _parentCanvas = VisualTreeHelper.GetParent(this) as Canvas;
        if (DataContext is IWindowInstance win)
        {
            SetWindow(win);
        }
    }

    public void Initialize(IWindowInstance window, IWindowManager windowManager)
    {
        _window = window;
        _windowManager = windowManager;
        DataContext = window;
        SetWindow(window);
    }

    private void SetWindow(IWindowInstance window)
    {
        _window = window;
        TitleTextBlock.Text = window.Title;
        WindowContentPresenter.Content = window.Content;

        UpdateZIndexAndPosition();
        UpdateStateVisuals();
    }

    public void UpdateZIndexAndPosition()
    {
        if (_window == null) return;

        Canvas.SetLeft(this, _window.Left);
        Canvas.SetTop(this, _window.Top);
        Width = _window.Width;
        Height = _window.Height;
        Panel.SetZIndex(this, _window.ZIndex);

        Visibility = _window.State == WindowDisplayState.Minimized ? Visibility.Collapsed : Visibility.Visible;
    }

    public void UpdateStateVisuals()
    {
        if (_window == null) return;

        // Toggle Maximize / Restore icon
        if (_window.State == WindowDisplayState.Maximized)
        {
            MaximizeIcon.Data = TryFindResource("IconRestore") as Geometry ?? MaximizeIcon.Data;
            WindowBorder.CornerRadius = new CornerRadius(0);
        }
        else
        {
            MaximizeIcon.Data = TryFindResource("IconMaximize") as Geometry ?? MaximizeIcon.Data;
            WindowBorder.CornerRadius = TryFindResource("WindowCornerRadius") as CornerRadius? ?? new CornerRadius(10);
        }

        WindowBorder.BorderBrush = _window.IsActive
            ? (TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue)
            : (TryFindResource("NexusBorderBrush") as Brush ?? Brushes.Gray);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_window == null || _windowManager == null) return;

        _windowManager.ActivateWindow(_window.Id);

        if (e.ClickCount == 2)
        {
            // Double-click toggles maximize / restore
            if (_window.State == WindowDisplayState.Maximized)
            {
                _windowManager.RestoreWindow(_window.Id);
            }
            else
            {
                _windowManager.MaximizeWindow(_window.Id);
            }
            UpdateZIndexAndPosition();
            UpdateStateVisuals();
            return;
        }

        if (_window.State == WindowDisplayState.Maximized)
        {
            // Restore before drag if dragging from maximized state
            _windowManager.RestoreWindow(_window.Id);
            UpdateZIndexAndPosition();
            UpdateStateVisuals();
        }

        _isDragging = true;
        _dragStartPoint = e.GetPosition(_parentCanvas);
        _windowStartLocation = new Point(_window.Left, _window.Top);
        TitleBar.CaptureMouse();
    }

    private void TitleBar_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _window == null || _parentCanvas == null) return;

        var currentPoint = e.GetPosition(_parentCanvas);
        var deltaX = currentPoint.X - _dragStartPoint.X;
        var deltaY = currentPoint.Y - _dragStartPoint.Y;

        var newLeft = _windowStartLocation.X + deltaX;
        var newTop = _windowStartLocation.Y + deltaY;

        // Clamp to prevent moving entirely off-screen
        var canvasWidth = _parentCanvas.ActualWidth;
        var canvasHeight = _parentCanvas.ActualHeight;

        newTop = Math.Max(0, Math.Min(newTop, canvasHeight - 40));
        newLeft = Math.Max(-_window.Width + 80, Math.Min(newLeft, canvasWidth - 80));

        _window.Left = newLeft;
        _window.Top = newTop;

        Canvas.SetLeft(this, newLeft);
        Canvas.SetTop(this, newTop);
    }

    private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            TitleBar.ReleaseMouseCapture();

            // Check for window snapping triggers on drag release
            if (_window != null && _parentCanvas != null && _windowManager != null)
            {
                var canvasWidth = _parentCanvas.ActualWidth;
                var canvasHeight = _parentCanvas.ActualHeight;

                if (_window.Top <= 5)
                {
                    // Dragged to top edge -> Maximize
                    _windowManager.MaximizeWindow(_window.Id);
                    UpdateZIndexAndPosition();
                    UpdateStateVisuals();
                }
                else if (_window.Left <= 5)
                {
                    // Snap Left Half
                    _windowManager.SnapWindow(_window.Id, SnapPosition.LeftHalf, canvasWidth, canvasHeight);
                    UpdateZIndexAndPosition();
                    UpdateStateVisuals();
                }
                else if (_window.Left + _window.Width >= canvasWidth - 5)
                {
                    // Snap Right Half
                    _windowManager.SnapWindow(_window.Id, SnapPosition.RightHalf, canvasWidth, canvasHeight);
                    UpdateZIndexAndPosition();
                    UpdateStateVisuals();
                }
            }
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_window != null && _windowManager != null)
        {
            _windowManager.MinimizeWindow(_window.Id);
            UpdateZIndexAndPosition();
        }
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_window == null || _windowManager == null) return;

        if (_window.State == WindowDisplayState.Maximized)
        {
            _windowManager.RestoreWindow(_window.Id);
        }
        else
        {
            _windowManager.MaximizeWindow(_window.Id);
        }

        UpdateZIndexAndPosition();
        UpdateStateVisuals();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_window != null && _windowManager != null)
        {
            _windowManager.CloseWindow(_window.Id);
        }
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        if (_window != null)
        {
            _window.IsAlwaysOnTop = !_window.IsAlwaysOnTop;
            PinButton.Opacity = _window.IsAlwaysOnTop ? 1.0 : 0.5;
        }
    }

    private void Resize_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_window == null || !_window.CanResize || _parentCanvas == null) return;
        if (_window.State == WindowDisplayState.Maximized) return;

        if (sender is FrameworkElement element)
        {
            _isResizing = true;
            _resizeDirection = element.Name.Replace("Resize", "");
            _resizeStartPoint = e.GetPosition(_parentCanvas);
            _resizeStartBounds = new Rect(_window.Left, _window.Top, _window.Width, _window.Height);
            element.CaptureMouse();
            e.Handled = true;
        }
    }

    private void Resize_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isResizing || _window == null || _parentCanvas == null) return;

        var current = e.GetPosition(_parentCanvas);
        var deltaX = current.X - _resizeStartPoint.X;
        var deltaY = current.Y - _resizeStartPoint.Y;

        var newLeft = _resizeStartBounds.Left;
        var newTop = _resizeStartBounds.Top;
        var newWidth = _resizeStartBounds.Width;
        var newHeight = _resizeStartBounds.Height;

        if (_resizeDirection.Contains("Right"))
        {
            newWidth = Math.Max(_window.MinWidth, _resizeStartBounds.Width + deltaX);
        }
        else if (_resizeDirection.Contains("Left"))
        {
            var potentialWidth = _resizeStartBounds.Width - deltaX;
            if (potentialWidth >= _window.MinWidth)
            {
                newLeft = _resizeStartBounds.Left + deltaX;
                newWidth = potentialWidth;
            }
        }

        if (_resizeDirection.Contains("Bottom"))
        {
            newHeight = Math.Max(_window.MinHeight, _resizeStartBounds.Height + deltaY);
        }
        else if (_resizeDirection.Contains("Top"))
        {
            var potentialHeight = _resizeStartBounds.Height - deltaY;
            if (potentialHeight >= _window.MinHeight)
            {
                newTop = _resizeStartBounds.Top + deltaY;
                newHeight = potentialHeight;
            }
        }

        _window.Left = newLeft;
        _window.Top = newTop;
        _window.Width = newWidth;
        _window.Height = newHeight;

        UpdateZIndexAndPosition();
    }

    private void Resize_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isResizing)
        {
            _isResizing = false;
            if (sender is FrameworkElement element)
            {
                element.ReleaseMouseCapture();
            }
        }
    }
}
