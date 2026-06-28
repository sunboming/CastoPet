namespace CastoPet.Core;

public static class ExpressionWheelSelector
{
    public static int? GetSelectedIndex(
        double pointerX,
        double pointerY,
        double originX,
        double originY,
        int itemCount)
    {
        if (itemCount <= 0)
        {
            return null;
        }

        var vectorX = pointerX - originX;
        var vectorY = pointerY - originY;
        var distance = Math.Sqrt(vectorX * vectorX + vectorY * vectorY);
        if (distance < ExpressionWheelCatalog.InnerRadius || distance > ExpressionWheelCatalog.OuterRadius)
        {
            return null;
        }

        var angle = Math.Atan2(vectorY, vectorX) + Math.PI / 2;
        if (angle < 0)
        {
            angle += 2 * Math.PI;
        }

        return (int)Math.Round(angle / (2 * Math.PI / itemCount)) % itemCount;
    }
}
