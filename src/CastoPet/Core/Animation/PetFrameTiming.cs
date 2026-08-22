namespace CastoPet.Core.Animation;

public static class PetFrameTiming
{
    public static TimeSpan GetDuration(
        PetActionDefinition? action,
        int frameIndex,
        TimeSpan fallback)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frameIndex);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fallback, TimeSpan.Zero);

        if (action?.FrameDurations is { } durations
            && frameIndex < durations.Count
            && durations[frameIndex] is TimeSpan authored)
        {
            return authored;
        }

        return action?.FrameInterval ?? fallback;
    }

    public static TimeSpan GetTotalDuration(
        PetActionDefinition? action,
        int frameCount,
        TimeSpan fallback)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frameCount);

        var total = TimeSpan.Zero;
        for (var index = 0; index < frameCount; index++)
        {
            total += GetDuration(action, index, fallback);
        }

        return total;
    }
}
