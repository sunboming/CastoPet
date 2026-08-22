namespace CastoPet.Core.Movement;

public readonly record struct CursorNudgeResult(bool ShouldMove, double X, double Y);

public static class CursorNudgePlanner
{
    public const double ActivationRadius = 60;
    public const double OneShotPushDistance = 24;
    public const double ManualMovementThreshold = 8;
    public static readonly TimeSpan ManualMovementCooldown = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan MaxContinuousPushDuration = TimeSpan.FromSeconds(2);

    public static CursorNudgeResult CalculateNudge(
        double cursorX,
        double cursorY,
        double petCenterX,
        double petCenterY,
        double movementDeltaX,
        double movementDeltaY,
        PetMovementBounds bounds)
    {
        var distanceToPet = Distance(cursorX, cursorY, petCenterX, petCenterY);
        if (distanceToPet > ActivationRadius)
        {
            return new CursorNudgeResult(false, cursorX, cursorY);
        }

        var movementDistance = Distance(0, 0, movementDeltaX, movementDeltaY);
        if (movementDistance <= 0.001)
        {
            return new CursorNudgeResult(false, cursorX, cursorY);
        }

        var x = cursorX + movementDeltaX / movementDistance * OneShotPushDistance;
        var y = cursorY + movementDeltaY / movementDistance * OneShotPushDistance;
        var maxX = bounds.Left + Math.Max(0, bounds.Width - 1);
        var maxY = bounds.Top + Math.Max(0, bounds.Height - 1);

        return new CursorNudgeResult(
            true,
            Math.Clamp(x, bounds.Left, maxX),
            Math.Clamp(y, bounds.Top, maxY));
    }

    public static bool IsManualMovement(
        double currentX,
        double currentY,
        double expectedX,
        double expectedY)
    {
        return Distance(currentX, currentY, expectedX, expectedY) > ManualMovementThreshold;
    }

    public static bool CanNudgeAfterManualMovement(TimeSpan now, TimeSpan? lastManualMovement)
    {
        return lastManualMovement is null || now - lastManualMovement.Value >= ManualMovementCooldown;
    }

    public static bool CanNudge(
        bool isMouseButtonPressed,
        TimeSpan now,
        TimeSpan? lastManualMovement,
        TimeSpan? pushStartedAt)
    {
        if (isMouseButtonPressed || !CanNudgeAfterManualMovement(now, lastManualMovement))
        {
            return false;
        }

        return pushStartedAt is null || now - pushStartedAt.Value <= MaxContinuousPushDuration;
    }

    private static double Distance(double ax, double ay, double bx, double by)
    {
        var dx = bx - ax;
        var dy = by - ay;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
