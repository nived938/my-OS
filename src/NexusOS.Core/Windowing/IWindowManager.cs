namespace NexusOS.Core.Windowing;

public enum WindowDisplayState
{
    Normal,
    Minimized,
    Maximized,
    SnappedLeft,
    SnappedRight,
    SnappedTopLeft,
    SnappedTopRight,
    SnappedBottomLeft,
    SnappedBottomRight,
    SnappedTopHalf,
    SnappedBottomHalf,
    FullScreen
}

public enum SnapPosition
{
    LeftHalf,
    RightHalf,
    TopHalf,
    BottomHalf,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public interface IWindowInstance
{
    string Id { get; }
    string Title { get; set; }
    string IconKey { get; set; }
    string AppId { get; }
    double Left { get; set; }
    double Top { get; set; }
    double Width { get; set; }
    double Height { get; set; }
    double MinWidth { get; set; }
    double MinHeight { get; set; }
    bool CanResize { get; set; }
    bool CanMaximize { get; set; }
    bool CanMinimize { get; set; }
    bool IsActive { get; set; }
    bool IsAlwaysOnTop { get; set; }
    int ZIndex { get; set; }
    WindowDisplayState State { get; set; }
    object? Content { get; set; }

    void RestoreBounds(double left, double top, double width, double height);
    void SaveNormalBounds();
    (double Left, double Top, double Width, double Height) GetSavedNormalBounds();
}

public interface IWindowManager
{
    IReadOnlyList<IWindowInstance> OpenWindows { get; }
    IWindowInstance? ActiveWindow { get; }
    
    event Action<IWindowInstance>? WindowOpened;
    event Action<IWindowInstance>? WindowClosed;
    event Action<IWindowInstance>? WindowActivated;
    event Action<IWindowInstance>? WindowStateChanged;

    void RegisterWindow(IWindowInstance window);
    void ActivateWindow(string windowId);
    void CloseWindow(string windowId);
    void MinimizeWindow(string windowId);
    void MaximizeWindow(string windowId);
    void RestoreWindow(string windowId);
    void SnapWindow(string windowId, SnapPosition position, double canvasWidth, double canvasHeight);
    void MinimizeAll();
    void RestoreAll();
    void AltTabNext();
    void AltTabPrevious();
}
