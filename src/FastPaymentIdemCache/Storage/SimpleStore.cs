using System.Text.Json;
using FastPaymentIdemCache.Models;

namespace FastPaymentIdemCache.Storage;

public sealed class SimpleStore : IDisposable
{
    private readonly Dictionary<string, byte[]> _store = new();
    private readonly ReaderWriterLockSlim _lock = new();

    private long _setCount;
    private long _getCount;
    private long _deleteCount;

    public void Set(string key, UserProfile profile)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            _lock.EnterWriteLock();

            using var ms = new MemoryStream();
            
            profile.SerializeToBinary(ms);
            
            _store[key] = ms.ToArray();

            Interlocked.Increment(ref _setCount);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public UserProfile? Get(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            _lock.EnterReadLock();

            var value = _store.GetValueOrDefault(key);

            Interlocked.Increment(ref _getCount);

            return value is null
                ? null
                : JsonSerializer.Deserialize<UserProfile>(value);
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