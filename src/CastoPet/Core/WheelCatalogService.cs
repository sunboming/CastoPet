namespace CastoPet.Core;

public static class WheelCatalogService
{
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
}
