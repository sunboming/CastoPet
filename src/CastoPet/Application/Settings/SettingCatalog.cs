using CastoPet.Application.Menus;
using CastoPet.Core.Settings;

namespace CastoPet.Application.Settings;

public static class SettingCatalog
{
    public static IReadOnlyList<SettingDefinition> Create(
        AppSettings settings,
        SettingActions actions)
    {
        return
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
                "click-through",
                "鼠标穿透",
                "让鼠标操作穿过桌宠窗口",
                SettingGroup.Interaction,
                ShowInDirectMenu: true,
                () => settings.ClickThrough,
                actions.ToggleClickThrough),
            new(
                "show-in-taskbar",
                "显示任务栏图标",
                "在 Windows 任务栏中显示桌宠窗口",
                SettingGroup.System,
                ShowInDirectMenu: true,
                () => settings.ShowInTaskbar,
                actions.ToggleShowInTaskbar),
            new(
                "start-with-windows",
                "开机自启动",
                "登录 Windows 后自动启动 CastoPet",
                SettingGroup.System,
                ShowInDirectMenu: true,
                () => settings.StartWithWindows,
                actions.ToggleStartWithWindows),
        ];
    }

    public static IReadOnlyList<SettingDefinition> Create(MenuCommandService commands)
    {
        return Create(
            commands.Settings,
            new SettingActions(
                commands.ToggleTopmost,
                commands.ToggleClickThrough,
                commands.ToggleShowInTaskbar,
                commands.ToggleStartWithWindows));
    }
}
