using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CacheService.Models;
using CacheService.Parser;
using CacheService.Storage;

namespace CacheService.Server;

public class TcpServer
{
    private static readonly byte[] OkResponse = "OK\r\n"u8.ToArray();
    private static readonly byte[] NilResponse = "(nil)\r\n"u8.ToArray();
    private static readonly byte[] ErrResponse = "-ERR Unknown command\r\n"u8.ToArray();
    private readonly string _host;
    private readonly int _port;
    private readonly SimpleStore _store;

    public TcpServer(string host, int port, SimpleStore store)
    {
        ArgumentException.ThrowIfNullOrEmpty(host);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(port, other: 0);

        _host = host;
        _port = port;
        _store = store;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var serverEndpoint = new IPEndPoint(IPAddress.Parse(_host), _port);
        using var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        serverSocket.Bind(serverEndpoint);
        serverSocket.Listen();

        Console.WriteLine("CacheService is ready to listen...");

        while (!cancellationToken.IsCancellationRequested)
        {
            var clientSocket = await serverSocket.AcceptAsync(cancellationToken);

            _ = Task.Run(() => ProcessClientAsync(clientSocket, cancellationToken), cancellationToken);
        }

        Console.WriteLine("CacheService is stopped...");
    }

    private async Task ProcessClientAsync(Socket clientSocket, CancellationToken cancellationToken)
    {
        var arrayPool = ArrayPool<byte>.Shared;

        var array = arrayPool.Rent(minimumLength: 1024);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var length = await clientSocket.ReceiveAsync(array, cancellationToken);

                if (length == 0)
                {
                    Console.WriteLine("Client disconnected");

                    break;
                }

                var memory = new ReadOnlyMemory<byte>(array, start: 0, length);
                var result = CommandParser.Parse(memory.Span);

                var (command, key, value) = result.Decode();

                switch (command)
                {
                    case "SET":
                        var profile = JsonSerializer.Deserialize<UserProfile>(result.Value);

                        if (profile is null)
                        {
                            await clientSocket.SendAsync(ErrResponse);
                        }
                        else
                        {
                            _store.Set(key, profile);

                            await clientSocket.SendAsync(OkResponse);
                        }

                        break;

                    case "GET":
                        var storedValue = _store.Get(key);

                        if (storedValue is null)
                        {
                            await clientSocket.SendAsync(NilResponse);
                        }
                        else
                        {
                            var storedValueBytes = JsonSerializer.SerializeToUtf8Bytes(storedValue);

                            await clientSocket.SendAsync(storedValueBytes);
                        }

                        break;

                    case "DELETE":
                        _store.Delete(key);

                        await clientSocket.SendAsync(OkResponse);

                        break;

                    default: await clientSocket.SendAsync(ErrResponse); break;
                }

                Console.WriteLine("Received a command: {0} {1} {2}", command, key, value);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

            await clientSocket.SendAsync(Encoding.UTF8.GetBytes($"{e.Message}\r\n"));
        }
        finally
        {
            arrayPool.Return(array);

            CloseSocket(clientSocket);
        }
    }

    private static void CloseSocket(Socket clientSocket)
    {
        try
        {
            clientSocket.Shutdown(SocketShutdown.Both);
        }
        catch
        {
            // ignored
        }

        clientSocket.Close();
        clientSocket.Dispose();
    }
}