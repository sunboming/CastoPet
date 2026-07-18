using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using Wpf = System.Windows;

namespace CastoPet.Core;

public static class ApplicationIconService
{
    private static readonly Uri IconResourceUri = new(
        "/CastoPet;component/Assets/AppIcon.ico",
        UriKind.Relative);

    public static Drawing.Icon LoadTrayIcon()
    {
        var resource = Wpf.Application.GetResourceStream(IconResourceUri)
            ?? throw new InvalidOperationException("Packaged application icon is missing.");
        var size = Forms.SystemInformation.SmallIconSize;
        using var stream = resource.Stream;
        using var source = new Drawing.Icon(stream, size.Width, size.Height);
        return (Drawing.Icon)source.Clone();
    }
}
