namespace CastoPet.Core.Wheel;

public sealed record WheelCategory(
    string Id,
    string DisplayName,
    IReadOnlyList<WheelActionItem> Items);
