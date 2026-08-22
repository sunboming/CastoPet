namespace CastoPet.Core;

public sealed record WheelCategory(
    string Id,
    string DisplayName,
    IReadOnlyList<WheelActionItem> Items);
