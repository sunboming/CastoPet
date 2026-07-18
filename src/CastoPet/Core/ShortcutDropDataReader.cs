using System.IO;
using System.Text;
using System.Windows;
using WpfDataFormats = System.Windows.DataFormats;

namespace CastoPet.Core;

internal static class ShortcutDropDataReader
{
    private static readonly string[] UrlDropFormats = ["UniformResourceLocatorW", "UniformResourceLocator"];

    public static bool ContainsSupportedFormat(System.Windows.IDataObject data)
    {
        if (data.GetDataPresent(WpfDataFormats.FileDrop, autoConvert: true) ||
            data.GetDataPresent(WpfDataFormats.UnicodeText, autoConvert: true) ||
            data.GetDataPresent(WpfDataFormats.Text, autoConvert: true) ||
            data.GetDataPresent(WpfDataFormats.StringFormat, autoConvert: true))
        {
            return true;
        }

        return UrlDropFormats.Any(format => data.GetDataPresent(format, autoConvert: true));
    }

    public static IReadOnlyList<string> ExtractPaths(System.Windows.IDataObject data)
    {
        if (!data.GetDataPresent(WpfDataFormats.FileDrop, autoConvert: true))
        {
            return [];
        }

        return data.GetData(WpfDataFormats.FileDrop, autoConvert: true) is string[] paths
            ? paths.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray()
            : [];
    }

    public static IReadOnlyList<string> ExtractTextValues(System.Windows.IDataObject data)
    {
        string[] formats =
        [
            WpfDataFormats.UnicodeText,
            WpfDataFormats.Text,
            WpfDataFormats.StringFormat,
            .. UrlDropFormats,
        ];
        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var format in formats)
        {
            if (!data.GetDataPresent(format, autoConvert: true))
            {
                continue;
            }

            var value = ReadText(data.GetData(format, autoConvert: true), format);
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values.ToArray();
    }

    private static string? ReadText(object? value, string format)
    {
        if (value is string text)
        {
            return NormalizeText(text);
        }

        if (value is byte[] bytes)
        {
            var encoding = format.EndsWith('W') ? Encoding.Unicode : Encoding.UTF8;
            return NormalizeText(encoding.GetString(bytes));
        }

        if (value is not Stream stream)
        {
            return null;
        }

        var originalPosition = stream.CanSeek ? stream.Position : (long?)null;
        try
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            var encoding = format.EndsWith('W') ? Encoding.Unicode : Encoding.UTF8;
            using var reader = new StreamReader(
                stream,
                encoding,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024,
                leaveOpen: true);
            return NormalizeText(reader.ReadToEnd());
        }
        finally
        {
            if (originalPosition is long position)
            {
                stream.Position = position;
            }
        }
    }

    private static string? NormalizeText(string text)
    {
        var normalized = text.Trim('\0', ' ', '\t', '\r', '\n');
        return normalized.Length == 0 ? null : normalized;
    }
}
