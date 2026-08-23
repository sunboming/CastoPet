using System.IO;
using System.Text;
using System.Text.Json;

using CastoPet.Application.Settings;
using CastoPet.Core.Settings;
using CastoPet.Infrastructure.Diagnostics;

namespace CastoPet.Infrastructure.Persistence;

public sealed class SettingsService : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly AppPaths _paths;
    private readonly LoggingService _logger;

    public SettingsService(AppPaths paths, LoggingService logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public AppSettings Load()
    {
        if (!File.Exists(_paths.SettingsFile))
        {
            return TryLoadBackup(out var recovered)
                ? recovered
                : AppSettings.Default;
        }

        try
        {
            return ReadAndNormalize(_paths.SettingsFile);
        }
        catch (Exception ex)
        {
            PreserveInvalidSettingsFile();
            TryLogError("Failed to load settings. The last valid backup will be tried.", ex);
            if (TryLoadBackup(out var recovered))
            {
                RestoreBackup();
                return recovered;
            }

            TryLogError("No valid settings backup was available. Defaults will be used.");
            return AppSettings.Default;
        }
    }

    public bool Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(_paths.DataDirectory);
            settings.SchemaVersion = AppSettings.CurrentSchemaVersion;
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            WriteTemporaryFile(json);

            if (File.Exists(_paths.SettingsFile))
            {
                File.Replace(
                    _paths.SettingsTemporaryFile,
                    _paths.SettingsFile,
                    _paths.SettingsBackupFile,
                    ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(_paths.SettingsTemporaryFile, _paths.SettingsFile);
                File.Copy(_paths.SettingsFile, _paths.SettingsBackupFile, overwrite: true);
            }

            return true;
        }
        catch (Exception ex)
        {
            TryDeleteTemporaryFile();
            TryLogError("Failed to save settings.", ex);
            return false;
        }
    }

    private AppSettings ReadAndNormalize(string path)
    {
        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
            ?? throw new InvalidDataException("Settings file is empty.");
        if (settings.SchemaVersion < 0 || settings.SchemaVersion > AppSettings.CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Unsupported settings schema version {settings.SchemaVersion}.");
        }

        settings.SchemaVersion = AppSettings.CurrentSchemaVersion;
        if (!Enum.IsDefined(settings.ThemeMode))
        {
            settings.ThemeMode = AppThemeMode.System;
        }

        if (settings.LastAutomaticUpdateCheckDate is { } date &&
            !DateOnly.TryParseExact(date, "yyyy-MM-dd", out _))
        {
            settings.LastAutomaticUpdateCheckDate = null;
        }

        return settings;
    }

    private bool TryLoadBackup(out AppSettings settings)
    {
        settings = AppSettings.Default;
        if (!File.Exists(_paths.SettingsBackupFile))
        {
            return false;
        }

        try
        {
            settings = ReadAndNormalize(_paths.SettingsBackupFile);
            return true;
        }
        catch (Exception ex)
        {
            TryLogError("Failed to load the settings backup.", ex);
            return false;
        }
    }

    private void WriteTemporaryFile(string json)
    {
        using var stream = new FileStream(
            _paths.SettingsTemporaryFile,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(json);
        writer.Flush();
        stream.Flush(flushToDisk: true);
    }

    private void PreserveInvalidSettingsFile()
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmssfff");
            var invalidPath = Path.Combine(_paths.DataDirectory, $"settings.invalid-{timestamp}.json");
            File.Move(_paths.SettingsFile, invalidPath);
        }
        catch (Exception ex)
        {
            TryLogError("The invalid settings file could not be preserved.", ex);
        }
    }

    private void RestoreBackup()
    {
        try
        {
            File.Copy(_paths.SettingsBackupFile, _paths.SettingsTemporaryFile, overwrite: true);
            File.Move(_paths.SettingsTemporaryFile, _paths.SettingsFile, overwrite: true);
        }
        catch (Exception ex)
        {
            TryDeleteTemporaryFile();
            TryLogError("The settings backup was loaded but could not replace the damaged file.", ex);
        }
    }

    private void TryDeleteTemporaryFile()
    {
        try
        {
            File.Delete(_paths.SettingsTemporaryFile);
        }
        catch (Exception)
        {
        }
    }

    private void TryLogError(string message, Exception? exception = null)
    {
        try
        {
            _logger.Error(message, exception);
        }
        catch (Exception)
        {
        }
    }
}
