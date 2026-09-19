using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FastPaymentIdemCache.Models;
using FastPaymentIdemCache.Observability;
using FastPaymentIdemCache.Parser;
using FastPaymentIdemCache.Storage;

namespace FastPaymentIdemCache.Server;

public class TcpServer
{
    private const int MaximumMessageLength = 4_096; // 4 KB
    private const int MaxConcurrentConnections = 5;

    private static readonly byte[] OkResponse = "OK\r\n"u8.ToArray();
    private static readonly byte[] NilResponse = "(nil)\r\n"u8.ToArray();
    private static readonly byte[] ErrResponse = "-ERR Unknown command\r\n"u8.ToArray();
    private readonly string _host;
    private readonly int _port;
    private readonly SimpleStore _store;

    private readonly SemaphoreSlim _concurrentConnections;

    public TcpServer(string host, int port, SimpleStore store)
    {
        ArgumentException.ThrowIfNullOrEmpty(host);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(port, other: 0);

        _host = host;
        _port = port;
        _store = store;

        _concurrentConnections = new SemaphoreSlim(initialCount: MaxConcurrentConnections);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var serverEndpoint = new IPEndPoint(IPAddress.Parse(_host), _port);
        using var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        serverSocket.Bind(serverEndpoint);
        serverSocket.Listen();

        Console.WriteLine("FastPaymentIdemCache is ready to listen...");

        while (!cancellationToken.IsCancellationRequested)
        {
            var clientSocket = await serverSocket.AcceptAsync(cancellationToken);

            AppTelemetry.OnConnectionAccepted();

            try
            {
                await _concurrentConnections.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                CloseSocket(clientSocket);
            }

            _ = Task.Run(() => ProcessClientAsync(clientSocket, cancellationToken), cancellationToken);
        }

        Console.WriteLine("FastPaymentIdemCache is stopped...");
    }

    private async Task ProcessClientAsync(Socket clientSocket, CancellationToken cancellationToken)
    {
        AppTelemetry.OnClientConnected();

        var arrayPool = ArrayPool<byte>.Shared;

        var array = arrayPool.Rent(minimumLength: MaximumMessageLength + 1);
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

                if (length > MaximumMessageLength)
                {
                    Console.WriteLine("Client has reached limit bytes, dropping connection");

                    AppTelemetry.AddProtocolError(AppTelemetry.ErrorReasonInvalidPayload);

                    break;
                }

                AppTelemetry.AddBytesReceived(length);

                var memory = new ReadOnlyMemory<byte>(array, start: 0, length);
                var result = CommandParser.Parse(memory.Span);

                var (command, key, _) = result.Decode();

                switch (command)
                {
                    case "SET":
                        using (var activity = AppTelemetry.ActivitySource.StartActivity())
                        {
                            activity?.SetTag(AppTelemetry.CommandTypeTagName, "SET");
                            activity?.SetTag(AppTelemetry.CommandLengthTagName, length);

                            var stopwatch = Stopwatch.StartNew();

                            var profile = JsonSerializer.Deserialize<UserProfile>(result.Value);

                            if (profile is null)
                            {
                                await SendResponseAsync(clientSocket, ErrResponse);

                                AppTelemetry.AddProtocolError(AppTelemetry.ErrorReasonInvalidPayload);
                            }
                            else
                            {
                                _store.Set(key, profile);

                                await SendResponseAsync(clientSocket, OkResponse);
                            }

                            stopwatch.Stop();
                            
                            AppTelemetry.AddCommandProcessed("SET");
                            AppTelemetry.AddCommandExecutionTime("SET", stopwatch.Elapsed.TotalSeconds);

                            break;
                        }

                    case "GET":
                        using (var activity = AppTelemetry.ActivitySource.StartActivity())
                        {
                            activity?.SetTag(AppTelemetry.CommandTypeTagName, "GET");
                            activity?.SetTag(AppTelemetry.CommandLengthTagName, length);

                            var stopwatch = Stopwatch.StartNew();

                            var storedValue = _store.Get(key);

                            if (storedValue is null)
                            {
                                await SendResponseAsync(clientSocket, NilResponse);
                            }
                            else
                            {
                                var storedValueBytes = JsonSerializer.SerializeToUtf8Bytes(storedValue);

                                await SendResponseAsync(clientSocket, storedValueBytes);
                            }

                            stopwatch.Stop();

                            AppTelemetry.AddCommandProcessed("GET");
                            AppTelemetry.AddCommandExecutionTime("GET", stopwatch.Elapsed.TotalSeconds);
                        }

                        break;

                    case "DELETE":
                        using (var activity = AppTelemetry.ActivitySource.StartActivity())
                        {
                            activity?.SetTag(AppTelemetry.CommandTypeTagName, "DELETE");
                            activity?.SetTag(AppTelemetry.CommandLengthTagName, length);

                            var stopwatch = Stopwatch.StartNew();

                            _store.Delete(key);

                            await SendResponseAsync(clientSocket, OkResponse);

                            stopwatch.Stop();

                            AppTelemetry.AddCommandProcessed("DELETE");
                            AppTelemetry.AddCommandExecutionTime("DELETE", stopwatch.Elapsed.TotalSeconds);
                        }

                        break;

                    default:
                        using (var activity = AppTelemetry.ActivitySource.StartActivity())
                        {
                            activity?.SetTag(AppTelemetry.CommandTypeTagName, "ERROR");
                            activity?.SetTag(AppTelemetry.CommandLengthTagName, length);
                            
                            var stopwatch = Stopwatch.StartNew();

                            AppTelemetry.AddProtocolError(AppTelemetry.ErrorReasonUnknownCommand);

                            await SendResponseAsync(clientSocket, ErrResponse);

                            stopwatch.Stop();
                            
                            AppTelemetry.AddCommandProcessed("ERROR");
                            AppTelemetry.AddCommandExecutionTime("ERROR", stopwatch.Elapsed.TotalSeconds);
                        }

                        break;
                }

                Console.WriteLine("Received a command: {0} {1}", command, key);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

            AppTelemetry.AddProtocolError(AppTelemetry.ErrorReasonException);

            await SendResponseAsync(clientSocket, Encoding.UTF8.GetBytes($"{e.Message}\r\n"));
        }
        finally
        {
            AppTelemetry.OnClientDisconnected();

            _concurrentConnections.Release();

            arrayPool.Return(array);

            CloseSocket(clientSocket);
        }
    }

    private static async Task SendResponseAsync(Socket socket, byte[] payload)
    {
        await socket.SendAsync(payload);

        AppTelemetry.AddBytesSent(payload.Length);
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