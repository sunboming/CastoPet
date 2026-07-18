using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace CastoPet.Core;

public sealed class TrayService : IDisposable
{
    public const string ShowOrRestoreText = "显示/恢复";
    public const string SettingsText = "设置";
    public const string ExitText = "退出";

    private readonly MenuCommandService _commands;
    private readonly Drawing.Icon _applicationIcon;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly IReadOnlyList<(SettingDefinition Definition, Forms.ToolStripMenuItem Item)> _settingItems;

    public TrayService(MenuCommandService commands)
    {
        _commands = commands;
        _settingItems = SettingCatalog.Create(commands)
            .Where(definition => definition.ShowInDirectMenu)
            .Select(definition => (definition, CreateCheckedItem(definition.Label, definition.Toggle)))
            .ToArray();

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(ShowOrRestoreText, null, (_, _) => _commands.ShowOrRestore());
        menu.Items.Add(new Forms.ToolStripSeparator());
        foreach (var (_, item) in _settingItems)
        {
            menu.Items.Add(item);
        }
        menu.Items.Add(SettingsText, null, (_, _) => _commands.ShowSettings());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(ExitText, null, (_, _) => _commands.Exit());

        _applicationIcon = ApplicationIconService.LoadTrayIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "CastoPet",
            Icon = _applicationIcon,
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
        foreach (var (definition, item) in _settingItems)
        {
            item.Checked = definition.GetValue();
        }
    }

    public void Dispose()
    {
        _commands.SettingsChanged -= RefreshChecks;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _applicationIcon.Dispose();
    }
}
