using System.IO;

using CastoPet.Core.Product;

namespace CastoPet.Infrastructure.Persistence;

public sealed class AppPaths
{
    public AppPaths(string? baseDirectory = null)
    {
        DataDirectory = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            CastoPetProductIdentity.Current.DataDirectoryName);
        LogsDirectory = Path.Combine(DataDirectory, "logs");
        CrashesDirectory = Path.Combine(DataDirectory, "Crashes");
        SettingsFile = Path.Combine(DataDirectory, "settings.json");
        SettingsBackupFile = SettingsFile + ".bak";
        SettingsTemporaryFile = SettingsFile + ".tmp";
        LogFile = Path.Combine(LogsDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
    }

    public string DataDirectory { get; }
    public string LogsDirectory { get; }
    public string CrashesDirectory { get; }
    public string SettingsFile { get; }
    public string SettingsBackupFile { get; }
    public string SettingsTemporaryFile { get; }
    public string LogFile { get; }

    public static AppPaths ForProduct(
        CastoPetProductIdentity identity,
        string? localAppDataRoot = null)
    {
        ArgumentNullException.ThrowIfNull(identity);
        var root = localAppDataRoot
            ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new AppPaths(Path.Combine(root, identity.DataDirectoryName));
    }
}
