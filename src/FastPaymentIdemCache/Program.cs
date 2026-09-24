using FastPaymentIdemCache.Observability;
using FastPaymentIdemCache.Server;
using FastPaymentIdemCache.Storage;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FastPaymentIdemCache;

public class Program
{
    private static async Task Main(string[] _)
    {
        using var traceProvider = Sdk.CreateTracerProviderBuilder()
            .ConfigureResource(r => r.AddService(AppTelemetry.ServiceName, AppTelemetry.ServiceVersion))
            .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(probability: 0.01)))
            .AddSource(AppTelemetry.ServiceName)
            .AddOtlpExporter()
            .Build();

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(r => r.AddService(AppTelemetry.ServiceName, AppTelemetry.ServiceVersion))
            .AddMeter(AppTelemetry.ServiceName)
            .AddRuntimeInstrumentation()
            .AddView(
                AppTelemetry.CommandExecutionTimeInstrumentName,
                new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = [0.0005, 0.001, 0.002, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5]
                }
            )
            .AddOtlpExporter()
            .Build();

        Console.WriteLine("Starting FastPaymentIdemCache server...");

        using var store = new SimpleStore();

        AppTelemetry.SetStoreEntryCountProvider(() => store.Count);

        var tcpServer = new TcpServer("127.0.0.1", port: 9000, store);

        await tcpServer.StartAsync();

        Console.WriteLine("Press any key to stop server...");

        Console.ReadLine();
    }
}