using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CastoPet.Core;

internal static class ShortcutIconService
{
    private const uint FileAttributeDirectory = 0x10;
    private const uint FileAttributeNormal = 0x80;
    private const uint ShgfiIcon = 0x100;
    private const uint ShgfiSmallIcon = 0x1;
    private const uint ShgfiUseFileAttributes = 0x10;

    public static ImageSource? TryLoadSmallIcon(ShortcutDefinition definition)
    {
        var (path, attributes, useFileAttributes) = ResolveShellLookup(definition);
        var flags = ShgfiIcon | ShgfiSmallIcon;
        if (useFileAttributes)
        {
            flags |= ShgfiUseFileAttributes;
        }

        ShellFileInfo info = default;
        try
        {
            var result = SHGetFileInfo(
                path,
                attributes,
                out info,
                (uint)Marshal.SizeOf<ShellFileInfo>(),
                flags);
            if (result == IntPtr.Zero || info.IconHandle == IntPtr.Zero)
            {
                return null;
            }

            var source = Imaging.CreateBitmapSourceFromHIcon(
                info.IconHandle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            if (info.IconHandle != IntPtr.Zero)
            {
                _ = DestroyIcon(info.IconHandle);
            }
        }
    }

    private static (string Path, uint Attributes, bool UseFileAttributes) ResolveShellLookup(
        ShortcutDefinition definition)
    {
        if (definition.Type == ShortcutType.WebUrl)
        {
            return ("shortcut.url", FileAttributeNormal, true);
        }

        var exists = File.Exists(definition.Target) || Directory.Exists(definition.Target);
        var attributes = definition.Type == ShortcutType.Folder
            ? FileAttributeDirectory
            : FileAttributeNormal;
        return (definition.Target, attributes, !exists);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShellFileInfo
    {
        public IntPtr IconHandle;
        public int IconIndex;
        public uint Attributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string TypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string path,
        uint fileAttributes,
        out ShellFileInfo fileInfo,
        uint fileInfoSize,
        uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr iconHandle);
}
