namespace CastoPet.Core;

public static class ExpressionWheelCatalog
{
    public const int ItemCount = 8;
    public const int DividerCount = ItemCount;
    public const bool UsesPreviewImages = false;
    public const double WheelDiameter = 280;
    public const double WheelOuterDiameter = 256;
    public const double WheelInnerDiameter = 84;
    public const double InnerRadius = 34;
    public const double OuterRadius = 124;
    public const double SelectedScale = 1.18;
    public static readonly TimeSpan HoldDelay = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan ExpressionDuration = TimeSpan.FromSeconds(2);

    public static readonly IReadOnlyList<ExpressionWheelItem> Items = new[]
    {
        Create("Happy"),
        Create("Shy"),
        Create("Sleepy"),
        Create("Surprised"),
        Create("Pouting"),
        Create("Confused"),
        Create("Proud"),
        Create("Crying"),
    };

    private static ExpressionWheelItem Create(string label)
    {
        return new ExpressionWheelItem(label, $"Assets/Expressions/Castorice.Expression.{label}.png");
    }
}
