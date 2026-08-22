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

sealed class FakeSettingsWindow : ISettingsWindow
{
    public event EventHandler? Closed;

    public bool IsVisible { get; private set; }
    public int ShowCount { get; private set; }
    public int ActivateCount { get; private set; }

    public void Show()
    {
        ShowCount++;
        IsVisible = true;
    }

    public bool Activate()
    {
        ActivateCount++;
        return true;
    }

    public void Close()
    {
        CloseFromUser();
    }

    public void CloseFromUser()
    {
        IsVisible = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }
}

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
