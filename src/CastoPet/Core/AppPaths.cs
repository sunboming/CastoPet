using System.IO;

namespace CastoPet.Core;

public sealed class AppPaths
{
    public AppPaths(string? baseDirectory = null)
    {
        DataDirectory = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CastoPet");
        LogsDirectory = Path.Combine(DataDirectory, "logs");
        SettingsFile = Path.Combine(DataDirectory, "settings.json");
        LogFile = Path.Combine(LogsDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
    }

    public string DataDirectory { get; }
    public string LogsDirectory { get; }
    public string SettingsFile { get; }
    public string LogFile { get; }
}
