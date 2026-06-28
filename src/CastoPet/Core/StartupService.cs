using System.IO;
using Microsoft.Win32;

namespace CastoPet.Core;

public sealed class StartupService
{
    public const string ValueName = "CastoPet";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly LoggingService _logger;

    public StartupService(LoggingService logger)
    {
        _logger = logger;
    }

    public bool IsEnabled(string? executablePath = null)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            if (key?.GetValue(ValueName) is not string value || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(executablePath)
                || MatchesExecutablePath(value, executablePath);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to read startup registration.", ex);
            return false;
        }
    }

    public bool SetEnabled(bool enabled, string executablePath)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKey, writable: true);

            if (enabled)
            {
                key.SetValue(ValueName, $"\"{executablePath}\"");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to update startup registration.", ex);
            return false;
        }
    }

    public static bool MatchesExecutablePath(string registeredValue, string executablePath)
    {
        static string Normalize(string path)
        {
            return Path.GetFullPath(path.Trim().Trim('"'));
        }

        try
        {
            return string.Equals(
                Normalize(registeredValue),
                Normalize(executablePath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
