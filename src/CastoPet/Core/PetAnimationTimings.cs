namespace CastoPet.Core;

public static class PetAnimationTimings
{
    public static readonly TimeSpan IdleBreathingCycleDuration = TimeSpan.FromMilliseconds(1900);
    public static readonly TimeSpan ExpressionEnterDuration = TimeSpan.FromMilliseconds(120);
    public static readonly TimeSpan ExpressionExitDuration = TimeSpan.FromMilliseconds(180);
    public static readonly TimeSpan WheelOpenDuration = TimeSpan.FromMilliseconds(120);
    public static readonly TimeSpan WheelSelectionDuration = TimeSpan.FromMilliseconds(90);

    public const double IdleBreathingTranslateY = 3;
    public const double IdleBreathingScaleDelta = 0.012;
    public const double ExpressionEnterStartScale = 0.985;
    public const double ExpressionDimmedOpacity = 0.96;
    public const double WheelOpenStartScale = 0.92;
}
