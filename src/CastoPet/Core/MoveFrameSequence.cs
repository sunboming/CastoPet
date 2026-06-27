namespace CastoPet.Core;

public static class MoveFrameSequence
{
    public const int FrameCount = 8;
    public const double DistancePerFrame = 10;
    public const double BaseSpeedPixelsPerSecond = 90;
    public const double MinSpeedPixelsPerSecond = 80;
    public const double MaxSpeedPixelsPerSecond = 105;

    public static readonly IReadOnlyList<string> FramePaths = Enumerable
        .Range(0, FrameCount)
        .Select(index => $"Assets/States/Move/Castorice.Move.{index:00}.png")
        .ToArray();

    public static double StepDistance(TimeSpan elapsed, double distanceToTarget)
    {
        if (elapsed <= TimeSpan.Zero || distanceToTarget <= 0)
        {
            return 0;
        }

        var speed = distanceToTarget > 240 ? MaxSpeedPixelsPerSecond
            : distanceToTarget < 80 ? MinSpeedPixelsPerSecond
            : BaseSpeedPixelsPerSecond;

        return Math.Min(distanceToTarget, speed * elapsed.TotalSeconds);
    }
}
