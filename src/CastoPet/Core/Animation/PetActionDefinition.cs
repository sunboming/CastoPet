namespace CastoPet.Core.Animation;

public sealed record PetActionDefinition(
    string Id,
    PetActionKind Kind,
    IReadOnlyList<string> FramePaths,
    TimeSpan? FrameInterval = null,
    TimeSpan? MinScheduleDelay = null,
    TimeSpan? MaxScheduleDelay = null,
    IReadOnlyList<TimeSpan?>? FrameDurations = null);
