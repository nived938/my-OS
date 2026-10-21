namespace NexusOS.Core.Apps;

public enum AppCategory
{
    System,
    Productivity,
    Internet,
    Development,
    Games,
    Utilities,
    Customization
}

public interface IApp
{
    string Id { get; }
    string Name { get; }
    string DisplayName { get; }
    string Description { get; }
    string Version { get; }
    string Publisher { get; }
    AppCategory Category { get; }
    string IconKey { get; }
    bool IsInstalled { get; set; }
    bool IsPinnedToTaskbar { get; set; }
    bool IsPinnedToStart { get; set; }

    object CreateView();
}

public interface IAppRegistry
{
    IReadOnlyList<IApp> Apps { get; }
    void Register(IApp app);
    void Unregister(string appId);
    IApp? FindById(string appId);
    IEnumerable<IApp> Search(string query);
}

public class AppRegistry : IAppRegistry
{
    private readonly List<IApp> _apps = new();

    public IReadOnlyList<IApp> Apps => _apps.AsReadOnly();

    public void Register(IApp app)
    {
        if (_apps.Any(a => a.Id == app.Id))
            return;
        _apps.Add(app);
    }

    public void Unregister(string appId)
    {
        var app = _apps.FirstOrDefault(a => a.Id == appId);
        if (app != null)
        {
            _apps.Remove(app);
        }
    }

    public IApp? FindById(string appId) => _apps.FirstOrDefault(a => a.Id.Equals(appId, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<IApp> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _apps;

        return _apps.Where(a =>
            a.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            a.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            a.Category.ToString().Contains(query, StringComparison.OrdinalIgnoreCase));
    }
}
