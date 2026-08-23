using System.IO;
using System.Text;

using CastoPet.Application.Diagnostics;
using CastoPet.Infrastructure.Persistence;

namespace CastoPet.Infrastructure.Diagnostics;

public sealed class LoggingService : IApplicationLogger
{
    public const long DefaultMaxLogFileBytes = 2 * 1024 * 1024;
    public const int DefaultMaxArchiveFiles = 5;

    private readonly AppPaths _paths;
    private readonly long _maxLogFileBytes;
    private readonly int _maxArchiveFiles;
    private readonly object _gate = new();

    public LoggingService(
        AppPaths paths,
        long maxLogFileBytes = DefaultMaxLogFileBytes,
        int maxArchiveFiles = DefaultMaxArchiveFiles)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxLogFileBytes, 0);
        ArgumentOutOfRangeException.ThrowIfNegative(maxArchiveFiles);
        _paths = paths;
        _maxLogFileBytes = maxLogFileBytes;
        _maxArchiveFiles = maxArchiveFiles;
    }

    public void Info(string message)
    {
        Write("INFO", message, null);
    }

    public void Error(string message, Exception? exception = null)
    {
        Write("ERROR", message, exception);
    }

    private void Write(string level, string message, Exception? exception)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(_paths.LogsDirectory);
            var line = $"{DateTime.Now:O} [{level}] {message}";
            if (exception is not null)
            {
                line += Environment.NewLine + exception;
            }

            var entry = line + Environment.NewLine;
            RotateIfNeeded(Encoding.UTF8.GetByteCount(entry));
            File.AppendAllText(_paths.LogFile, entry, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    private void RotateIfNeeded(int incomingBytes)
    {
        if (!File.Exists(_paths.LogFile) || new FileInfo(_paths.LogFile).Length == 0 ||
            new FileInfo(_paths.LogFile).Length + incomingBytes <= _maxLogFileBytes)
        {
            return;
        }

        if (_maxArchiveFiles == 0)
        {
            File.Delete(_paths.LogFile);
            return;
        }

        for (var index = _maxArchiveFiles; index >= 1; index--)
        {
            var destination = $"{_paths.LogFile}.{index}";
            var source = index == 1
                ? _paths.LogFile
                : $"{_paths.LogFile}.{index - 1}";
            if (File.Exists(source))
            {
                File.Move(source, destination, overwrite: true);
            }
        }
    }
}
