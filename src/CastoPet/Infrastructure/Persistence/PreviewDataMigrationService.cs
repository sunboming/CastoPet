using System.IO;
using System.Text;

namespace CastoPet.Core;

public static class PreviewDataMigrationService
{
    private const string MarkerFileName = ".legacy-preview-data-imported";

    public static string GetMarkerPath(AppPaths previewPaths) =>
        Path.Combine(previewPaths.DataDirectory, MarkerFileName);

    public static bool TryMigrate(
        CastoPetProductIdentity identity,
        string? localAppDataRoot,
        AppPaths targetPaths,
        LoggingService logger)
    {
        if (identity.Edition != CastoPetEdition.Preview)
        {
            return true;
        }

        var markerPath = GetMarkerPath(targetPaths);
        if (File.Exists(markerPath))
        {
            return true;
        }

        try
        {
            var legacyPaths = AppPaths.ForProduct(CastoPetProductIdentity.Stable, localAppDataRoot);
            Directory.CreateDirectory(targetPaths.DataDirectory);
            CopyIfMissing(legacyPaths.SettingsFile, targetPaths.SettingsFile);
            CopyIfMissing(legacyPaths.SettingsBackupFile, targetPaths.SettingsBackupFile);
            CopyIfMissing(legacyPaths.ShortcutsFile, targetPaths.ShortcutsFile);
            File.WriteAllText(
                markerPath,
                DateTimeOffset.UtcNow.ToString("O"),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }
        catch (Exception ex)
        {
            try
            {
                logger.Error("Could not import legacy Preview settings into the isolated data directory.", ex);
            }
            catch
            {
            }

            return false;
        }
    }

    private static void CopyIfMissing(string source, string destination)
    {
        if (!File.Exists(source) || File.Exists(destination))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)
            ?? throw new InvalidOperationException("Migration destination must have a directory."));
        File.Copy(source, destination, overwrite: false);
    }
}
