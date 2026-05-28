namespace CacheService;

class Program
{
    static async Task Main(string[] args)
    {
        var tcpServer = new TcpServer("127.0.0.1", port: 8080);

        await tcpServer.StartAsync();

        Console.ReadLine();
    }
}