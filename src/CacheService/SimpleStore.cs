namespace CacheService;

public sealed class SimpleStore : IDisposable
{
    private readonly Dictionary<string, byte[]> _store = new();
    private readonly ReaderWriterLockSlim _lock = new();

    private long _setCount;
    private long _getCount;
    private long _deleteCount;

    public void Set(string key, byte[] value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            _lock.EnterWriteLock();

            _store[key] = value;

            Interlocked.Increment(ref _setCount);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public byte[]? Get(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            _lock.EnterReadLock();

            var value = _store.GetValueOrDefault(key);

            Interlocked.Increment(ref _getCount);

            return value;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Delete(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            _lock.EnterWriteLock();

            _store.Remove(key);

            Interlocked.Increment(ref _deleteCount);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public (long setCount, long getCount, long deleteCount) GetStatistics() => (_setCount, _getCount, _deleteCount);

    /// <inheritdoc />
    public void Dispose()
    {
        _lock.Dispose();
    }
}