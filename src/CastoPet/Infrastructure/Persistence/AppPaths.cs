using System.IO;

using Velopack.Locators;

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

    public static AppPaths ForCurrentDistribution(CastoPetProductIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        var locator = VelopackLocator.Current;
        return ForDistribution(
            identity,
            locator.IsPortable,
            locator.RootAppDir);
    }

    public static AppPaths ForDistribution(
        CastoPetProductIdentity identity,
        bool isPortable,
        string? portableRoot = null,
        string? localAppDataRoot = null)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (!isPortable)
        {
            return ForProduct(identity, localAppDataRoot);
        }

        var root = string.IsNullOrWhiteSpace(portableRoot)
            ? AppContext.BaseDirectory
            : portableRoot;
        return new AppPaths(Path.Combine(root, "UserData"));
    }
}
