using CacheService.Observability;
using CacheService.Server;
using CacheService.Storage;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CacheService;

public class Program
{
    private static async Task Main(string[] _)
    {
        Sdk.CreateTracerProviderBuilder()
            .ConfigureResource(r => r.AddService(AppTelemetry.ServiceName, AppTelemetry.ServiceVersion))
            .AddSource(AppTelemetry.ServiceName)
            .AddConsoleExporter()
            .Build();

        Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(r => r.AddService(AppTelemetry.ServiceName, AppTelemetry.ServiceVersion))
            .AddMeter(AppTelemetry.ServiceName)
            .AddConsoleExporter()
            .Build();

        Console.WriteLine("Starting CacheService...");

        using var store = new SimpleStore();

        var tcpServer = new TcpServer("127.0.0.1", port: 9000, store);

        await tcpServer.StartAsync();

        Console.WriteLine("Press any key to close...");

        Console.ReadLine();
    }
}