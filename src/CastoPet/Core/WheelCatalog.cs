namespace CastoPet.Core;

public sealed record WheelCatalog(IReadOnlyList<WheelCategory> Categories)
{
    public const int MaxVisibleItemsPerRing = 8;
    public const double InnerRadius = 34;
    public const double FirstRingOuterRadius = 124;
    public const double SecondRingOuterRadius = 210;
    public const double SelectedScale = 1.18;

    public static readonly TimeSpan HoldDelay = TimeSpan.FromMilliseconds(400);
    public static readonly TimeSpan CategoryDwellDelay = TimeSpan.FromMilliseconds(120);
}
