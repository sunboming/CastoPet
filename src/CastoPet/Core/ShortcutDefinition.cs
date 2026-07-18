namespace CastoPet.Core;

public sealed record ShortcutDefinition(
    string Id,
    string Name,
    ShortcutType Type,
    string Target,
    string Arguments,
    string? WorkingDirectory,
    int SortOrder)
{
    public string? IconPath { get; init; }
}

public sealed record ShortcutMutationResult(
    bool Succeeded,
    bool Added = false,
    bool Duplicate = false,
    string? Error = null);
