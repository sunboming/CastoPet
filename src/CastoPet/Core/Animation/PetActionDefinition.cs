namespace CastoPet.Core;

public sealed record PetActionDefinition(
    string Id,
    PetActionKind Kind,
    IReadOnlyList<string> FramePaths,
    TimeSpan? FrameInterval = null,
    double? DistancePerFrame = null,
    TimeSpan? MinScheduleDelay = null,
    TimeSpan? MaxScheduleDelay = null,
    double? BaseSpeedPixelsPerSecond = null,
    double? MinSpeedPixelsPerSecond = null,
    double? MaxSpeedPixelsPerSecond = null,
    IReadOnlyList<TimeSpan?>? FrameDurations = null);
