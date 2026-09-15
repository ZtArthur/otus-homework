using System.Net.Sockets;
using System.Text;

namespace FastPaymentIdemCache.LoadTests;

public class FastPaymentIdemCacheClient : IDisposable
{
    private readonly string _host;
    private readonly int _port;

    private TcpClient? _client;
    private NetworkStream? _networkStream;

    public FastPaymentIdemCacheClient(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task ConnectAsync()
    {
        _client = new TcpClient(_host, _port);

        if (!_client.Connected)
        {
            await _client.ConnectAsync(_host, _port);
        }

        _networkStream = _client.GetStream();
    }

    public async Task<byte[]> SetAsync(string key, byte[] value)
    {
        ArgumentNullException.ThrowIfNull(_networkStream);

        var command = $"SET {key} {Encoding.UTF8.GetString(value)}";

        await WriteAsync(command);

        return await ReadAsync();
    }

    public async Task<byte[]> GetAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(_networkStream);

        var command = $"GET {key}";

        await WriteAsync(command);

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

        return bytesRead > 0 ? buffer : [];
    }

    private async ValueTask WriteAsync(string payload)
    {
        var commandBytes = Encoding.UTF8.GetBytes(payload);

        await _networkStream!.WriteAsync(commandBytes);
    }
}