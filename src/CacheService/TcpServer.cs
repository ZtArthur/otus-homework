using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace CacheService;

public class TcpServer
{
    private readonly string _host;
    private readonly int _port;

    public TcpServer(string host, int port)
    {
        ArgumentException.ThrowIfNullOrEmpty(host);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(port, other: 0);

        _host = host;
        _port = port;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var serverEndpoint = new IPEndPoint(IPAddress.Parse(_host), _port);
        using var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        serverSocket.Bind(serverEndpoint);
        serverSocket.Listen();

        while (!cancellationToken.IsCancellationRequested)
        {
            var clientSocket = await serverSocket.AcceptAsync(cancellationToken);

            _ = Task.Run(() => ProcessClientAsync(clientSocket, cancellationToken), cancellationToken);
        }
    }

    private static async Task ProcessClientAsync(Socket clientSocket, CancellationToken cancellationToken)
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

                Console.WriteLine("Received a command: {0}", result.AsString());
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
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