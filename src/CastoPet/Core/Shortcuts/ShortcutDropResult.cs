namespace CastoPet.Core;

public sealed record ShortcutDropResult(
    int AddedCount,
    int DuplicateCount,
    int UnsupportedCount,
    int FailedCount);
