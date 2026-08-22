using CastoPet.Core.Settings;

namespace CastoPet.Application.Settings;

public static class SettingsTransaction
{
    public static bool TryApply(
        AppSettings settings,
        Action<AppSettings> mutation,
        Func<AppSettings, bool> save)
    {
        var snapshot = settings.Clone();
        try
        {
            mutation(settings);
            settings.SchemaVersion = AppSettings.CurrentSchemaVersion;
            if (save(settings))
            {
                return true;
            }
        }
        catch (Exception)
        {
        }

        settings.CopyFrom(snapshot);
        return false;
    }
}
