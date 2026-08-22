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
    private readonly Forms.ContextMenuStrip _contextMenu;
    private readonly IReadOnlyList<(SettingDefinition Definition, Forms.ToolStripMenuItem Item)> _settingItems;
    private bool _disposed;

    public TrayService(
        MenuCommandService commands,
        CastoPetFeatureProfile? features = null,
        CastoPetProductIdentity? identity = null)
    {
        _commands = commands;
        identity ??= CastoPetProductIdentity.Current;
        _settingItems = SettingCatalog.Create(commands, features)
            .Where(definition => definition.ShowInDirectMenu)
            .Select(definition => (definition, CreateCheckedItem(definition.Label, definition.Toggle)))
            .ToArray();

        _contextMenu = new Forms.ContextMenuStrip();
        _contextMenu.Items.Add(ShowOrRestoreText, null, (_, _) => _commands.ShowOrRestore());
        _contextMenu.Items.Add(new Forms.ToolStripSeparator());
        foreach (var (_, item) in _settingItems)
        {
            _contextMenu.Items.Add(item);
        }
        _contextMenu.Items.Add(SettingsText, null, (_, _) => _commands.ShowSettings());
        _contextMenu.Items.Add(new Forms.ToolStripSeparator());
        _contextMenu.Items.Add(ExitText, null, (_, _) => _commands.Exit());

        _applicationIcon = ApplicationIconService.LoadTrayIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = identity.DisplayName,
            Icon = _applicationIcon,
            ContextMenuStrip = _contextMenu,
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
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _commands.SettingsChanged -= RefreshChecks;
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip = null;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _applicationIcon.Dispose();
    }
}
