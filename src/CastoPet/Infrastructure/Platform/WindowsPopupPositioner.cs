using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using CastoPet.Core.Wheel;

namespace CastoPet.Infrastructure.Platform;

public static class WindowsPopupPositioner
{
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;

    public static bool TryCenterAt(Visual popupContent, System.Windows.Point invocationDevicePoint)
    {
        if (PresentationSource.FromVisual(popupContent) is not HwndSource source
            || source.Handle == IntPtr.Zero
            || !GetWindowRect(source.Handle, out var bounds))
        {
            return false;
        }

        var width = bounds.Right - bounds.Left;
        var height = bounds.Bottom - bounds.Top;
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        var placement = RadialWheelPopupPosition.Calculate(
            invocationDevicePoint.X,
            invocationDevicePoint.Y,
            width,
            height);
        return SetWindowPos(
            source.Handle,
            IntPtr.Zero,
            placement.Left,
            placement.Top,
            0,
            0,
            SwpNoSize | SwpNoZOrder | SwpNoActivate);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
