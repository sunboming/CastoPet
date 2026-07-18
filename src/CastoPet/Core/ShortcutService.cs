using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CastoPet.Core;

public sealed class ShortcutService
{
    public const int MaxEntries = 128;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly AppPaths _paths;
    private readonly LoggingService _logger;
    private readonly object _gate = new();
    private List<ShortcutDefinition> _entries = [];

    public ShortcutService(AppPaths paths, LoggingService logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public event EventHandler? Changed;

    public void Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_paths.ShortcutsFile))
            {
                _entries = [];
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(_paths.ShortcutsFile));
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    throw new JsonException("Shortcut storage root must be an array.");
                }

                var loaded = new List<ShortcutDefinition>();
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    try
                    {
                        var entry = element.Deserialize<ShortcutDefinition>(JsonOptions);
                        if (entry is not null && IsValid(entry) && loaded.Count < MaxEntries &&
                            !loaded.Any(existing => HasSameIdentity(existing, entry)))
                        {
                            loaded.Add(entry);
                        }
                    }
                    catch (Exception ex) when (ex is JsonException or NotSupportedException)
                    {
                        SafeLog("Ignored a malformed shortcut entry.", ex);
                    }
                }

                _entries = OrderAndRenumber(loaded);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                BackupInvalidFile();
                _entries = [];
                SafeLog("Shortcut storage could not be loaded; recovered with an empty list.", ex);
            }
        }
    }

    public IReadOnlyList<ShortcutDefinition> GetAll()
    {
        lock (_gate)
        {
            return _entries.OrderBy(entry => entry.SortOrder).ToArray();
        }
    }

    public ShortcutMutationResult TryAdd(ShortcutDefinition candidate)
    {
        lock (_gate)
        {
            if (!IsValid(candidate))
            {
                return new(false, Error: "Shortcut is invalid.");
            }

            var duplicateIndex = _entries.FindIndex(entry => HasSameIdentity(entry, candidate));
            if (duplicateIndex >= 0)
            {
                var existing = _entries[duplicateIndex];
                var enriched = EnrichSteamGameMetadata(existing, candidate);
                if (!existing.Equals(enriched))
                {
                    var enrichedEntries = _entries.ToList();
                    enrichedEntries[duplicateIndex] = enriched;
                    var persisted = PersistMutation(Renumber(enrichedEntries));
                    return persisted.Succeeded
                        ? new(true, Duplicate: true)
                        : persisted;
                }

                return new(true, Duplicate: true);
            }

            if (_entries.Count >= MaxEntries)
            {
                return new(false, Error: "Shortcut limit reached.");
            }

            var next = Renumber([.. _entries.OrderBy(entry => entry.SortOrder), candidate]);
            return PersistMutation(next, added: true);
        }
    }

    public ShortcutMutationResult Rename(string id, string name)
    {
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new(false, Error: "Name is required.");
            }

            var index = _entries.FindIndex(entry => entry.Id == id);
            if (index < 0)
            {
                return new(false, Error: "Shortcut was not found.");
            }

            var next = _entries.ToList();
            next[index] = next[index] with { Name = name.Trim() };
            return PersistMutation(Renumber(next));
        }
    }

    public ShortcutMutationResult Delete(string id)
    {
        lock (_gate)
        {
            var next = _entries.Where(entry => entry.Id != id).ToList();
            if (next.Count == _entries.Count)
            {
                return new(false, Error: "Shortcut was not found.");
            }

            return PersistMutation(OrderAndRenumber(next));
        }
    }

    public ShortcutMutationResult UpdateLaunchOptions(
        string id,
        string? arguments,
        string? workingDirectory)
    {
        lock (_gate)
        {
            var index = _entries.FindIndex(entry => entry.Id == id);
            if (index < 0)
            {
                return new(false, Error: "Shortcut was not found.");
            }

            if (_entries[index].Type != ShortcutType.Program)
            {
                return new(false, Error: "Launch options are only available for programs.");
            }

            var normalizedDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                ? null
                : workingDirectory.Trim();
            if (normalizedDirectory is not null && !Directory.Exists(normalizedDirectory))
            {
                return new(false, Error: "Working directory does not exist.");
            }

            var next = _entries.ToList();
            next[index] = next[index] with
            {
                Arguments = arguments?.Trim() ?? "",
                WorkingDirectory = normalizedDirectory,
            };
            return PersistMutation(OrderAndRenumber(next));
        }
    }

    public ShortcutMutationResult Move(string id, int destinationIndex)
    {
        lock (_gate)
        {
            var ordered = _entries.OrderBy(entry => entry.SortOrder).ToList();
            var sourceIndex = ordered.FindIndex(entry => entry.Id == id);
            if (sourceIndex < 0 || destinationIndex < 0 || destinationIndex >= ordered.Count)
            {
                return new(false, Error: "Move is outside the shortcut list.");
            }

            var item = ordered[sourceIndex];
            ordered.RemoveAt(sourceIndex);
            ordered.Insert(destinationIndex, item);
            return PersistMutation(Renumber(ordered));
        }
    }

    private ShortcutMutationResult PersistMutation(List<ShortcutDefinition> next, bool added = false)
    {
        var temporaryFile = _paths.ShortcutsFile + ".tmp";
        try
        {
            Directory.CreateDirectory(_paths.ShortcutsDirectory);
            var json = JsonSerializer.Serialize(next, JsonOptions);
            using (var stream = new FileStream(temporaryFile, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryFile, _paths.ShortcutsFile, overwrite: true);
            _entries = next;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TryDeleteTemporaryFile(temporaryFile);
            SafeLog("Shortcut storage mutation failed.", ex);
            return new(false, Error: ex.Message);
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return new(true, Added: added);
    }

    private void BackupInvalidFile()
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var backup = Path.Combine(_paths.ShortcutsDirectory, $"shortcuts.invalid-{timestamp}.json");
            var suffix = 1;
            while (File.Exists(backup))
            {
                backup = Path.Combine(_paths.ShortcutsDirectory, $"shortcuts.invalid-{timestamp}-{suffix++}.json");
            }

            File.Copy(_paths.ShortcutsFile, backup);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SafeLog("Malformed shortcut storage could not be backed up.", ex);
        }
    }

    private void SafeLog(string message, Exception exception)
    {
        try
        {
            _logger.Error(message, exception);
        }
        catch (Exception)
        {
        }
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception)
        {
        }
    }

    private static bool IsValid(ShortcutDefinition entry) =>
        !string.IsNullOrWhiteSpace(entry.Id) &&
        !string.IsNullOrWhiteSpace(entry.Name) &&
        !string.IsNullOrWhiteSpace(entry.Target) &&
        Enum.IsDefined(entry.Type);

    private static bool HasSameIdentity(ShortcutDefinition left, ShortcutDefinition right)
    {
        if (IsUriType(left.Type) || IsUriType(right.Type))
        {
            return left.Type == right.Type && string.Equals(NormalizeUrl(left.Target), NormalizeUrl(right.Target), StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(NormalizeWindowsPath(left.Target), NormalizeWindowsPath(right.Target), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUriType(ShortcutType type) =>
        type is ShortcutType.WebUrl or ShortcutType.SteamGame;

    private static ShortcutDefinition EnrichSteamGameMetadata(
        ShortcutDefinition existing,
        ShortcutDefinition candidate)
    {
        if (existing.Type != ShortcutType.SteamGame || candidate.Type != ShortcutType.SteamGame ||
            string.IsNullOrWhiteSpace(candidate.IconPath))
        {
            return existing;
        }

        var name = existing.Name;
        if (ShortcutUriPolicy.TryGetSteamGameUri(existing.Target, out _, out var gameId) &&
            string.Equals(name, $"Steam {gameId}", StringComparison.Ordinal))
        {
            name = candidate.Name;
        }

        return existing with
        {
            Name = name,
            IconPath = candidate.IconPath,
        };
    }

    private static string NormalizeWindowsPath(string path)
    {
        var normalized = Path.GetFullPath(path.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return normalized.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    private static string NormalizeUrl(string value)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return value.Trim();
        }

        var builder = new UriBuilder(uri)
        {
            Host = uri.IdnHost.ToLowerInvariant(),
            Fragment = string.Empty,
        };
        if ((builder.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && builder.Port == 443) ||
            (builder.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && builder.Port == 80))
        {
            builder.Port = -1;
        }

        return builder.Uri.AbsoluteUri.TrimEnd('/');
    }

    private static List<ShortcutDefinition> OrderAndRenumber(IEnumerable<ShortcutDefinition> entries) =>
        Renumber(entries.OrderBy(entry => entry.SortOrder));

    private static List<ShortcutDefinition> Renumber(IEnumerable<ShortcutDefinition> entries) =>
        entries
            .Select((entry, index) => entry with { SortOrder = index })
            .ToList();

}
