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
    private readonly string _executablePath;

    public MenuCommandService(
        IPetCommandTarget window,
        AppSettings settings,
        ISettingsStore settingsService,
        IStartupRegistration startupService,
        IApplicationLogger logger,
        IUserNotificationService notifications,
        IApplicationShutdown applicationShutdown,
        string executablePath)
    {
        _window = window;
        Settings = settings;
        _settingsService = settingsService;
        _startupService = startupService;
        _logger = logger;
        _notifications = notifications;
        _applicationShutdown = applicationShutdown;
        _executablePath = executablePath;
    }

    public AppSettings Settings { get; }

    public event Action? SettingsChanged;

    public event Action? SettingsRequested;

    public void ShowOrRestore()
    {
        _window.ShowOrRestore();
    }

    public void ShowSettings()
    {
        SettingsRequested?.Invoke();
    }

    public void ToggleTopmost()
    {
        ApplyAndSave(settings => settings.Topmost = !settings.Topmost, "Always on top setting changed.");
    }

    public void ToggleClickThrough()
    {
        ApplyAndSave(settings => settings.ClickThrough = !settings.ClickThrough, "Mouse click-through setting changed.");
    }

    public void ToggleActiveMovement()
    {
        ApplyAndSave(settings => settings.ActiveMovement = !settings.ActiveMovement, "Active movement setting changed.");
    }

    public void TogglePushCursor()
    {
        ApplyAndSave(settings => settings.PushCursor = !settings.PushCursor, "Push cursor setting changed.");
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

    public void SetThemeMode(AppThemeMode mode)
    {
        if (!Enum.IsDefined(mode) || Settings.ThemeMode == mode)
        {
            return;
        }

        ApplyAndSave(settings => settings.ThemeMode = mode, $"Settings theme changed to {mode}.", applyToPet: false);
    }

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
