using System.IO;

namespace CastoPet.Core;

public sealed class LoggingService
{
    private readonly AppPaths _paths;
    private readonly object _gate = new();

    public LoggingService(AppPaths paths)
    {
        _paths = paths;
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

            File.AppendAllText(_paths.LogFile, line + Environment.NewLine);
        }
    }
}
