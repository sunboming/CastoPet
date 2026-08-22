using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

using CastoPet.Core.Diagnostics;
using CastoPet.Core.Product;
using CastoPet.Infrastructure.Persistence;

namespace CastoPet.Infrastructure.Diagnostics;

public sealed record CrashReportInfo(string Id, string Path, DateTimeOffset CreatedUtc);

public sealed class CrashReportService
{
    public const int DefaultMaxReports = 20;

    private readonly AppPaths _paths;
    private readonly LoggingService _logger;
    private readonly int _maxReports;
    private readonly Func<DateTimeOffset> _nowProvider;
    private readonly CastoPetBuildInfo _buildInfo;

    public CrashReportService(
        AppPaths paths,
        LoggingService logger,
        int maxReports = DefaultMaxReports,
        Func<DateTimeOffset>? nowProvider = null,
        CastoPetBuildInfo? buildInfo = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxReports, 1);
        _paths = paths;
        _logger = logger;
        _maxReports = maxReports;
        _nowProvider = nowProvider ?? (() => DateTimeOffset.UtcNow);
        _buildInfo = buildInfo ?? CastoPetBuildInfo.Current(CastoPetFeatureProfile.Current.Edition);
    }

    public bool TryWriteReport(Exception exception, out CrashReportInfo? report)
    {
        return TryWriteReport(exception, CrashReportKind.Fatal, out report);
    }

    public bool TryWriteReport(
        Exception exception,
        CrashReportKind kind,
        out CrashReportInfo? report)
    {
        report = null;
        string? temporaryPath = null;

        try
        {
            Directory.CreateDirectory(_paths.CrashesDirectory);
            var now = _nowProvider();
            var prefix = kind == CrashReportKind.Fatal ? "crash" : "diagnostic";
            var id = $"{prefix}-{now:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}";
            var finalPath = System.IO.Path.Combine(_paths.CrashesDirectory, $"{id}.txt");
            temporaryPath = finalPath + ".tmp";
            var context = CreateContext(now, kind);
            var content = CrashReportFormatter.Format(context, exception, ReadLogTail());

            File.WriteAllText(temporaryPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, finalPath);
            report = new CrashReportInfo(id, finalPath, now);
            TryPruneOldReports();
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

    private CrashReportContext CreateContext(DateTimeOffset timestampUtc, CrashReportKind kind)
    {
        return new CrashReportContext(
            timestampUtc,
            _buildInfo.Version,
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.UserName,
            _buildInfo.Edition.ToString(),
            _buildInfo.SourceCommit,
            kind);
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

    private void TryPruneOldReports()
    {
        try
        {
            var oldReports = Directory
                .EnumerateFiles(_paths.CrashesDirectory, "*.txt", SearchOption.TopDirectoryOnly)
                .Where(path =>
                {
                    var name = System.IO.Path.GetFileName(path);
                    return name.StartsWith("crash-", StringComparison.Ordinal)
                        || name.StartsWith("diagnostic-", StringComparison.Ordinal);
                })
                .OrderByDescending(GetChronologicalReportKey, StringComparer.Ordinal)
                .Skip(_maxReports);
            foreach (var path in oldReports)
            {
                File.Delete(path);
            }
        }
        catch (Exception exception)
        {
            TryLogFailure(exception);
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

    private static string GetChronologicalReportKey(string path)
    {
        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        var prefixSeparator = name.IndexOf('-');
        return prefixSeparator >= 0 ? name[(prefixSeparator + 1)..] : name;
    }
}
