using System.IO;

using CastoPet;
using CastoPet.Core.Shortcuts;

namespace CastoPet.Application.Shortcuts;

public sealed class ShortcutDropHandler
{
    private static readonly HashSet<string> UnsupportedExecutableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bat", ".cmd", ".ps1", ".vbs", ".js", ".jse",
        ".wsf", ".wsh", ".hta", ".com", ".scr", ".msi",
    };

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
            if (TryCreateUriShortcut(textValue, name: null, out var definition))
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

        if (UnsupportedExecutableExtensions.Contains(extension))
        {
            counts.Unsupported++;
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
            var lines = File.ReadAllLines(path);
            var url = lines
                .Select(TryReadInternetShortcutUrl)
                .FirstOrDefault(value => value is not null);
            var iconPath = lines
                .Select(TryReadInternetShortcutIconPath)
                .FirstOrDefault(value => value is not null);
            var name = Path.GetFileNameWithoutExtension(path);
            if (TryCreateUriShortcut(url, name, out var definition))
            {
                definition = definition with { IconPath = NormalizeIconPath(iconPath, path) };
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

    private static bool TryCreateUriShortcut(
        string? value,
        string? name,
        out ShortcutDefinition definition)
    {
        definition = null!;
        var target = value?.Trim();
        if (ShortcutUriPolicy.TryGetWebUri(target, out var webUri))
        {
            definition = CreateDefinition(
                string.IsNullOrWhiteSpace(name) ? webUri!.IdnHost.ToLowerInvariant() : name,
                ShortcutType.WebUrl,
                target!);
            return true;
        }

        if (ShortcutUriPolicy.TryGetSteamGameUri(target, out _, out var gameId))
        {
            definition = CreateDefinition(
                string.IsNullOrWhiteSpace(name) ? $"Steam {gameId}" : name,
                ShortcutType.SteamGame,
                target!);
            return true;
        }

        return false;
    }

    private static string? TryReadInternetShortcutUrl(string line)
        => TryReadInternetShortcutValue(line, "URL");

    private static string? TryReadInternetShortcutIconPath(string line)
        => TryReadInternetShortcutValue(line, "IconFile");

    private static string? TryReadInternetShortcutValue(string line, string key)
    {
        var separatorIndex = line.IndexOf('=');
        if (separatorIndex < 0 ||
            !line[..separatorIndex].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return line[(separatorIndex + 1)..].Trim();
    }

    private static string? NormalizeIconPath(string? value, string shortcutPath)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var candidate = Environment.ExpandEnvironmentVariables(value.Trim().Trim('"'));
        if (!Path.IsPathFullyQualified(candidate))
        {
            candidate = Path.Combine(Path.GetDirectoryName(shortcutPath) ?? "", candidate);
        }

        var fullPath = Path.GetFullPath(candidate);
        return File.Exists(fullPath) && Path.GetExtension(fullPath).Equals(".ico", StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : null;
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
