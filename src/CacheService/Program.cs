using CacheService.Server;
using CacheService.Storage;
using Generators.BinarySerializer;

namespace CacheService;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var message = HelloGenerator.GetMessage();
        
        Console.WriteLine(message);
        
        // Console.WriteLine("Starting CacheService...");
        //
        // using var store = new SimpleStore();
        // var tcpServer = new TcpServer("127.0.0.1", port: 9000, store);
        //
        // await tcpServer.StartAsync();
        //
        // Console.WriteLine("Press any key to close...");

        Console.ReadLine();
    }
}