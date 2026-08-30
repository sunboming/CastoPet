using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CastoPet.Infrastructure.Platform;

public static class ClickThroughService
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;

    public static void Apply(Window window, bool clickThrough, bool showInTaskbar)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var style = GetWindowLong(handle, GwlExStyle);

        if (clickThrough)
        {
            style |= WsExTransparent;
        }
        else
        {
            style &= ~WsExTransparent;
        }

        if (showInTaskbar)
        {
            style &= ~WsExToolWindow;
        }
        else
        {
            style |= WsExToolWindow;
        }

        SetWindowLong(handle, GwlExStyle, style);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
