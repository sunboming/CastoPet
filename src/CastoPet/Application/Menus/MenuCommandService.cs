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

    public void ToggleInputReactiveMode()
    {
        ApplyAndSave(settings => settings.InputReactiveMode = !settings.InputReactiveMode, "Input reactive mode setting changed.");
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
            Wpf.MessageBox.Show(
                "CastoPet 无法更新开机自启动设置。",
                "CastoPet",
                Wpf.MessageBoxButton.OK,
                Wpf.MessageBoxImage.Warning);
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
        Wpf.Application.Current.Shutdown();
    }

    private bool ApplyAndSave(
        Action<AppSettings> mutation,
        string logMessage,
        bool applyToPet = true)
    {
        if (!SettingsTransaction.TryApply(Settings, mutation, _settingsService.Save))
        {
            SettingsChanged?.Invoke();
            Wpf.MessageBox.Show(
                "CastoPet 无法保存设置，修改已撤销。",
                "CastoPet",
                Wpf.MessageBoxButton.OK,
                Wpf.MessageBoxImage.Warning);
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
