namespace CastoPet.Core.Wheel;

public sealed record WheelActionItem(
    string Id,
    string DisplayName,
    WheelActionType ActionType,
    string? ActionReference,
    bool IsEnabled = true);
