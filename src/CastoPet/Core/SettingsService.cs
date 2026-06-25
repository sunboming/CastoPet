using System.IO;
using System.Text.Json;

namespace CastoPet.Core;

public sealed class SettingsService
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
        try
        {
            if (!File.Exists(_paths.SettingsFile))
            {
                return AppSettings.Default;
            }

            var json = File.ReadAllText(_paths.SettingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? AppSettings.Default;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load settings. Defaults will be used.", ex);
            return AppSettings.Default;
        }
    }

    public bool Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(_paths.DataDirectory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_paths.SettingsFile, json);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to save settings.", ex);
            return false;
        }
    }
}
