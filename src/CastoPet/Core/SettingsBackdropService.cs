using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CastoPet.Core;

public static class SettingsBackdropService
{
    private const int UseImmersiveDarkMode = 20;
    private const int WindowCornerPreference = 33;
    private const int SystemBackdropType = 38;
    private const int RoundCorners = 2;
    private const int DesktopAcrylic = 3;
    private const int NoSystemBackdrop = 1;
    private const int AccentPolicyAttribute = 19;
    private const int AcrylicBlurBehind = 4;

    public static bool IsSupported(Version version)
    {
        return version.Major > 10 || (version.Major == 10 && version.Build >= 22621);
    }

    public static uint PackAccentColor(byte alpha, byte red, byte green, byte blue)
    {
        return ((uint)alpha << 24) | ((uint)blue << 16) | ((uint)green << 8) | red;
    }

    public static bool TryApply(Window window, bool useDarkFrame)
    {
        if (!OperatingSystem.IsWindows() || !IsSupported(Environment.OSVersion.Version))
        {
            return false;
        }

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            var margins = new Margins(-1);
            _ = DwmExtendFrameIntoClientArea(handle, ref margins);
            if (HwndSource.FromHwnd(handle)?.CompositionTarget is { } compositionTarget)
            {
                compositionTarget.BackgroundColor = System.Windows.Media.Colors.Transparent;
            }

            var dark = useDarkFrame ? 1 : 0;
            var corners = RoundCorners;
            _ = DwmSetWindowAttribute(handle, UseImmersiveDarkMode, ref dark, sizeof(int));
            _ = DwmSetWindowAttribute(handle, WindowCornerPreference, ref corners, sizeof(int));

            var noBackdrop = NoSystemBackdrop;
            _ = DwmSetWindowAttribute(handle, SystemBackdropType, ref noBackdrop, sizeof(int));
            if (TryApplyAccentAcrylic(handle, useDarkFrame))
            {
                return true;
            }

            var backdrop = DesktopAcrylic;
            return DwmSetWindowAttribute(handle, SystemBackdropType, ref backdrop, sizeof(int)) >= 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    private static bool TryApplyAccentAcrylic(IntPtr handle, bool useDarkFrame)
    {
        var tint = useDarkFrame
            ? PackAccentColor(0x68, 0x26, 0x1A, 0x32)
            : PackAccentColor(0x54, 0xE4, 0xD2, 0xF1);
        var policy = new AccentPolicy
        {
            State = AcrylicBlurBehind,
            Flags = 2,
            GradientColor = tint,
        };
        var policySize = Marshal.SizeOf<AccentPolicy>();
        var policyPointer = Marshal.AllocHGlobal(policySize);
        try
        {
            Marshal.StructureToPtr(policy, policyPointer, fDeleteOld: false);
            var data = new WindowCompositionAttributeData
            {
                Attribute = AccentPolicyAttribute,
                Data = policyPointer,
                SizeOfData = policySize,
            };
            return SetWindowCompositionAttribute(handle, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(policyPointer);
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int valueSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr window, ref Margins margins);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowCompositionAttribute(IntPtr window, ref WindowCompositionAttributeData data);

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int State;
        public int Flags;
        public uint GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Margins
    {
        public Margins(int value)
        {
            Left = value;
            Right = value;
            Top = value;
            Bottom = value;
        }

        public int Left { get; }
        public int Right { get; }
        public int Top { get; }
        public int Bottom { get; }
    }
}
