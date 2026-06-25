using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace CastoPet.Core;

public sealed class TrayService : IDisposable
{
    public const string ShowOrRestoreText = "显示/恢复";
    public const string AlwaysOnTopText = "始终置顶";
    public const string MouseClickThroughText = "鼠标穿透";
    public const string ShowTaskbarIconText = "显示任务栏图标";
    public const string StartWithWindowsText = "开机自启动";
    public const string ExitText = "退出";

    private readonly MenuCommandService _commands;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _topmostItem;
    private readonly Forms.ToolStripMenuItem _clickThroughItem;
    private readonly Forms.ToolStripMenuItem _taskbarItem;
    private readonly Forms.ToolStripMenuItem _startupItem;

    public TrayService(MenuCommandService commands)
    {
        _commands = commands;
        _topmostItem = CreateCheckedItem(AlwaysOnTopText, _commands.ToggleTopmost);
        _clickThroughItem = CreateCheckedItem(MouseClickThroughText, _commands.ToggleClickThrough);
        _taskbarItem = CreateCheckedItem(ShowTaskbarIconText, _commands.ToggleShowInTaskbar);
        _startupItem = CreateCheckedItem(StartWithWindowsText, _commands.ToggleStartWithWindows);

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(ShowOrRestoreText, null, (_, _) => _commands.ShowOrRestore());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_topmostItem);
        menu.Items.Add(_clickThroughItem);
        menu.Items.Add(_taskbarItem);
        menu.Items.Add(_startupItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(ExitText, null, (_, _) => _commands.Exit());

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "CastoPet",
            Icon = Drawing.SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true,
        };

        _notifyIcon.DoubleClick += (_, _) => _commands.ShowOrRestore();
        _commands.SettingsChanged += RefreshChecks;
        RefreshChecks();
    }

    private static Forms.ToolStripMenuItem CreateCheckedItem(string text, Action action)
    {
        var item = new Forms.ToolStripMenuItem(text);
        item.Click += (_, _) => action();
        return item;
    }

    private void RefreshChecks()
    {
        _topmostItem.Checked = _commands.Settings.Topmost;
        _clickThroughItem.Checked = _commands.Settings.ClickThrough;
        _taskbarItem.Checked = _commands.Settings.ShowInTaskbar;
        _startupItem.Checked = _commands.Settings.StartWithWindows;
    }

    public void Dispose()
    {
        _commands.SettingsChanged -= RefreshChecks;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
