using CastoPet.Application.Diagnostics;
using CastoPet.Core.Settings;

namespace CastoPet.Application.Updates;

public sealed class UpdateCoordinator
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(8);

    private readonly IUpdateService _updateService;
    private readonly AppSettings _settings;
    private readonly Func<AppSettings, bool> _saveSettings;
    private readonly Func<DateOnly> _todayProvider;
    private readonly TimeSpan _timeout;
    private readonly IApplicationLogger? _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public UpdateCoordinator(
        IUpdateService updateService,
        AppSettings settings,
        Func<AppSettings, bool> saveSettings,
        Func<DateOnly>? todayProvider = null,
        TimeSpan? timeout = null,
        IApplicationLogger? logger = null)
    {
        _updateService = updateService;
        _settings = settings;
        _saveSettings = saveSettings;
        _todayProvider = todayProvider ?? (() => DateOnly.FromDateTime(DateTime.Now));
        _timeout = timeout ?? DefaultTimeout;
        _logger = logger;
    }

    public string CurrentVersion => _updateService.CurrentVersion;

    public bool IsInstalled => _updateService.IsInstalled;

    public async Task<UpdateCheckResult> CheckAsync(
        bool manual,
        CancellationToken cancellationToken = default)
    {
        var today = _todayProvider();
        if (!UpdateCheckPolicy.ShouldCheck(manual, _settings.LastAutomaticUpdateCheckDate, today))
        {
            return Result(UpdateCheckStatus.Skipped);
        }

        if (!await _gate.WaitAsync(0, cancellationToken))
        {
            return Result(UpdateCheckStatus.Busy);
        }

        try
        {
            if (!_updateService.IsInstalled)
            {
                return Result(UpdateCheckStatus.DevelopmentBuild);
            }

            if (!manual)
            {
                _settings.LastAutomaticUpdateCheckDate = UpdateCheckPolicy.FormatDate(today);
                if (!_saveSettings(_settings))
                {
                    TryLogError("Automatic update attempt date could not be persisted.");
                }
            }

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(_timeout);
            var update = await _updateService.CheckForUpdatesAsync(timeoutSource.Token);
            return update is null
                ? Result(UpdateCheckStatus.Current)
                : new UpdateCheckResult(UpdateCheckStatus.Available, _updateService.CurrentVersion, update);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            TryLogError($"{GetCheckLabel(manual)} update check timed out.", ex);
            return Result(UpdateCheckStatus.Failed);
        }
        catch (OperationCanceledException)
        {
            TryLogInfo($"{GetCheckLabel(manual)} update check was canceled.");
            return Result(UpdateCheckStatus.Failed);
        }
        catch (Exception ex)
        {
            TryLogError($"{GetCheckLabel(manual)} update check failed.", ex);
            return Result(UpdateCheckStatus.Failed);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DownloadUpdatesAsync(
        UpdateAvailability update,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!_updateService.IsInstalled || !await _gate.WaitAsync(0, cancellationToken))
        {
            return false;
        }

        try
        {
            await _updateService.DownloadUpdatesAsync(update, progress, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            TryLogInfo("Update download was canceled.");
            return false;
        }
        catch (Exception ex)
        {
            TryLogError("Update download failed.", ex);
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void ApplyUpdatesAndRestart(UpdateAvailability update)
    {
        _updateService.ApplyUpdatesAndRestart(update);
    }

    private UpdateCheckResult Result(UpdateCheckStatus status)
    {
        return new UpdateCheckResult(status, _updateService.CurrentVersion);
    }

    private static string GetCheckLabel(bool manual) => manual ? "Manual" : "Automatic";

    private void TryLogInfo(string message)
    {
        try
        {
            _logger?.Info(message);
        }
        catch (Exception)
        {
        }
    }

    private void TryLogError(string message, Exception? exception = null)
    {
        try
        {
            _logger?.Error(message, exception);
        }
        catch (Exception)
        {
        }
    }
}
