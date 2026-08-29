namespace CastoPet.Core.Settings;

public enum SettingGroup
{
    Behavior,
    Interaction,
    System,
}

public sealed record SettingDefinition(
    string Id,
    string Label,
    string Description,
    SettingGroup Group,
    bool ShowInDirectMenu,
    Func<bool> GetValue,
    Action Toggle);

public sealed record SettingActions(
    Action ToggleTopmost,
    Action ToggleActiveMovement,
    Action ToggleClickThrough,
    Action TogglePushCursor,
    Action ToggleShowInTaskbar,
    Action ToggleStartWithWindows)
{
    private static readonly Action NoOp = () => { };

    public static SettingActions None { get; } = new(
        NoOp,
        NoOp,
        NoOp,
        NoOp,
        NoOp,
        NoOp);
}
