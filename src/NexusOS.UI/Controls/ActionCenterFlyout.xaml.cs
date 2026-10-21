using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NexusOS.Core.Notifications;

namespace NexusOS.UI.Controls;

public partial class ActionCenterFlyout : UserControl
{
    private INotificationService? _notificationService;

    public ActionCenterFlyout()
    {
        InitializeComponent();
        CalendarDateText.Text = DateTime.Now.ToString("dddd, MMMM d");
    }

    public void Initialize(INotificationService notificationService)
    {
        _notificationService = notificationService;
        _notificationService.NotificationReceived += OnNotificationUpdated;
        _notificationService.NotificationDismissed += OnNotificationUpdated;
        _notificationService.NotificationsCleared += RefreshNotifications;

        RefreshNotifications();
    }

    private void OnNotificationUpdated(NexusNotification notif)
    {
        Dispatcher.Invoke(RefreshNotifications);
    }

    private void RefreshNotifications()
    {
        NotificationsListContainer.Children.Clear();
        if (_notificationService == null || _notificationService.Notifications.Count == 0)
        {
            NotificationsListContainer.Children.Add(new TextBlock
            {
                Text = "No new notifications",
                Foreground = TryFindResource("NexusTextMutedBrush") as Brush ?? Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 30, 0, 30)
            });
            return;
        }

        foreach (var notif in _notificationService.Notifications)
        {
            var card = CreateNotificationCard(notif);
            NotificationsListContainer.Children.Add(card);
        }
    }

    private Border CreateNotificationCard(NexusNotification notif)
    {
        var card = new Border
        {
            Background = TryFindResource("NexusSurfaceBrush") as Brush ?? Brushes.DarkSlateGray,
            BorderBrush = TryFindResource("NexusBorderSubtleBrush") as Brush ?? Brushes.DimGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header: App name, Time, Dismiss button
        var header = new DockPanel();
        var appName = new TextBlock
        {
            Text = notif.SourceApp,
            FontSize = 11,
            Foreground = TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue,
            FontWeight = FontWeights.SemiBold
        };
        DockPanel.SetDock(appName, Dock.Left);

        var dismissBtn = new Button
        {
            Content = "x",
            Style = TryFindResource("NexusButtonGhost") as Style,
            Padding = new Thickness(4, 0, 4, 0),
            FontSize = 11,
            BorderThickness = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        dismissBtn.Click += (s, e) => _notificationService?.Dismiss(notif.Id);
        DockPanel.SetDock(dismissBtn, Dock.Right);

        var timeText = new TextBlock
        {
            Text = notif.Timestamp.ToString("t"),
            FontSize = 10.5,
            Foreground = TryFindResource("NexusTextMutedBrush") as Brush ?? Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 8, 0)
        };
        DockPanel.SetDock(timeText, Dock.Right);

        header.Children.Add(appName);
        header.Children.Add(dismissBtn);
        header.Children.Add(timeText);
        Grid.SetRow(header, 0);

        // Content: Title & Message
        var body = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        var title = new TextBlock
        {
            Text = notif.Title,
            FontWeight = FontWeights.SemiBold,
            FontSize = 12.5,
            TextWrapping = TextWrapping.Wrap
        };
        var msg = new TextBlock
        {
            Text = notif.Message,
            FontSize = 12,
            Foreground = TryFindResource("NexusTextSecondaryBrush") as Brush ?? Brushes.Silver,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        };
        body.Children.Add(title);
        body.Children.Add(msg);
        Grid.SetRow(body, 1);

        grid.Children.Add(header);
        grid.Children.Add(body);
        card.Child = grid;

        if (notif.OnClick != null)
        {
            card.Cursor = System.Windows.Input.Cursors.Hand;
            card.MouseLeftButtonUp += (s, e) => notif.OnClick?.Invoke();
        }

        return card;
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        _notificationService?.ClearAll();
    }
}
