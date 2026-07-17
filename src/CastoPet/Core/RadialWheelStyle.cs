namespace CastoPet.Core;

internal readonly record struct RadialWheelColor(byte Alpha, byte Red, byte Green, byte Blue);

internal static class RadialWheelStyle
{
    private static readonly RadialWheelColor FirstRingFill = new(180, 75, 44, 110);
    private static readonly RadialWheelColor SecondRingFill = new(166, 94, 60, 130);
    private static readonly RadialWheelColor FirstRingDisabledFill = new(110, 75, 44, 110);
    private static readonly RadialWheelColor SecondRingDisabledFill = new(96, 94, 60, 130);

    public static readonly RadialWheelColor SelectedFill = new(220, 143, 99, 187);
    public static readonly RadialWheelColor NormalStroke = new(135, 224, 211, 240);
    public static readonly RadialWheelColor SelectedStroke = new(235, 250, 239, 255);
    public static readonly RadialWheelColor SelectedGlow = new(210, 190, 153, 222);

    public const double NormalStrokeThickness = 0.9;
    public const double SelectedStrokeThickness = 1.35;
    public const double SectorGapRadians = 0.016;
    public const byte LabelShadowAlpha = 120;
    public const double LabelShadowBlurRadius = 5;
    public const double LabelShadowOpacity = 0.55;
    public const double SelectedGlowBlurRadius = 12;
    public const double SelectedGlowOpacity = 0.32;

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
