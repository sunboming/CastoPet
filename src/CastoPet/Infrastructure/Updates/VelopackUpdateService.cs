using System.Reflection;
using Velopack;
using Velopack.Sources;

using CastoPet.Application.Updates;

namespace CastoPet.Infrastructure.Updates;

public sealed class VelopackUpdateService : IUpdateService
{
    public const string RepositoryUrl = "https://github.com/sunboming/CastoPet";

    private readonly UpdateManager _manager;

    public VelopackUpdateService()
    {
        var source = new GithubSource(
            RepositoryUrl,
            accessToken: null,
            prerelease: false);
        _manager = new UpdateManager(source);
    }

    public bool IsInstalled => _manager.IsInstalled;

    public string CurrentVersion => _manager.CurrentVersion?.ToString()
        ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3)
        ?? "0.1.0";

    public async Task<UpdateAvailability?> CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        var update = await _manager.CheckForUpdatesAsync().WaitAsync(cancellationToken);
        if (update is null)
        {
            return null;
        }

        return new UpdateAvailability(
            update.TargetFullRelease.Version.ToString(),
            update.TargetFullRelease.NotesMarkdown ?? string.Empty,
            update);
    }

    public Task DownloadUpdatesAsync(
        UpdateAvailability update,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        var nativeUpdate = GetNativeUpdate(update);
        return _manager.DownloadUpdatesAsync(
            nativeUpdate,
            value => progress?.Report(value),
            cancellationToken);
    }

    public void ApplyUpdatesAndRestart(UpdateAvailability update)
    {
        var nativeUpdate = GetNativeUpdate(update);
        _manager.ApplyUpdatesAndRestart(nativeUpdate.TargetFullRelease);
    }

    private static UpdateInfo GetNativeUpdate(UpdateAvailability update)
    {
        return update.NativeHandle as UpdateInfo
            ?? throw new InvalidOperationException("The update does not contain Velopack metadata.");
    }
}
