namespace CastoPet.Core;

public sealed record UpdateAvailability(
    string Version,
    string ReleaseNotes,
    object? NativeHandle = null);

public interface IUpdateService
{
    bool IsInstalled { get; }

    string CurrentVersion { get; }

    Task<UpdateAvailability?> CheckForUpdatesAsync(CancellationToken cancellationToken);

    Task DownloadUpdatesAsync(
        UpdateAvailability update,
        IProgress<int>? progress,
        CancellationToken cancellationToken);

    void ApplyUpdatesAndRestart(UpdateAvailability update);
}

public enum UpdateCheckStatus
{
    Skipped,
    Busy,
    DevelopmentBuild,
    Current,
    Available,
    Failed,
}

public sealed record UpdateCheckResult(
    UpdateCheckStatus Status,
    string CurrentVersion,
    UpdateAvailability? AvailableUpdate = null);
