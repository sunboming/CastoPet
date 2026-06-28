using System.Runtime.InteropServices;
using WpfPoint = System.Windows.Point;

namespace CastoPet.Core;

public sealed class WindowsCursorService
{
    public WpfPoint GetPosition()
    {
        return GetCursorPos(out var point)
            ? new WpfPoint(point.X, point.Y)
            : new WpfPoint();
    }

    public void SetPosition(double x, double y)
    {
        SetCursorPos((int)Math.Round(x), (int)Math.Round(y));
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint
    {
        public readonly int X;
        public readonly int Y;
    }
}
