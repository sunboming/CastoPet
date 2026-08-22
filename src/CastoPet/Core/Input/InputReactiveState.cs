namespace CastoPet.Core;

public sealed class InputReactiveState
{
    public static readonly TimeSpan HighlightDuration = TimeSpan.FromMilliseconds(160);

    private readonly Dictionary<string, TimeSpan> _expiresAt = new(StringComparer.OrdinalIgnoreCase);

    public void AddKey(string key, TimeSpan now)
    {
        _expiresAt[key] = now + HighlightDuration;
    }

    public IReadOnlyList<string> GetActiveHighlights(TimeSpan now)
    {
        foreach (var key in _expiresAt
            .Where(pair => pair.Value <= now)
            .Select(pair => pair.Key)
            .ToArray())
        {
            _expiresAt.Remove(key);
        }

        return _expiresAt.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public void Clear()
    {
        _expiresAt.Clear();
    }
}
