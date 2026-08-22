namespace CastoPet.Application.Settings;

public interface ISettingsWindow
{
    event EventHandler? Closed;

    bool IsVisible { get; }

    void Show();

    bool Activate();

    void Close();
}

public sealed class SettingsWindowService : IDisposable
{
    private readonly Func<ISettingsWindow> _windowFactory;
    private ISettingsWindow? _window;

    public SettingsWindowService(Func<ISettingsWindow> windowFactory)
    {
        _windowFactory = windowFactory;
    }

    public void ShowOrActivate()
    {
        if (_window is null)
        {
            _window = _windowFactory();
            _window.Closed += OnWindowClosed;
        }

        if (!_window.IsVisible)
        {
            _window.Show();
        }

        _window.Activate();
    }

    public void Dispose()
    {
        if (_window is null)
        {
            return;
        }

        var window = _window;
        window.Closed -= OnWindowClosed;
        _window = null;
        window.Close();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (_window is not null)
        {
            _window.Closed -= OnWindowClosed;
            _window = null;
        }
    }
}
