using System.Reflection;
using System.Windows;
using CastoPet.Core;

namespace CastoPet;

public partial class App : System.Windows.Application
{
    private LoggingService? _logger;
    private SingleInstanceService? _singleInstance;
    private TrayService? _tray;
    private PetWindow? _window;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var paths = new AppPaths();
        _logger = new LoggingService(paths);
        _logger.Info("CastoPet starting.");

        _singleInstance = new SingleInstanceService(_logger);
        if (!_singleInstance.IsPrimaryInstance)
        {
            await _singleInstance.SignalRestoreAsync();
            Shutdown();
            return;
        }

        var settingsService = new SettingsService(paths, _logger);
        var settings = settingsService.Load();
        var executablePath = Environment.ProcessPath
            ?? Assembly.GetExecutingAssembly().Location;
        var startupService = new StartupService(_logger);
        settings.StartWithWindows = startupService.IsEnabled(executablePath);

        var skinSelectionService = new PetSkinSelectionService(_logger);
        var skin = skinSelectionService.LoadCurrentSkin(settings);
        var assets = new AssetService(_logger, skin);
        _window = new PetWindow(assets, _logger);
        var commands = new MenuCommandService(
            _window,
            settings,
            settingsService,
            startupService,
            _logger,
            executablePath);

        _window.AttachContextMenu(commands);
        _tray = new TrayService(commands);
        _singleInstance.StartRestoreServer(() => Dispatcher.Invoke(commands.ShowOrRestore));

        _window.Show();
        _window.ApplySettings(settings);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.Info("CastoPet shutdown.");
        _tray?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
