using System.Reflection;

namespace CastoPet.Core;

public sealed class DisabledUpdateService(string? currentVersion = null) : IUpdateService
{
    public bool IsInstalled => false;

    public string CurrentVersion { get; } = currentVersion
        ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3)
        ?? "unknown";

    public Task<UpdateAvailability?> CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<UpdateAvailability?>(null);
    }

    public Task DownloadUpdatesAsync(
        UpdateAvailability update,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public void ApplyUpdatesAndRestart(UpdateAvailability update)
    {
    }
}
