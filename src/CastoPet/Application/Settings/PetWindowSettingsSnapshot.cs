using CastoPet.Core.Settings;

namespace CastoPet.Application.Settings;

public sealed record PetWindowSettingsSnapshot(
    bool Topmost,
    bool ClickThrough,
    bool ShowInTaskbar)
{
    public static PetWindowSettingsSnapshot FromSettings(AppSettings settings)
    {
        return new PetWindowSettingsSnapshot(
            settings.Topmost,
            settings.ClickThrough,
            settings.ShowInTaskbar);
    }
}
