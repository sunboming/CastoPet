namespace CastoPet.Core;

public sealed class BoundedLruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Func<TKey, TValue> _loader;
    private readonly Dictionary<TKey, LinkedListNode<Entry>> _entries;
    private readonly LinkedList<Entry> _usage = new();

    private sealed record Entry(TKey Key, TValue Value);

    public BoundedLruCache(
        int capacity,
        Func<TKey, TValue> loader,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        ArgumentNullException.ThrowIfNull(loader);
        _capacity = capacity;
        _loader = loader;
        _entries = new Dictionary<TKey, LinkedListNode<Entry>>(comparer);
    }

    public int Count => _entries.Count;

    public TValue Get(TKey key)
    {
        if (_entries.TryGetValue(key, out var existing))
        {
            _usage.Remove(existing);
            _usage.AddFirst(existing);
            return existing.Value.Value;
        }

        var value = _loader(key);
        var node = _usage.AddFirst(new Entry(key, value));
        _entries.Add(key, node);
        if (_entries.Count > _capacity && _usage.Last is { } oldest)
        {
            _usage.RemoveLast();
            _entries.Remove(oldest.Value.Key);
        }

        return value;
    }

    public void Clear()
    {
        _entries.Clear();
        _usage.Clear();
    }
}
