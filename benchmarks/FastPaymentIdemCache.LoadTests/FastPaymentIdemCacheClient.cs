using System.Net.Sockets;
using System.Text;

namespace FastPaymentIdemCache.LoadTests;

public class FastPaymentIdemCacheClient : IDisposable
{
    private readonly string _host;
    private readonly int _port;

    private TcpClient? _client;
    private NetworkStream? _networkStream;

    public bool IsConnected => _client?.Connected ?? false;

    public FastPaymentIdemCacheClient(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task ConnectAsync()
    {
        _client = new TcpClient { NoDelay = true };

        await _client.ConnectAsync(_host, _port);

        _networkStream = _client.GetStream();
    }

    public async Task<byte[]> SetAsync(string key, byte[] value)
    {
        ArgumentNullException.ThrowIfNull(_networkStream);

        var prefix = Encoding.UTF8.GetBytes($"SET {key} ");
        var command = new byte[prefix.Length + value.Length];

        prefix.CopyTo(command, index: 0);
        value.CopyTo(command, prefix.Length);

        await _networkStream.WriteAsync(command);

        return await ReadAsync();
    }

    public async Task<byte[]> GetAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(_networkStream);

        await WriteAsync($"GET {key}");

        return await ReadAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _networkStream?.Dispose();
        _client?.Dispose();
    }

    private async Task<byte[]> ReadAsync()
    {
        var buffer = new byte[4096];

        var bytesRead = await _networkStream!.ReadAsync(buffer);

        return bytesRead > 0 ? buffer[..bytesRead] : [];
    }

    private async ValueTask WriteAsync(string payload)
    {
        var commandBytes = Encoding.UTF8.GetBytes(payload);

        await _networkStream!.WriteAsync(commandBytes);
    }
}