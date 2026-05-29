namespace CacheService;

public sealed class SimpleStore
{
    private readonly Dictionary<string, byte[]> _store = new();

    public void Set(string key, byte[] value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        _store[key] = value;
    }

    public byte[]? Get(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        return _store.GetValueOrDefault(key);
    }

    public void Delete(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        _store.Remove(key);
    }
}