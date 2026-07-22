namespace CastoPet.Core;

public readonly record struct RadialWheelArc(double StartAngle, double SweepAngle)
{
    public double StepAngle(int itemCount) => itemCount > 0 ? SweepAngle / itemCount : 0;
}

public static class RadialWheelArcLayout
{
    public static RadialWheelArc CreateSecondRingArc(
        int selectedCategoryIndex,
        int categoryCount,
        int itemCount)
    {
        if (selectedCategoryIndex < 0 || selectedCategoryIndex >= categoryCount ||
            categoryCount <= 0 || itemCount <= 0)
        {
            return new RadialWheelArc(0, 0);
        }

        var categoryCenter = (selectedCategoryIndex + 0.5) * Math.Tau / categoryCount;
        var sweep = Math.Clamp(itemCount * Math.PI / 6, Math.PI / 3, Math.PI);
        return new RadialWheelArc(NormalizeAngle(categoryCenter - sweep / 2), sweep);
    }

    public static int GetItemIndex(double pointerAngle, RadialWheelArc arc, int itemCount)
    {
        if (itemCount <= 0 || arc.SweepAngle <= 0)
        {
            return -1;
        }

        var offset = NormalizeAngle(pointerAngle - arc.StartAngle);
        if (offset > arc.SweepAngle + 1e-9)
        {
            return -1;
        }

        return Math.Min((int)(offset / arc.StepAngle(itemCount)), itemCount - 1);
    }

    private static double NormalizeAngle(double angle)
    {
        var normalized = angle % Math.Tau;
        return normalized < 0 ? normalized + Math.Tau : normalized;
    }
}
