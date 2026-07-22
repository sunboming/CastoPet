namespace CastoPet.Core;

internal readonly record struct RadialWheelColor(byte Alpha, byte Red, byte Green, byte Blue);

internal static class RadialWheelStyle
{
    private static readonly RadialWheelColor FirstRingFill = new(218, 238, 228, 248);
    private static readonly RadialWheelColor SecondRingFill = new(210, 228, 212, 244);
    private static readonly RadialWheelColor FirstRingDisabledFill = new(145, 238, 228, 248);
    private static readonly RadialWheelColor SecondRingDisabledFill = new(136, 228, 212, 244);

    public static readonly RadialWheelColor NormalStroke = new(175, 170, 145, 197);
    public static readonly RadialWheelColor SelectedStroke = new(255, 126, 85, 164);

    public const double NormalStrokeThickness = 1.05;
    public const double SelectedStrokeThickness = 2;
    public const double SectorGapRadians = 0;

    public static RadialWheelColor GetNormalFill(RadialWheelRing ring, bool isEnabled) =>
        (ring, isEnabled) switch
        {
            (RadialWheelRing.First, true) => FirstRingFill,
            (RadialWheelRing.Second, true) => SecondRingFill,
            (RadialWheelRing.First, false) => FirstRingDisabledFill,
            (RadialWheelRing.Second, false) => SecondRingDisabledFill,
            _ => throw new ArgumentOutOfRangeException(nameof(ring), ring, "Only selectable wheel rings have sector fills."),
        };
}
