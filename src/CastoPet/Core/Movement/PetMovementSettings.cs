namespace CastoPet.Core.Movement;

public sealed record PetMovementSettings(
    double DistancePerFrame = 10,
    double BaseSpeedPixelsPerSecond = 90,
    double MinSpeedPixelsPerSecond = 80,
    double MaxSpeedPixelsPerSecond = 105)
{
    public void Validate()
    {
        foreach (var value in new[] { DistancePerFrame, BaseSpeedPixelsPerSecond, MinSpeedPixelsPerSecond, MaxSpeedPixelsPerSecond })
        {
            if (!double.IsFinite(value) || value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Movement settings must be finite and positive.");
            }
        }

        if (MinSpeedPixelsPerSecond > BaseSpeedPixelsPerSecond || BaseSpeedPixelsPerSecond > MaxSpeedPixelsPerSecond)
        {
            throw new ArgumentException("Movement base speed must be within its speed range.");
        }
    }
}
