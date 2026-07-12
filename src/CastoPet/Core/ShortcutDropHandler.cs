using System.IO;

namespace CastoPet.Core;

public sealed class ShortcutDropHandler
{
    private readonly ShortcutService _shortcutService;

    public ShortcutDropHandler(ShortcutService shortcutService)
    {
        _shortcutService = shortcutService;
    }

    public ShortcutDropResult AddDroppedItems(
        IReadOnlyList<string> paths,
        IReadOnlyList<string> textValues)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(textValues);

        var counts = new DropCounts();
        foreach (var path in paths)
        {
            AddPath(path, counts);
        }

        foreach (var textValue in textValues)
        {
            if (TryCreateWebShortcut(textValue, name: null, out var definition))
            {
                AddDefinition(definition, counts);
            }
            else
            {
                counts.Unsupported++;
            }
        }

        return new ShortcutDropResult(
            counts.Added,
            counts.Duplicate,
            counts.Unsupported,
            counts.Failed);
    }

    private void AddPath(string path, DropCounts counts)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            counts.Unsupported++;
            return;
        }

        if (Directory.Exists(path))
        {
            var fullPath = Path.GetFullPath(path);
            AddDefinition(CreateFileSystemShortcut(fullPath, ShortcutType.Folder), counts);
            return;
        }

        if (!File.Exists(path))
        {
            counts.Unsupported++;
            return;
        }

        var extension = Path.GetExtension(path);
        if (extension.Equals(".url", StringComparison.OrdinalIgnoreCase))
        {
            AddInternetShortcut(path, counts);
            return;
        }

        var type = extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
            ? ShortcutType.Program
            : extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase)
                ? ShortcutType.WindowsShortcut
                : ShortcutType.File;
        AddDefinition(CreateFileSystemShortcut(Path.GetFullPath(path), type), counts);
    }

    private void AddInternetShortcut(string path, DropCounts counts)
    {
        try
        {
            var url = File.ReadLines(path)
                .Select(TryReadInternetShortcutUrl)
                .FirstOrDefault(value => value is not null);
            var name = Path.GetFileNameWithoutExtension(path);
            if (TryCreateWebShortcut(url, name, out var definition))
            {
                AddDefinition(definition, counts);
            }
            else
            {
                counts.Unsupported++;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            counts.Failed++;
        }
    }

    private void AddDefinition(ShortcutDefinition definition, DropCounts counts)
    {
        var result = _shortcutService.TryAdd(definition);
        if (result.Added)
        {
            counts.Added++;
        }
        else if (result.Duplicate)
        {
            counts.Duplicate++;
        }
        else
        {
            counts.Failed++;
        }
    }

    private static ShortcutDefinition CreateFileSystemShortcut(string path, ShortcutType type)
    {
        var name = type == ShortcutType.Folder
            ? new DirectoryInfo(path).Name
            : type is ShortcutType.Program or ShortcutType.WindowsShortcut
                ? Path.GetFileNameWithoutExtension(path)
                : Path.GetFileName(path);
        return CreateDefinition(name, type, path);
    }

    private static bool TryCreateWebShortcut(
        string? value,
        string? name,
        out ShortcutDefinition definition)
    {
        definition = null!;
        var target = value?.Trim();
        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !(uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
              uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        definition = CreateDefinition(
            string.IsNullOrWhiteSpace(name) ? uri.IdnHost.ToLowerInvariant() : name,
            ShortcutType.WebUrl,
            target!);
        return true;
    }

    private static string? TryReadInternetShortcutUrl(string line)
    {
        var separatorIndex = line.IndexOf('=');
        if (separatorIndex < 0 ||
            !line[..separatorIndex].Trim().Equals("URL", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return line[(separatorIndex + 1)..].Trim();
    }

    private static ShortcutDefinition CreateDefinition(string name, ShortcutType type, string target) =>
        new(
            Guid.NewGuid().ToString("N"),
            name,
            type,
            target,
            "",
            null,
            0);

    private sealed class DropCounts
    {
        public int Added { get; set; }
        public int Duplicate { get; set; }
        public int Unsupported { get; set; }
        public int Failed { get; set; }
    }
}
