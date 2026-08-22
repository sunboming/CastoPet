using System.Runtime.InteropServices;
using WpfPoint = System.Windows.Point;

namespace CastoPet.Infrastructure.Platform;

public sealed class WindowsCursorService
{
    private const int VirtualKeyLeftButton = 0x01;
    private const int VirtualKeyRightButton = 0x02;
    private const int VirtualKeyMiddleButton = 0x04;
    private const int VirtualKeyXButton1 = 0x05;
    private const int VirtualKeyXButton2 = 0x06;

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

    public bool IsAnyMouseButtonPressed()
    {
        return IsPressed(VirtualKeyLeftButton)
            || IsPressed(VirtualKeyRightButton)
            || IsPressed(VirtualKeyMiddleButton)
            || IsPressed(VirtualKeyXButton1)
            || IsPressed(VirtualKeyXButton2);
    }

    private static bool IsPressed(int virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint
    {
        public readonly int X;
        public readonly int Y;
    }
}
