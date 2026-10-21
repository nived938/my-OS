namespace NexusOS.Core.Events;

public interface IEventAggregator
{
    void Subscribe<T>(Action<T> action);
    void Unsubscribe<T>(Action<T> action);
    void Publish<T>(T message);
}

public class EventAggregator : IEventAggregator
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly object _lock = new();

    public void Subscribe<T>(Action<T> action)
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _subscribers[type] = list;
            }
            list.Add(action);
        }
    }

    public void Unsubscribe<T>(Action<T> action)
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var list))
            {
                list.Remove(action);
            }
        }
    }

    public void Publish<T>(T message)
    {
        List<Delegate> subscribersCopy;
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var list))
                return;
            subscribersCopy = new List<Delegate>(list);
        }

        foreach (var handler in subscribersCopy)
        {
            if (handler is Action<T> action)
            {
                try
                {
                    action(message);
                }
                catch
                {
                    // Ignore or log event handler errors to avoid crashing bus
                }
            }
        }
    }
}
