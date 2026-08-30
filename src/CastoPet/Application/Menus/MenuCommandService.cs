using CastoPet.Application.Diagnostics;
using CastoPet.Application.Settings;
using CastoPet.Core.Settings;

namespace CastoPet.Application.Menus;

public sealed class MenuCommandService
{
    private readonly IPetCommandTarget _window;
    private readonly ISettingsStore _settingsService;
    private readonly IStartupRegistration _startupService;
    private readonly IApplicationLogger _logger;
    private readonly IUserNotificationService _notifications;
    private readonly IApplicationShutdown _applicationShutdown;
    private readonly Action _openCrashReports;
    private readonly Action _checkForUpdates;
    private readonly string _executablePath;

    public MenuCommandService(
        IPetCommandTarget window,
        AppSettings settings,
        ISettingsStore settingsService,
        IStartupRegistration startupService,
        IApplicationLogger logger,
        IUserNotificationService notifications,
        IApplicationShutdown applicationShutdown,
        string executablePath,
        Action openCrashReports,
        Action checkForUpdates)
    {
        _window = window;
        Settings = settings;
        _settingsService = settingsService;
        _startupService = startupService;
        _logger = logger;
        _notifications = notifications;
        _applicationShutdown = applicationShutdown;
        _executablePath = executablePath;
        _openCrashReports = openCrashReports;
        _checkForUpdates = checkForUpdates;
    }

    public AppSettings Settings { get; }

    public event Action? SettingsChanged;

    public void ShowOrRestore()
    {
        _window.ShowOrRestore();
    }

    public void ToggleTopmost()
    {
        ApplyAndSave(settings => settings.Topmost = !settings.Topmost, "Always on top setting changed.");
    }

    public void ToggleClickThrough()
    {
        ApplyAndSave(settings => settings.ClickThrough = !settings.ClickThrough, "Mouse click-through setting changed.");
    }

    public void ToggleShowInTaskbar()
    {
        ApplyAndSave(settings => settings.ShowInTaskbar = !settings.ShowInTaskbar, "Taskbar visibility setting changed.");
    }

    public void ToggleStartWithWindows()
    {
        var target = !Settings.StartWithWindows;
        if (!_startupService.SetEnabled(target, _executablePath))
        {
            _notifications.ShowWarning(
                "CastoPet 无法更新开机自启动设置。",
                "CastoPet");
            return;
        }

        if (!ApplyAndSave(settings => settings.StartWithWindows = target, "Start with Windows setting changed."))
        {
            _startupService.SetEnabled(!target, _executablePath);
        }
    }

    public void OpenCrashReports() => _openCrashReports();

    public void CheckForUpdates() => _checkForUpdates();

    public void Exit()
    {
        _logger.Info("CastoPet exiting.");
        _applicationShutdown.Shutdown();
    }

    private bool ApplyAndSave(
        Action<AppSettings> mutation,
        string logMessage,
        bool applyToPet = true)
    {
        if (!SettingsTransaction.TryApply(Settings, mutation, _settingsService.Save))
        {
            SettingsChanged?.Invoke();
            _notifications.ShowWarning(
                "CastoPet 无法保存设置，修改已撤销。",
                "CastoPet");
            return false;
        }

        if (applyToPet)
        {
            _window.ApplySettings(Settings);
        }

        _logger.Info(logMessage);
        SettingsChanged?.Invoke();
        return true;
    }
}
