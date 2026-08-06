namespace CastoPet.Core;

public readonly record struct PetMovementBounds(double Left, double Top, double Width, double Height);

public readonly record struct PetMovementTarget(double Left, double Top);

public static class PetMovementPlanner
{
    public const double MouseInterestRadius = 360;
    public const double MouseApproachOffset = 32;
    public const double MinMouseApproachOffset = 20;
    public const double MaxMouseApproachOffset = 40;
    public const double StopDistance = 4;
    public const double MovementEase = 0.14;

    public static PetMovementTarget ClampTarget(
        double left,
        double top,
        double windowWidth,
        double windowHeight,
        PetMovementBounds bounds)
    {
        var maxLeft = bounds.Left + Math.Max(0, bounds.Width - windowWidth);
        var maxTop = bounds.Top + Math.Max(0, bounds.Height - windowHeight);

        return new PetMovementTarget(
            Math.Clamp(left, bounds.Left, maxLeft),
            Math.Clamp(top, bounds.Top, maxTop));
    }

    public static PetMovementTarget CalculateMouseApproachTarget(
        double petLeft,
        double petTop,
        double petWidth,
        double petHeight,
        double mouseX,
        double mouseY,
        PetMovementBounds bounds)
    {
        var petCenterX = petLeft + petWidth / 2;
        var petCenterY = petTop + petHeight / 2;
        var dx = mouseX - petCenterX;
        var dy = mouseY - petCenterY;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance <= 0.001)
        {
            return ClampTarget(
                mouseX - petWidth / 2 - MouseApproachOffset,
                mouseY - petHeight / 2,
                petWidth,
                petHeight,
                bounds);
        }

        var targetCenterX = mouseX - dx / distance * MouseApproachOffset;
        var targetCenterY = mouseY - dy / distance * MouseApproachOffset;

        return ClampTarget(
            targetCenterX - petWidth / 2,
            targetCenterY - petHeight / 2,
            petWidth,
            petHeight,
            bounds);
    }

    public static PetMovementTarget ResolveMouseApproachTarget(
        double petLeft,
        double petTop,
        double petWidth,
        double petHeight,
        double mouseX,
        double mouseY,
        PetMovementBounds bounds,
        PetMovementTarget? activeTarget,
        bool retainActiveTarget)
    {
        if (retainActiveTarget && activeTarget is { } retained)
        {
            return retained;
        }

        return CalculateMouseApproachTarget(
            petLeft,
            petTop,
            petWidth,
            petHeight,
            mouseX,
            mouseY,
            bounds);
    }

    public static PetMovementTarget StepToward(
        double currentLeft,
        double currentTop,
        PetMovementTarget target)
    {
        return new PetMovementTarget(
            currentLeft + (target.Left - currentLeft) * MovementEase,
            currentTop + (target.Top - currentTop) * MovementEase);
    }

    public static bool IsClose(double currentLeft, double currentTop, PetMovementTarget target)
    {
        var dx = target.Left - currentLeft;
        var dy = target.Top - currentTop;

        return Math.Sqrt(dx * dx + dy * dy) <= StopDistance;
    }

    public static bool IsAtMouseApproachTarget(
        double petLeft,
        double petTop,
        double petWidth,
        double petHeight,
        double mouseX,
        double mouseY,
        PetMovementBounds bounds)
    {
        var target = CalculateMouseApproachTarget(petLeft, petTop, petWidth, petHeight, mouseX, mouseY, bounds);

        return IsClose(petLeft, petTop, target);
    }
}
