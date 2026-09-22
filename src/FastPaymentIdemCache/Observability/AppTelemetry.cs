using System.Diagnostics;
using System.Diagnostics.Metrics;
using FastPaymentIdemCache.Models;

namespace FastPaymentIdemCache.Observability;

public static class AppTelemetry
{
    public const string ServiceName = "FastPaymentIdemCache";
    public const string ServiceVersion = "1.0.0";

    public const string CommandTypeTagName = "app.command.type";
    public const string CommandLengthTagName = "app.command.length.bytes";

    public const string CommandsProcessedInstrumentName = "app.commands.processed.total";
    public const string CommandExecutionTimeInstrumentName = "app.command.execution.time.seconds";

    public const string ServerConnectionsCurrentInstrumentName = "app.server.connections.current";
    public const string ServerConnectionsTotalInstrumentName = "app.server.connections.total";
    public const string ServerBytesReceivedInstrumentName = "app.server.bytes.received.total";
    public const string ServerBytesSentInstrumentName = "app.server.bytes.sent.total";
    public const string ServerProtocolErrorsInstrumentName = "app.server.protocol.errors.total";
    public const string StoreEntriesInstrumentName = "app.store.entries";

    public const string ErrorReasonTagName = "app.server.error.reason";
    public const string ErrorReasonUnknownCommand = "unknown_command";
    public const string ErrorReasonInvalidPayload = "invalid_payload";
    public const string ErrorReasonException = "exception";

    public static readonly ActivitySource ActivitySource = new(ServiceName);

    private static readonly Meter Meter = new(ServiceName);

    private static readonly Counter<long> CommandsProcessed =
        Meter.CreateCounter<long>(CommandsProcessedInstrumentName, description: "Total number of commands processed.");

    private static readonly Histogram<double> CommandExecutionTimeSeconds =
        Meter.CreateHistogram<double>(CommandExecutionTimeInstrumentName, description: "Command execution time in seconds.");

    private static readonly Counter<long> ConnectionsTotal =
        Meter.CreateCounter<long>(ServerConnectionsTotalInstrumentName, description: "Total number of accepted TCP connections.");

    private static readonly Counter<long> BytesReceived =
        Meter.CreateCounter<long>(ServerBytesReceivedInstrumentName, description: "Total number of bytes received from clients.");

    private static readonly Counter<long> BytesSent =
        Meter.CreateCounter<long>(ServerBytesSentInstrumentName, description: "Total number of bytes sent to clients.");

    private static readonly Counter<long> ProtocolErrors =
        Meter.CreateCounter<long>(ServerProtocolErrorsInstrumentName, description: "Total number of protocol and processing errors.");

    private static long _currentConnections;

    private static Func<int>? _storeEntryCountProvider;

    static AppTelemetry()
    {
        Meter.CreateObservableGauge(
            ServerConnectionsCurrentInstrumentName,
            () => new Measurement<int>((int)Interlocked.Read(ref _currentConnections)),
            description: "Number of currently connected clients.");

        Meter.CreateObservableGauge(
            StoreEntriesInstrumentName,
            () => new Measurement<int>(_storeEntryCountProvider?.Invoke() ?? 0),
            description: "Number of entries in the store.");
    }

    public static void AddCommandProcessed(CommandType commandType)
    {
        CommandsProcessed.Add(delta: 1, new KeyValuePair<string, object?>(CommandTypeTagName, commandType));
    }

    public static void AddCommandExecutionTime(CommandType commandType, double seconds)
    {
        CommandExecutionTimeSeconds.Record(seconds, new KeyValuePair<string, object?>(CommandTypeTagName, commandType));
    }

    public static void OnConnectionAccepted()
    {
        ConnectionsTotal.Add(1);
    }

    public static void OnClientConnected()
    {
        Interlocked.Increment(ref _currentConnections);
    }

    public static void OnClientDisconnected()
    {
        Interlocked.Decrement(ref _currentConnections);
    }

    public static void AddBytesReceived(long bytes)
    {
        BytesReceived.Add(bytes);
    }

    public static void AddBytesSent(long bytes)
    {
        BytesSent.Add(bytes);
    }

    public static void AddProtocolError(string reason)
    {
        ProtocolErrors.Add(1, new KeyValuePair<string, object?>(ErrorReasonTagName, reason));
    }

    public static void SetStoreEntryCountProvider(Func<int> provider)
    {
        _storeEntryCountProvider = provider;
    }
}