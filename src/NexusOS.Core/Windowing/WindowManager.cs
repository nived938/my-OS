namespace NexusOS.Core.Windowing;

public class WindowManager : IWindowManager
{
    private readonly List<IWindowInstance> _windows = new();
    private int _highestZIndex = 1;

    public IReadOnlyList<IWindowInstance> OpenWindows => _windows.AsReadOnly();

    public IWindowInstance? ActiveWindow => _windows
        .Where(w => w.State != WindowDisplayState.Minimized)
        .OrderByDescending(w => w.ZIndex)
        .FirstOrDefault();

    public event Action<IWindowInstance>? WindowOpened;
    public event Action<IWindowInstance>? WindowClosed;
    public event Action<IWindowInstance>? WindowActivated;
    public event Action<IWindowInstance>? WindowStateChanged;

    public void RegisterWindow(IWindowInstance window)
    {
        if (_windows.Any(w => w.Id == window.Id))
            return;

        _windows.Add(window);
        BringToFrontInternal(window);
        WindowOpened?.Invoke(window);
    }

    public void ActivateWindow(string windowId)
    {
        var window = _windows.FirstOrDefault(w => w.Id == windowId);
        if (window == null) return;

        if (window.State == WindowDisplayState.Minimized)
        {
            window.State = WindowDisplayState.Normal;
            WindowStateChanged?.Invoke(window);
        }

        BringToFrontInternal(window);
        WindowActivated?.Invoke(window);
    }

    public void CloseWindow(string windowId)
    {
        var window = _windows.FirstOrDefault(w => w.Id == windowId);
        if (window == null) return;

        _windows.Remove(window);
        WindowClosed?.Invoke(window);

        // Activate the next top window
        var nextActive = ActiveWindow;
        if (nextActive != null)
        {
            ActivateWindow(nextActive.Id);
        }
    }

    public void MinimizeWindow(string windowId)
    {
        var window = _windows.FirstOrDefault(w => w.Id == windowId);
        if (window == null) return;

        window.State = WindowDisplayState.Minimized;
        window.IsActive = false;
        WindowStateChanged?.Invoke(window);

        var nextActive = ActiveWindow;
        if (nextActive != null)
        {
            BringToFrontInternal(nextActive);
            WindowActivated?.Invoke(nextActive);
        }
    }

    public void MaximizeWindow(string windowId)
    {
        var window = _windows.FirstOrDefault(w => w.Id == windowId);
        if (window == null || !window.CanMaximize) return;

        if (window.State == WindowDisplayState.Normal)
        {
            window.SaveNormalBounds();
        }

        window.State = WindowDisplayState.Maximized;
        BringToFrontInternal(window);
        WindowStateChanged?.Invoke(window);
    }

    public void RestoreWindow(string windowId)
    {
        var window = _windows.FirstOrDefault(w => w.Id == windowId);
        if (window == null) return;

        var (left, top, width, height) = window.GetSavedNormalBounds();
        window.RestoreBounds(left, top, width, height);
        window.State = WindowDisplayState.Normal;
        BringToFrontInternal(window);
        WindowStateChanged?.Invoke(window);
    }

    public void SnapWindow(string windowId, SnapPosition position, double canvasWidth, double canvasHeight)
    {
        var window = _windows.FirstOrDefault(w => w.Id == windowId);
        if (window == null) return;

        if (window.State == WindowDisplayState.Normal)
        {
            window.SaveNormalBounds();
        }

        switch (position)
        {
            case SnapPosition.LeftHalf:
                window.Left = 0;
                window.Top = 0;
                window.Width = canvasWidth / 2.0;
                window.Height = canvasHeight;
                window.State = WindowDisplayState.SnappedLeft;
                break;

            case SnapPosition.RightHalf:
                window.Left = canvasWidth / 2.0;
                window.Top = 0;
                window.Width = canvasWidth / 2.0;
                window.Height = canvasHeight;
                window.State = WindowDisplayState.SnappedRight;
                break;

            case SnapPosition.TopLeft:
                window.Left = 0;
                window.Top = 0;
                window.Width = canvasWidth / 2.0;
                window.Height = canvasHeight / 2.0;
                window.State = WindowDisplayState.SnappedTopLeft;
                break;

            case SnapPosition.TopRight:
                window.Left = canvasWidth / 2.0;
                window.Top = 0;
                window.Width = canvasWidth / 2.0;
                window.Height = canvasHeight / 2.0;
                window.State = WindowDisplayState.SnappedTopRight;
                break;

            case SnapPosition.BottomLeft:
                window.Left = 0;
                window.Top = canvasHeight / 2.0;
                window.Width = canvasWidth / 2.0;
                window.Height = canvasHeight / 2.0;
                window.State = WindowDisplayState.SnappedBottomLeft;
                break;

            case SnapPosition.BottomRight:
                window.Left = canvasWidth / 2.0;
                window.Top = canvasHeight / 2.0;
                window.Width = canvasWidth / 2.0;
                window.Height = canvasHeight / 2.0;
                window.State = WindowDisplayState.SnappedBottomRight;
                break;

            case SnapPosition.TopHalf:
                window.Left = 0;
                window.Top = 0;
                window.Width = canvasWidth;
                window.Height = canvasHeight / 2.0;
                window.State = WindowDisplayState.SnappedTopHalf;
                break;

            case SnapPosition.BottomHalf:
                window.Left = 0;
                window.Top = canvasHeight / 2.0;
                window.Width = canvasWidth;
                window.Height = canvasHeight / 2.0;
                window.State = WindowDisplayState.SnappedBottomHalf;
                break;
        }

        BringToFrontInternal(window);
        WindowStateChanged?.Invoke(window);
    }

    public void MinimizeAll()
    {
        foreach (var window in _windows.Where(w => w.State != WindowDisplayState.Minimized))
        {
            window.State = WindowDisplayState.Minimized;
            window.IsActive = false;
            WindowStateChanged?.Invoke(window);
        }
    }

    public void RestoreAll()
    {
        foreach (var window in _windows.Where(w => w.State == WindowDisplayState.Minimized))
        {
            window.State = WindowDisplayState.Normal;
            WindowStateChanged?.Invoke(window);
        }

        var top = ActiveWindow;
        if (top != null)
        {
            BringToFrontInternal(top);
        }
    }

    public void AltTabNext()
    {
        var visibleWindows = _windows
            .Where(w => w.State != WindowDisplayState.Minimized)
            .OrderByDescending(w => w.ZIndex)
            .ToList();

        if (visibleWindows.Count <= 1) return;

        // Take the second most recent window and bring it forward
        var target = visibleWindows.Skip(1).FirstOrDefault() ?? visibleWindows.First();
        ActivateWindow(target.Id);
    }

    public void AltTabPrevious()
    {
        var visibleWindows = _windows
            .Where(w => w.State != WindowDisplayState.Minimized)
            .OrderBy(w => w.ZIndex)
            .ToList();

        if (visibleWindows.Count <= 1) return;
        var target = visibleWindows.FirstOrDefault();
        if (target != null)
        {
            ActivateWindow(target.Id);
        }
    }

    private void BringToFrontInternal(IWindowInstance window)
    {
        _highestZIndex++;
        window.ZIndex = _highestZIndex;

        foreach (var w in _windows)
        {
            w.IsActive = (w.Id == window.Id);
        }
    }
}
