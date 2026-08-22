using CastoPet.Application.Shortcuts;
using CastoPet.Core.Skins;
using CastoPet.Core.Wheel;

namespace CastoPet.Application.Wheel;

public sealed class WheelCatalogService : IDisposable
{
    private readonly PetExpressionDefinition[] _expressions;
    private readonly ShortcutService _shortcuts;
    private readonly object _gate = new();
    private WheelCatalog _current;
    private bool _disposed;

    public WheelCatalogService(
        IEnumerable<PetExpressionDefinition> expressions,
        ShortcutService shortcuts)
    {
        ArgumentNullException.ThrowIfNull(expressions);
        ArgumentNullException.ThrowIfNull(shortcuts);

        _expressions = expressions.ToArray();
        _shortcuts = shortcuts;
        _current = CreateCurrent();
        _shortcuts.Changed += OnShortcutsChanged;
    }

    public event EventHandler? Changed;

    public WheelCatalog Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public static WheelCatalog Create(
        IEnumerable<PetExpressionDefinition> expressions,
        IEnumerable<WheelActionItem> shortcuts)
    {
        ArgumentNullException.ThrowIfNull(expressions);
        ArgumentNullException.ThrowIfNull(shortcuts);

        var expressionItems = expressions
            .Select(expression => new WheelActionItem(
                expression.Id,
                expression.Label,
                WheelActionType.Expression,
                expression.Id))
            .ToArray();
        var shortcutItems = shortcuts.ToArray();

        if (shortcutItems.Length == 0)
        {
            shortcutItems =
            [
                new WheelActionItem(
                    "shortcuts-empty",
                    "拖入项目添加",
                    WheelActionType.Disabled,
                    null,
                    IsEnabled: false),
            ];
        }

        WheelCategory[] categories =
        [
            new WheelCategory("expressions", "表情", expressionItems),
            new WheelCategory("shortcuts", "快捷启动", shortcutItems),
        ];

        return new WheelCatalog(categories);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _shortcuts.Changed -= OnShortcutsChanged;
        }
    }

    private void OnShortcutsChanged(object? sender, EventArgs e)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _current = CreateCurrent();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private WheelCatalog CreateCurrent()
    {
        var shortcutItems = _shortcuts.GetAll()
            .Select(shortcut => new WheelActionItem(
                shortcut.Id,
                shortcut.Name,
                WheelActionType.Shortcut,
                shortcut.Id))
            .ToArray();
        return Create(_expressions, shortcutItems);
    }
}
