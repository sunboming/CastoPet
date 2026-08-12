namespace CastoPet.Core;

public static class SettingCatalog
{
    public static IReadOnlyList<SettingDefinition> Create(
        AppSettings settings,
        SettingActions actions,
        CastoPetFeatureProfile? features = null)
    {
        features ??= CastoPetFeatureProfile.Current;
        List<SettingDefinition> definitions =
        [
            new(
                "topmost",
                "始终置顶",
                "让桌宠保持在其他窗口上方",
                SettingGroup.Behavior,
                ShowInDirectMenu: true,
                () => settings.Topmost,
                actions.ToggleTopmost),
            new(
                "active-movement",
                "主动移动",
                "允许桌宠在桌面上自行移动",
                SettingGroup.Behavior,
                ShowInDirectMenu: false,
                () => settings.ActiveMovement,
                actions.ToggleActiveMovement),
            new(
                "click-through",
                "鼠标穿透",
                "让鼠标操作穿过桌宠窗口",
                SettingGroup.Interaction,
                ShowInDirectMenu: true,
                () => settings.ClickThrough,
                actions.ToggleClickThrough),
            new(
                "push-cursor",
                "推动鼠标",
                "桌宠移动时可以轻推附近的鼠标指针",
                SettingGroup.Interaction,
                ShowInDirectMenu: false,
                () => settings.PushCursor,
                actions.TogglePushCursor),
            new(
                "input-reactive-mode",
                "输入响应模式",
                "使用键盘与鼠标输入响应外观",
                SettingGroup.Interaction,
                ShowInDirectMenu: false,
                () => settings.InputReactiveMode,
                actions.ToggleInputReactiveMode),
            new(
                "show-in-taskbar",
                "显示任务栏图标",
                "在 Windows 任务栏中显示桌宠窗口",
                SettingGroup.System,
                ShowInDirectMenu: false,
                () => settings.ShowInTaskbar,
                actions.ToggleShowInTaskbar),
            new(
                "start-with-windows",
                "开机自启动",
                "登录 Windows 后自动启动 CastoPet",
                SettingGroup.System,
                ShowInDirectMenu: false,
                () => settings.StartWithWindows,
                actions.ToggleStartWithWindows),
        ];

        return definitions
            .Where(definition => IsAvailable(definition.Id, features))
            .ToArray();
    }

    public static IReadOnlyList<SettingDefinition> Create(
        MenuCommandService commands,
        CastoPetFeatureProfile? features = null)
    {
        return Create(
            commands.Settings,
            new SettingActions(
                commands.ToggleTopmost,
                commands.ToggleActiveMovement,
                commands.ToggleClickThrough,
                commands.TogglePushCursor,
                commands.ToggleInputReactiveMode,
                commands.ToggleShowInTaskbar,
                commands.ToggleStartWithWindows),
            features);
    }

    private static bool IsAvailable(string id, CastoPetFeatureProfile features) => id switch
    {
        "active-movement" => features.ActiveMovement,
        "push-cursor" => features.PushCursor,
        "input-reactive-mode" => features.InputReactiveMode,
        _ => true,
    };
}
