namespace CastoPet.Core.Wheel;

public readonly record struct RadialWheelPopupPosition(int Left, int Top)
{
    public static RadialWheelPopupPosition Calculate(
        double invocationDeviceX,
        double invocationDeviceY,
        int popupPixelWidth,
        int popupPixelHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(popupPixelWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(popupPixelHeight);

        return new RadialWheelPopupPosition(
            (int)Math.Round(invocationDeviceX - (popupPixelWidth / 2d), MidpointRounding.AwayFromZero),
            (int)Math.Round(invocationDeviceY - (popupPixelHeight / 2d), MidpointRounding.AwayFromZero));
    }
}
