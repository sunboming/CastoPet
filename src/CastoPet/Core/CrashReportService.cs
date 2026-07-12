using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace CastoPet.Core;

public sealed record CrashReportInfo(string Id, string Path, DateTimeOffset CreatedUtc);

public sealed class CrashReportService
{
    private readonly AppPaths _paths;
    private readonly LoggingService _logger;

    public CrashReportService(AppPaths paths, LoggingService logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public bool TryWriteReport(Exception exception, out CrashReportInfo? report)
    {
        report = null;
        string? temporaryPath = null;

        try
        {
            Directory.CreateDirectory(_paths.CrashesDirectory);
            var now = DateTimeOffset.UtcNow;
            var id = $"crash-{now:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}";
            var finalPath = System.IO.Path.Combine(_paths.CrashesDirectory, $"{id}.txt");
            temporaryPath = finalPath + ".tmp";
            var context = CreateContext(now);
            var content = CrashReportFormatter.Format(context, exception, ReadLogTail());

            File.WriteAllText(temporaryPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, finalPath);
            report = new CrashReportInfo(id, finalPath, now);
            return true;
        }
        catch (Exception writeException)
        {
            TryDeleteTemporaryFile(temporaryPath);
            TryLogFailure(writeException);
            return false;
        }
    }

    public CrashReportInfo? GetLatestUnacknowledged(string? acknowledgedId)
    {
        try
        {
            if (!Directory.Exists(_paths.CrashesDirectory))
            {
                return null;
            }

            var path = Directory
                .EnumerateFiles(_paths.CrashesDirectory, "crash-*.txt", SearchOption.TopDirectoryOnly)
                .OrderByDescending(System.IO.Path.GetFileName, StringComparer.Ordinal)
                .FirstOrDefault();
            if (path is null)
            {
                return null;
            }

            var id = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(id, acknowledgedId, StringComparison.Ordinal))
            {
                return null;
            }

            return new CrashReportInfo(id, path, File.GetCreationTimeUtc(path));
        }
        catch (Exception exception)
        {
            TryLogFailure(exception);
            return null;
        }
    }

    public bool OpenReportsDirectory()
    {
        try
        {
            Directory.CreateDirectory(_paths.CrashesDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{_paths.CrashesDirectory}\"",
                UseShellExecute = true,
            });
            return true;
        }
        catch (Exception exception)
        {
            TryLogFailure(exception);
            return false;
        }
    }

    private static CrashReportContext CreateContext(DateTimeOffset timestampUtc)
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "unknown";
        return new CrashReportContext(
            timestampUtc,
            version,
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.UserName);
    }

    private IReadOnlyList<string> ReadLogTail()
    {
        try
        {
            return File.Exists(_paths.LogFile)
                ? File.ReadLines(_paths.LogFile).TakeLast(CrashReportFormatter.MaxLogTailLines).ToArray()
                : Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private void TryLogFailure(Exception exception)
    {
        try
        {
            _logger.Error("Could not write or inspect a local crash report.", exception);
        }
        catch
        {
            // Crash handling must never throw a secondary exception.
        }
    }

    private static void TryDeleteTemporaryFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch
        {
        }
    }
}
