namespace CastoPet.Core.Wheel;

public enum RadialWheelRing
{
    Center,
    First,
    Second,
    Outside,
}

public readonly record struct RadialWheelSelection(RadialWheelRing Ring, int SectorIndex);

public static class RadialWheelSelector
{
    public static int GetCategoryIndex(double pointerX, double pointerY, int categoryCount) =>
        GetSectorIndex(pointerX, pointerY, categoryCount);

    public static RadialWheelSelection GetSelection(
        double pointerX,
        double pointerY,
        int firstRingItemCount,
        int secondRingItemCount,
        int selectedCategoryIndex = -1)
    {
        var distance = Math.Sqrt((pointerX * pointerX) + (pointerY * pointerY));
        if (distance < WheelCatalog.InnerRadius)
        {
            return new RadialWheelSelection(RadialWheelRing.Center, -1);
        }

        if (distance <= WheelCatalog.FirstRingOuterRadius)
        {
            return new RadialWheelSelection(
                RadialWheelRing.First,
                GetSectorIndex(pointerX, pointerY, firstRingItemCount));
        }

        if (distance <= WheelCatalog.InteractionOuterRadius)
        {
            var secondRingIndex = selectedCategoryIndex >= 0
                ? RadialWheelArcLayout.GetItemIndex(
                    GetPointerAngle(pointerX, pointerY),
                    RadialWheelArcLayout.CreateSecondRingArc(
                        selectedCategoryIndex,
                        firstRingItemCount,
                        secondRingItemCount),
                    secondRingItemCount)
                : GetSectorIndex(pointerX, pointerY, secondRingItemCount);
            return new RadialWheelSelection(
                RadialWheelRing.Second,
                secondRingIndex);
        }

        return new RadialWheelSelection(RadialWheelRing.Outside, -1);
    }

    private static int GetSectorIndex(double pointerX, double pointerY, int itemCount)
    {
        if (itemCount <= 0)
        {
            return -1;
        }

        var angle = GetPointerAngle(pointerX, pointerY);
        return Math.Min((int)(angle / (Math.Tau / itemCount)), itemCount - 1);
    }

    private static double GetPointerAngle(double pointerX, double pointerY)
    {
        var angle = Math.Atan2(pointerX, -pointerY);
        if (angle < 0)
        {
            angle += Math.Tau;
        }

        return angle;
    }
}
