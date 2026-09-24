using FastPaymentIdemCache.Models;

namespace FastPaymentIdemCache.Storage;

public sealed class SimpleStore : IDisposable
{
    private readonly Dictionary<string, byte[]> _store = new();
    private readonly ReaderWriterLockSlim _lock = new();

    private long _setCount;
    private long _getCount;
    private long _deleteCount;

    public void Set(string key, UserPaymentOperation operation)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            _lock.EnterWriteLock();

            using var ms = new MemoryStream();

            operation.SerializeToBinary(ms);
            
            _store[key] = ms.ToArray();

            Interlocked.Increment(ref _setCount);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public UserPaymentOperation? Get(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        byte[]? value;

        try
        {
            _lock.EnterReadLock();

            value = _store.GetValueOrDefault(key);

            Interlocked.Increment(ref _getCount);
        }
        finally
        {
            _lock.ExitReadLock();
        }

        if (value is null)
        {
            return null;
        }

        using var ms = new MemoryStream(value);

        return UserPaymentOperation.DeserializeFromBinary(ms);
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

    public int Count
    {
        get
        {
            _lock.EnterReadLock();

            try
            {
                return _store.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public (long setCount, long getCount, long deleteCount) GetStatistics() => (_setCount, _getCount, _deleteCount);

    /// <inheritdoc />
    public void Dispose()
    {
        _lock.Dispose();
    }
}