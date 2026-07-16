namespace CastoPet.Core;

internal readonly record struct RadialWheelColor(byte Alpha, byte Red, byte Green, byte Blue);

internal static class RadialWheelStyle
{
    private static readonly RadialWheelColor FirstRingFill = new(140, 66, 42, 110);
    private static readonly RadialWheelColor SecondRingFill = new(122, 86, 57, 132);
    private static readonly RadialWheelColor FirstRingDisabledFill = new(84, 66, 42, 110);
    private static readonly RadialWheelColor SecondRingDisabledFill = new(72, 86, 57, 132);

    public static readonly RadialWheelColor SelectedFill = new(191, 126, 87, 188);
    public static readonly RadialWheelColor NormalStroke = new(150, 236, 224, 255);
    public static readonly RadialWheelColor SelectedStroke = new(235, 250, 242, 255);

    public const double NormalStrokeThickness = 0.9;
    public const double SelectedStrokeThickness = 1.5;
    public const double SectorGapRadians = 0.016;
    public const byte LabelShadowAlpha = 120;
    public const double LabelShadowBlurRadius = 5;
    public const double LabelShadowOpacity = 0.58;

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
