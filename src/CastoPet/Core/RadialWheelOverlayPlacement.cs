namespace CastoPet.Core;

public readonly record struct RadialWheelOverlayPlacement(
    double CenterX,
    double CenterY,
    double Left,
    double Top)
{
    public static RadialWheelOverlayPlacement Calculate(
        double invocationX,
        double invocationY,
        double overlayWidth,
        double overlayHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(overlayWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(overlayHeight);

        return new RadialWheelOverlayPlacement(
            invocationX,
            invocationY,
            invocationX - (overlayWidth / 2),
            invocationY - (overlayHeight / 2));
    }
}
