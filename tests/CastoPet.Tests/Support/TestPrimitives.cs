namespace CastoPet.Tests;

static class Assert
{
    public static void True(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    public static void False(bool value, string message)
    {
        if (value) throw new InvalidOperationException(message);
    }

    public static void Contains(string text, string expected, string message)
    {
        if (!text.Contains(expected, StringComparison.Ordinal)) throw new InvalidOperationException(message);
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
        }
    }

    public static TException Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException ex)
        {
            return ex;
        }

        throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
    }
}

sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    private TempDirectory(string path)
    {
        Path = path;
    }

    public static TempDirectory Create()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CastoPet.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new TempDirectory(path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}

readonly record struct IdleFrameDiagnostic(
    string Name,
    int Width,
    int Height,
    Rectangle Bounds,
    double CenterX,
    double AdjacentAverageDelta);

sealed class FakeUpdateService : IUpdateService
{
    public bool IsInstalled { get; set; } = true;
    public string CurrentVersion => "0.1.0";
    public int CheckCount { get; private set; }
    public Exception? Exception { get; set; }
    public Func<UpdateAvailability?>? OnCheck { get; set; }
    public Task<UpdateAvailability?>? PendingCheck { get; set; }

    public Task<UpdateAvailability?> CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        CheckCount++;
        if (Exception is not null)
        {
            return Task.FromException<UpdateAvailability?>(Exception);
        }

        if (PendingCheck is not null)
        {
            return PendingCheck;
        }

        return Task.FromResult(OnCheck?.Invoke());
    }

    public Task DownloadUpdatesAsync(
        UpdateAvailability update,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report(100);
        return Task.CompletedTask;
    }

    public void ApplyUpdatesAndRestart(UpdateAvailability update)
    {
    }
}

sealed class FakePetCommandTarget : IPetCommandTarget
{
    public int ApplyCount { get; private set; }

    public void ShowOrRestore()
    {
    }

    public void ApplySettings(AppSettings settings)
    {
        ApplyCount++;
    }
}

sealed class FakeSettingsStore : ISettingsStore
{
    public bool SaveResult { get; set; } = true;
    public int SaveCount { get; private set; }

    public AppSettings Load() => AppSettings.Default;

    public bool Save(AppSettings settings)
    {
        SaveCount++;
        return SaveResult;
    }
}

sealed class FakeStartupRegistration : IStartupRegistration
{
    public bool SetResult { get; set; } = true;

    public bool SetEnabled(bool enabled, string executablePath) => SetResult;
}

sealed class FakeApplicationLogger : IApplicationLogger
{
    public List<string> Messages { get; } = [];

    public void Info(string message) => Messages.Add(message);

    public void Error(string message, Exception? exception = null) => Messages.Add(message);
}

sealed class FakeUserNotificationService : IUserNotificationService
{
    public int WarningCount { get; private set; }

    public void ShowWarning(string message, string title)
    {
        WarningCount++;
    }
}

sealed class FakeApplicationShutdown : IApplicationShutdown
{
    public int Count { get; private set; }

    public void Shutdown()
    {
        Count++;
    }
}
