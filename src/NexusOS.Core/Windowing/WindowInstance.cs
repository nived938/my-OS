using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NexusOS.Core.Windowing;

public class WindowInstance : IWindowInstance, INotifyPropertyChanged
{
    private string _title;
    private string _iconKey;
    private double _left = 100;
    private double _top = 80;
    private double _width = 800;
    private double _height = 550;
    private double _minWidth = 320;
    private double _minHeight = 220;
    private bool _canResize = true;
    private bool _canMaximize = true;
    private bool _canMinimize = true;
    private bool _isActive = true;
    private bool _isAlwaysOnTop = false;
    private int _zIndex = 1;
    private WindowDisplayState _state = WindowDisplayState.Normal;
    private object? _content;

    private double _savedLeft = 100;
    private double _savedTop = 80;
    private double _savedWidth = 800;
    private double _savedHeight = 550;

    public string Id { get; }
    public string AppId { get; }

    public WindowInstance(string appId, string title, string iconKey = "AppIcon", double width = 800, double height = 550)
    {
        Id = Guid.NewGuid().ToString();
        AppId = appId;
        _title = title;
        _iconKey = iconKey;
        _width = width;
        _height = height;
        _savedWidth = width;
        _savedHeight = height;
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string IconKey
    {
        get => _iconKey;
        set => SetProperty(ref _iconKey, value);
    }

    public double Left
    {
        get => _left;
        set => SetProperty(ref _left, value);
    }

    public double Top
    {
        get => _top;
        set => SetProperty(ref _top, value);
    }

    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    public double Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public double MinWidth
    {
        get => _minWidth;
        set => SetProperty(ref _minWidth, value);
    }

    public double MinHeight
    {
        get => _minHeight;
        set => SetProperty(ref _minHeight, value);
    }

    public bool CanResize
    {
        get => _canResize;
        set => SetProperty(ref _canResize, value);
    }

    public bool CanMaximize
    {
        get => _canMaximize;
        set => SetProperty(ref _canMaximize, value);
    }

    public bool CanMinimize
    {
        get => _canMinimize;
        set => SetProperty(ref _canMinimize, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool IsAlwaysOnTop
    {
        get => _isAlwaysOnTop;
        set => SetProperty(ref _isAlwaysOnTop, value);
    }

    public int ZIndex
    {
        get => _zIndex;
        set => SetProperty(ref _zIndex, value);
    }

    public WindowDisplayState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    public object? Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    public void SaveNormalBounds()
    {
        if (State == WindowDisplayState.Normal)
        {
            _savedLeft = _left;
            _savedTop = _top;
            _savedWidth = _width;
            _savedHeight = _height;
        }
    }

    public (double Left, double Top, double Width, double Height) GetSavedNormalBounds()
    {
        return (_savedLeft, _savedTop, _savedWidth, _savedHeight);
    }

    public void RestoreBounds(double left, double top, double width, double height)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
