using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CacheService.Observability;

public static class AppTelemetry
{
    public const string ServiceName = "CacheService";
    public const string ServiceVersion = "1.0.0";

    public const string TagCommandType = "app.command.type";
    public const string TagCommandLength = "app.command.length.bytes";

    public static readonly ActivitySource ActivitySource = new(ServiceName);

    private static readonly Meter Meter = new(ServiceName);

    private static readonly Counter<long> CommandsProcessed =
        Meter.CreateCounter<long>("app.commands.processed.total", description: "Total number of commands processed.");

    private static readonly Histogram<double> CommandExecutionTimeSeconds =
        Meter.CreateHistogram<double>("app.command.execution.time.seconds", description: "Command execution time in seconds.");

    public static void AddCommandProcessed(string commandType)
    {
        CommandsProcessed.Add(delta: 1, new KeyValuePair<string, object?>(TagCommandType, commandType));
    }

    public static void AddCommandExecutionTime(string commandType, double seconds)
    {
        CommandExecutionTimeSeconds.Record(seconds, new KeyValuePair<string, object?>(TagCommandType, commandType));
    }
}