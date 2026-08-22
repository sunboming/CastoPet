using System.Runtime.InteropServices;

using CastoPet.Core.Input;

namespace CastoPet.Infrastructure.Platform;

public sealed class WindowsInputHookService : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int WmLButtonDown = 0x0201;
    private const int WmRButtonDown = 0x0204;
    private const int WmMButtonDown = 0x0207;

    private readonly HookProc _keyboardProc;
    private readonly HookProc _mouseProc;
    private nint _keyboardHook;
    private nint _mouseHook;
    private bool _disposed;

    public WindowsInputHookService()
    {
        _keyboardProc = HandleKeyboardHook;
        _mouseProc = HandleMouseHook;
    }

    public event Action<InputReactiveEvent>? InputReceived;

    public bool IsRunning => _keyboardHook != 0 || _mouseHook != 0;

    public void Start()
    {
        if (_disposed || IsRunning)
        {
            return;
        }

        var module = GetModuleHandle(null);
        _keyboardHook = SetWindowsHookEx(WhKeyboardLl, _keyboardProc, module, 0);
        _mouseHook = SetWindowsHookEx(WhMouseLl, _mouseProc, module, 0);
    }

    public void Stop()
    {
        if (_keyboardHook != 0)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = 0;
        }

        if (_mouseHook != 0)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = 0;
        }
    }

    public static string? NormalizeVirtualKey(int virtualKey)
    {
        if (virtualKey is >= 0x41 and <= 0x5A)
        {
            return ((char)virtualKey).ToString();
        }

        if (virtualKey is >= 0x30 and <= 0x39)
        {
            return ((char)virtualKey).ToString();
        }

        return virtualKey switch
        {
            0x08 => "Backspace",
            0x0D => "Enter",
            0x10 => "Shift",
            0x11 => "Ctrl",
            0x12 => "Alt",
            0x20 => "Space",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            _ => null,
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }

    private nint HandleKeyboardHook(int code, nint wParam, nint lParam)
    {
        if (code >= 0 && (wParam == WmKeyDown || wParam == WmSysKeyDown))
        {
            var key = NormalizeVirtualKey(Marshal.ReadInt32(lParam));
            if (key is not null)
            {
                InputReceived?.Invoke(new InputReactiveEvent(InputReactiveEventKind.KeyDown, key));
            }
        }

        return CallNextHookEx(_keyboardHook, code, wParam, lParam);
    }

    private nint HandleMouseHook(int code, nint wParam, nint lParam)
    {
        if (code >= 0)
        {
            var id = (int)wParam switch
            {
                WmLButtonDown => "MouseLeft",
                WmRButtonDown => "MouseRight",
                WmMButtonDown => "MouseMiddle",
                _ => null,
            };

            if (id is not null)
            {
                InputReceived?.Invoke(new InputReactiveEvent(InputReactiveEventKind.MouseDown, id));
            }
        }

        return CallNextHookEx(_mouseHook, code, wParam, lParam);
    }

    private delegate nint HookProc(int code, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, HookProc callback, nint hMod, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hook);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandle(string? moduleName);
}
