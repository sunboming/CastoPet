namespace CastoPet.Core;

public sealed record PetWindowSettingsSnapshot(
    bool Topmost,
    bool ClickThrough,
    bool ShowInTaskbar,
    bool ActiveMovement,
    bool PushCursor,
    bool InputReactiveMode)
{
    public static PetWindowSettingsSnapshot FromSettings(AppSettings settings)
    {
        return new PetWindowSettingsSnapshot(
            settings.Topmost,
            settings.ClickThrough,
            settings.ShowInTaskbar,
            settings.ActiveMovement,
            settings.PushCursor,
            settings.InputReactiveMode);
    }
}
