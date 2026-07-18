using System.Reflection;
using System.Windows;
using CastoPet.Core;

namespace CastoPet;

public partial class App : System.Windows.Application
{
    private readonly AppPaths _paths;
    private readonly CrashReportService _crashReports;
    private readonly LoggingService _logger;
    private readonly CancellationTokenSource _applicationLifetime = new();
    private SingleInstanceService? _singleInstance;
    private TrayService? _tray;
    private PetWindow? _window;
    private SettingsWindowService? _settingsWindow;
    private UpdateCoordinator? _updates;
    private ShortcutService? _shortcutService;
    private WheelCatalogService? _wheelCatalogService;
    private ShortcutDropHandler? _shortcutDropHandler;
    private ShortcutLauncher? _shortcutLauncher;
    private int _crashRecorded;

    public App()
    {
        _paths = new AppPaths();
        _logger = new LoggingService(_paths);
        _crashReports = new CrashReportService(_paths, _logger);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _logger.Info("CastoPet starting.");

        _singleInstance = new SingleInstanceService(_logger);
        if (!_singleInstance.IsPrimaryInstance)
        {
            await _singleInstance.SignalRestoreAsync();
            Shutdown();
            return;
        }

        var settingsService = new SettingsService(_paths, _logger);
        var settings = settingsService.Load();
        var executablePath = Environment.ProcessPath
            ?? Assembly.GetExecutingAssembly().Location;
        var startupService = new StartupService(_logger);
        settings.StartWithWindows = startupService.IsEnabled(executablePath);

        var skinSelectionService = new PetSkinSelectionService(_logger);
        var skin = skinSelectionService.LoadCurrentSkin(settings);
        var assets = new AssetService(_logger, skin);
        _shortcutService = new ShortcutService(_paths, _logger);
        _shortcutService.Load();
        _wheelCatalogService = new WheelCatalogService(skin.Expressions, _shortcutService);
        _shortcutDropHandler = new ShortcutDropHandler(_shortcutService);
        _shortcutLauncher = new ShortcutLauncher(_logger);
        _window = new PetWindow(assets, _logger, _wheelCatalogService, _shortcutService, _shortcutDropHandler, _shortcutLauncher);
        var commands = new MenuCommandService(
            _window,
            settings,
            settingsService,
            startupService,
            _logger,
            executablePath);

        var updateService = new VelopackUpdateService();
        _updates = new UpdateCoordinator(updateService, settings, settingsService.Save, logger: _logger);
        _settingsWindow = new SettingsWindowService(() =>
        {
            var settingsWindow = new SettingsWindow(
                commands,
                _crashReports,
                _updates,
                _shortcutService,
                _shortcutDropHandler,
                _shortcutLauncher)
            {
                Owner = _window,
            };
            return settingsWindow;
        });
        commands.SettingsRequested += _settingsWindow.ShowOrActivate;

        _window.AttachContextMenu(commands);
        _tray = new TrayService(commands);
        _singleInstance.StartRestoreServer(() => Dispatcher.Invoke(commands.ShowOrRestore));

        _window.Show();
        _window.ApplySettings(settings);
        ShowPendingCrashNotification(settings, settingsService);
        _ = CheckForUpdatesAfterStartupAsync(_applicationLifetime.Token);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _applicationLifetime.Cancel();
        _logger.Info("CastoPet shutdown.");
        _tray?.Dispose();
        _settingsWindow?.Dispose();
        _wheelCatalogService?.Dispose();
        _singleInstance?.Dispose();
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        base.OnExit(e);
    }

    private void ShowPendingCrashNotification(AppSettings settings, SettingsService settingsService)
    {
        var report = _crashReports.GetLatestUnacknowledged(settings.LastAcknowledgedCrashId);
        if (report is null)
        {
            return;
        }

        var notification = new CrashNotificationWindow(
            () => _crashReports.OpenReportsDirectory(),
            () =>
            {
                settings.LastAcknowledgedCrashId = report.Id;
                settingsService.Save(settings);
            });
        notification.Show();
        notification.Activate();
    }

    private void OnDispatcherUnhandledException(
        object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        RecordCrashOnce(e.Exception);
    }

    private void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception
            ?? new InvalidOperationException($"Unhandled non-Exception object: {e.ExceptionObject}");
        RecordCrashOnce(exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (RecordCrashOnce(e.Exception))
        {
            e.SetObserved();
        }
    }

    private bool RecordCrashOnce(Exception exception)
    {
        if (Interlocked.Exchange(ref _crashRecorded, 1) != 0)
        {
            return false;
        }

        return _crashReports.TryWriteReport(exception, out _);
    }

    private async Task CheckForUpdatesAfterStartupAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            if (_updates is null)
            {
                return;
            }

            var result = await _updates.CheckAsync(manual: false, cancellationToken);
            if (result.Status != UpdateCheckStatus.Available || result.AvailableUpdate is null)
            {
                return;
            }

            var notes = string.IsNullOrWhiteSpace(result.AvailableUpdate.ReleaseNotes)
                ? "此版本没有发布说明。"
                : result.AvailableUpdate.ReleaseNotes;
            if (notes.Length > 600)
            {
                notes = notes[..600] + "...";
            }

            var choice = System.Windows.MessageBox.Show(
                $"CastoPet {result.AvailableUpdate.Version} 已可用。\n\n{notes}\n\n是否立即更新？",
                "CastoPet 更新",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (choice == MessageBoxResult.Yes)
            {
                var downloaded = await _updates.DownloadUpdatesAsync(result.AvailableUpdate, cancellationToken: cancellationToken);
                if (!downloaded)
                {
                    System.Windows.MessageBox.Show(
                        "更新下载失败，当前版本不会受到影响。你可以稍后在设置中重试。",
                        "CastoPet 更新",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                _updates.ApplyUpdatesAndRestart(result.AvailableUpdate);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.Error("Automatic update workflow failed.", ex);
        }
    }
}
