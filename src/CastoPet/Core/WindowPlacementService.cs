using System.Windows;

namespace CastoPet.Core;

public static class WindowPlacementService
{
    public static Rect CalculateBottomRight(
        double workAreaLeft,
        double workAreaTop,
        double workAreaWidth,
        double workAreaHeight,
        double windowWidth,
        double windowHeight,
        double margin)
    {
        var left = workAreaLeft + Math.Max(margin, workAreaWidth - windowWidth - margin);
        var top = workAreaTop + Math.Max(margin, workAreaHeight - windowHeight - margin);
        return new Rect(left, top, windowWidth, windowHeight);
    }

    public static void MoveToBottomRight(Window window, double margin = 24)
    {
        var workArea = SystemParameters.WorkArea;
        var width = window.Width > 0 ? window.Width : window.ActualWidth;
        var height = window.Height > 0 ? window.Height : window.ActualHeight;
        var target = CalculateBottomRight(
            workArea.Left,
            workArea.Top,
            workArea.Width,
            workArea.Height,
            width,
            height,
            margin);

        window.Left = target.Left;
        window.Top = target.Top;
    }
}
