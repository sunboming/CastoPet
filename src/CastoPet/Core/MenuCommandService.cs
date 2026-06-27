using CastoPet;
using Wpf = System.Windows;

namespace CastoPet.Core;

public sealed class MenuCommandService
{
    private readonly PetWindow _window;
    private readonly SettingsService _settingsService;
    private readonly StartupService _startupService;
    private readonly LoggingService _logger;
    private readonly string _executablePath;

    public MenuCommandService(
        PetWindow window,
        AppSettings settings,
        SettingsService settingsService,
        StartupService startupService,
        LoggingService logger,
        string executablePath)
    {
        _window = window;
        Settings = settings;
        _settingsService = settingsService;
        _startupService = startupService;
        _logger = logger;
        _executablePath = executablePath;
    }

    public AppSettings Settings { get; }

    public event Action? SettingsChanged;

    public void ShowOrRestore()
    {
        _window.ShowOrRestore();
    }

    public void ToggleTopmost()
    {
        Settings.Topmost = !Settings.Topmost;
        ApplyAndSave("Always on top setting changed.");
    }

    public void ToggleClickThrough()
    {
        Settings.ClickThrough = !Settings.ClickThrough;
        ApplyAndSave("Mouse click-through setting changed.");
    }

    public void ToggleActiveMovement()
    {
        Settings.ActiveMovement = !Settings.ActiveMovement;
        ApplyAndSave("Active movement setting changed.");
    }

    public void ToggleShowInTaskbar()
    {
        Settings.ShowInTaskbar = !Settings.ShowInTaskbar;
        ApplyAndSave("Taskbar visibility setting changed.");
    }

    public void ToggleStartWithWindows()
    {
        var target = !Settings.StartWithWindows;
        if (!_startupService.SetEnabled(target, _executablePath))
        {
            Wpf.MessageBox.Show(
                "CastoPet 无法更新开机自启动设置。",
                "CastoPet",
                Wpf.MessageBoxButton.OK,
                Wpf.MessageBoxImage.Warning);
            return;
        }

        Settings.StartWithWindows = target;
        ApplyAndSave("Start with Windows setting changed.");
    }

    public void Exit()
    {
        _logger.Info("CastoPet exiting.");
        Wpf.Application.Current.Shutdown();
    }

    private void ApplyAndSave(string logMessage)
    {
        _window.ApplySettings(Settings);
        _settingsService.Save(Settings);
        _logger.Info(logMessage);
        SettingsChanged?.Invoke();
    }
}
