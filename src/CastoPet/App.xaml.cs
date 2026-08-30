using System.Reflection;
using System.Windows;
using CastoPet.Core;

using CastoPet.Application.Diagnostics;
using CastoPet.Application.Menus;
using CastoPet.Application.Settings;
using CastoPet.Application.Updates;
using CastoPet.Core.Product;
using CastoPet.Core.Settings;
using CastoPet.Infrastructure.Assets;
using CastoPet.Infrastructure.Diagnostics;
using CastoPet.Infrastructure.Persistence;
using CastoPet.Infrastructure.Platform;
using CastoPet.Infrastructure.Updates;
using CastoPet.Presentation.Services;
using CastoPet.Presentation.Windows;

namespace CastoPet;

public partial class App : System.Windows.Application
{
    private readonly CastoPetProductIdentity _identity;
    private readonly AppPaths _paths;
    private readonly CrashReportService _crashReports;
    private readonly CrashCaptureCoordinator _crashCapture;
    private readonly LoggingService _logger;
    private readonly CancellationTokenSource _applicationLifetime = new();
    private SingleInstanceService? _singleInstance;
    private TrayService? _tray;
    private PetWindow? _window;
    private UpdateCoordinator? _updates;

    public App()
    {
        _identity = CastoPetProductIdentity.Current;
        _paths = AppPaths.ForProduct(_identity);
        _logger = new LoggingService(_paths);
        _crashReports = new CrashReportService(
            _paths,
            _logger,
            buildInfo: CastoPetBuildInfo.Current());
        _crashCapture = new CrashCaptureCoordinator((exception, kind) =>
            _crashReports.TryWriteReport(exception, kind, out _));
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _logger.Info($"{_identity.DisplayName} starting.");

        _singleInstance = new SingleInstanceService(_logger, _identity.InstanceName);
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
        var startupService = new StartupService(_logger, _identity.StartupValueName);
        settings.StartWithWindows = startupService.IsEnabled(executablePath);

        var assets = new AssetService(_logger, BuiltInPetSkins.Castorice);
        _window = new PetWindow(assets, _logger);
        _window.Title = _identity.DisplayName;
        IUpdateService updateService = new VelopackUpdateService();
        _updates = new UpdateCoordinator(updateService, settings, settingsService.Save, logger: _logger);
        var commands = new MenuCommandService(
            _window,
            settings,
            settingsService,
            startupService,
            _logger,
            new WpfUserNotificationService(),
            new WpfApplicationShutdown(),
            executablePath,
            OpenCrashReports,
            () => _ = CheckForUpdatesManuallyAsync(_applicationLifetime.Token));

        _window.AttachContextMenu(commands);
        _tray = new TrayService(commands, _identity);
        _singleInstance.StartRestoreServer(() => Dispatcher.Invoke(commands.ShowOrRestore));

        _window.Show();
        _window.ApplySettings(settings);
        ShowPendingCrashNotification(settings, settingsService);
        _ = CheckForUpdatesAfterStartupAsync(_applicationLifetime.Token);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _applicationLifetime.Cancel();
        _logger.Info($"{_identity.DisplayName} shutdown.");
        _tray?.Dispose();
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
        _crashCapture.TryRecordFatal(e.Exception);
    }

    private void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception
            ?? new InvalidOperationException($"Unhandled non-Exception object: {e.ExceptionObject}");
        _crashCapture.TryRecordFatal(exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _crashCapture.HandleUnobservedTaskException(e);
    }

    private void OpenCrashReports()
    {
        if (!_crashReports.OpenReportsDirectory())
        {
            System.Windows.MessageBox.Show(
                "无法打开崩溃日志目录。",
                "CastoPet",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async Task CheckForUpdatesManuallyAsync(CancellationToken cancellationToken)
    {
        if (_updates is null)
        {
            return;
        }

        try
        {
            var result = await _updates.CheckAsync(manual: true, cancellationToken);
            switch (result.Status)
            {
                case UpdateCheckStatus.Available when result.AvailableUpdate is not null:
                    await PromptAndInstallUpdateAsync(result.AvailableUpdate, cancellationToken);
                    break;
                case UpdateCheckStatus.Current:
                    System.Windows.MessageBox.Show("当前已是最新版本。", "CastoPet 更新", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case UpdateCheckStatus.DevelopmentBuild:
                    System.Windows.MessageBox.Show("开发构建不支持自动更新。", "CastoPet 更新", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case UpdateCheckStatus.Busy:
                    System.Windows.MessageBox.Show("更新检查正在进行。", "CastoPet 更新", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                default:
                    System.Windows.MessageBox.Show("检查更新失败，请稍后重试。", "CastoPet 更新", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.Error("Manual update workflow failed.", ex);
            System.Windows.MessageBox.Show("检查更新失败，请稍后重试。", "CastoPet 更新", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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

            await PromptAndInstallUpdateAsync(result.AvailableUpdate, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.Error("Automatic update workflow failed.", ex);
        }
    }

    private async Task PromptAndInstallUpdateAsync(
        UpdateAvailability update,
        CancellationToken cancellationToken)
    {
        if (_updates is null)
        {
            return;
        }

        var notes = string.IsNullOrWhiteSpace(update.ReleaseNotes)
            ? "此版本没有发布说明。"
            : update.ReleaseNotes;
        if (notes.Length > 600)
        {
            notes = notes[..600] + "...";
        }

        var choice = System.Windows.MessageBox.Show(
            $"CastoPet {update.Version} 已可用。\n\n{notes}\n\n是否立即更新？",
            "CastoPet 更新",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);
        if (choice != MessageBoxResult.Yes)
        {
            return;
        }

        var downloaded = await _updates.DownloadUpdatesAsync(update, cancellationToken: cancellationToken);
        if (!downloaded)
        {
            System.Windows.MessageBox.Show(
                "更新下载失败，当前版本不会受到影响。你可以稍后重试。",
                "CastoPet 更新",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _updates.ApplyUpdatesAndRestart(update);
    }
}
