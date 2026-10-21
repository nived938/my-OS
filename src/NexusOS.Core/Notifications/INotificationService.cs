namespace NexusOS.Core.Notifications;

public class NexusNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string SourceApp { get; set; } = "Nexus OS";
    public string IconKey { get; set; } = "Info";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsRead { get; set; } = false;
    public Action? OnClick { get; set; }
}

public interface INotificationService
{
    IReadOnlyList<NexusNotification> Notifications { get; }
    event Action<NexusNotification>? NotificationReceived;
    event Action<NexusNotification>? NotificationDismissed;
    event Action? NotificationsCleared;

    void Send(string title, string message, string sourceApp = "Nexus OS", string iconKey = "Info", Action? onClick = null);
    void Dismiss(string id);
    void ClearAll();
}

public class NotificationService : INotificationService
{
    private readonly List<NexusNotification> _notifications = new();

    public IReadOnlyList<NexusNotification> Notifications => _notifications.AsReadOnly();

    public event Action<NexusNotification>? NotificationReceived;
    public event Action<NexusNotification>? NotificationDismissed;
    public event Action? NotificationsCleared;

    public void Send(string title, string message, string sourceApp = "Nexus OS", string iconKey = "Info", Action? onClick = null)
    {
        var notif = new NexusNotification
        {
            Title = title,
            Message = message,
            SourceApp = sourceApp,
            IconKey = iconKey,
            OnClick = onClick,
            Timestamp = DateTime.Now
        };

        _notifications.Insert(0, notif);
        NotificationReceived?.Invoke(notif);
    }

    public void Dismiss(string id)
    {
        var notif = _notifications.FirstOrDefault(n => n.Id == id);
        if (notif != null)
        {
            _notifications.Remove(notif);
            NotificationDismissed?.Invoke(notif);
        }
    }

    public void ClearAll()
    {
        _notifications.Clear();
        NotificationsCleared?.Invoke();
    }
}
