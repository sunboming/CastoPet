namespace CastoPet.Core;

public enum CastoPetEdition
{
    Stable,
    Preview,
}

public sealed record CastoPetFeatureProfile(
    CastoPetEdition Edition,
    bool Petting,
    bool RadialWheel,
    bool ShortcutLauncher,
    bool ActiveMovement,
    bool PushCursor,
    bool InputReactiveMode,
    bool ExternalSkins)
{
    public static CastoPetFeatureProfile Stable { get; } = new(
        CastoPetEdition.Stable,
        Petting: false,
        RadialWheel: false,
        ShortcutLauncher: false,
        ActiveMovement: false,
        PushCursor: false,
        InputReactiveMode: false,
        ExternalSkins: false);

    public static CastoPetFeatureProfile Preview { get; } = new(
        CastoPetEdition.Preview,
        Petting: true,
        RadialWheel: true,
        ShortcutLauncher: true,
        ActiveMovement: true,
        PushCursor: true,
        InputReactiveMode: true,
        ExternalSkins: true);

    public static CastoPetFeatureProfile Current
    {
        get
        {
#if CASTOPET_STABLE
            return Stable;
#else
            return Preview;
#endif
        }
    }
}
