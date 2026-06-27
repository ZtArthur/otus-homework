using CacheService.Server;
using CacheService.Storage;

namespace CacheService;

internal class Program
{
    private static async Task Main(string[] args)
    {
        using var store = new SimpleStore();
        var tcpServer = new TcpServer("127.0.0.1", port: 8080, store);

        await tcpServer.StartAsync();

        Console.ReadLine();
    }
}