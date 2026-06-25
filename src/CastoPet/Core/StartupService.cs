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

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
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
}
