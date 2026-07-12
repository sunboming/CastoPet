namespace CastoPet.Core;

public sealed class UpdateCoordinator
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(8);

    private readonly IUpdateService _updateService;
    private readonly AppSettings _settings;
    private readonly Func<AppSettings, bool> _saveSettings;
    private readonly Func<DateOnly> _todayProvider;
    private readonly TimeSpan _timeout;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public UpdateCoordinator(
        IUpdateService updateService,
        AppSettings settings,
        Func<AppSettings, bool> saveSettings,
        Func<DateOnly>? todayProvider = null,
        TimeSpan? timeout = null)
    {
        _updateService = updateService;
        _settings = settings;
        _saveSettings = saveSettings;
        _todayProvider = todayProvider ?? (() => DateOnly.FromDateTime(DateTime.Now));
        _timeout = timeout ?? DefaultTimeout;
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
                _saveSettings(_settings);
            }

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(_timeout);
            var update = await _updateService.CheckForUpdatesAsync(timeoutSource.Token);
            return update is null
                ? Result(UpdateCheckStatus.Current)
                : new UpdateCheckResult(UpdateCheckStatus.Available, _updateService.CurrentVersion, update);
        }
        catch (Exception)
        {
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
        catch
        {
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
}
