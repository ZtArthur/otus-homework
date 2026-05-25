namespace Lesson_3;

public sealed class SimpleStore
{
    private readonly Dictionary<string, byte[]> _store = new();

    public void Set(string key, byte[] value) => _store[key] = value;

    public byte[]? Get(string key) => _store.GetValueOrDefault(key);

    public void Delete(string key) => _store.Remove(key);
}