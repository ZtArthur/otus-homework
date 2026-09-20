using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;

namespace FastPaymentIdemCache.LoadTests;

internal static class Program
{
    private const string Host = "127.0.0.1";
    private const int Port = 9000;
    private const int Keyspace = 100_000;

    private static readonly string[] ScenarioNames = ["stable-set", "stable-get", "max-set", "max-get"];

    // Сервер одновременно обслуживает не более 10 соединений (MaxConcurrentConnections)
    private static readonly SemaphoreSlim ConnectionGate = new(initialCount: 10, maxCount: 10);
    private static readonly ConcurrentBag<FastPaymentIdemCacheClient> Connections = new();

    private static readonly byte[] Payload = JsonSerializer.SerializeToUtf8Bytes(
        new UserPaymentOperation
        {
            TransactionId = Guid.NewGuid(),
            TransactionDate = DateTime.UtcNow,
            UserId = 42_000
        }
    );

    private static void Main(string[] args)
    {
        // Профиль-команда: stable-set | stable-get | max-set | max-get.
        // Stable — постоянная подача 500 RPS в течение 3 минут;
        // Max — предельная подача 10000 RPS в течение 1 минуты: сколько ok-RPS сервер реально
        //        переварил, то и максимальная возможность. Возникшие отказы означают, что
        //        потолок достигнут — NBomber останавливает тест сам, продолжать смысла нет.
        //        Если подача выбирается полностью без отказов — константу поднять и перезапустить.
        // Set/Get — каждый прогон шлёт только одну команду: чистые распределения latency и RPS
        //        по типам команд отдельно.
        
        var name = args.Length > 0 ? args[0] : "stable-set";

        if (!ScenarioNames.Contains(name))
        {
            Console.Error.WriteLine($"Неизвестный сценарий '{name}', доступны: {string.Join(", ", ScenarioNames)}");

            return;
        }

        var isMax = name.StartsWith("max", StringComparison.Ordinal);

        var rate = isMax ? 10_000 : 500;
        var during = isMax ? TimeSpan.FromMinutes(minutes: 1) : TimeSpan.FromMinutes(minutes: 3);

        Console.WriteLine(
            $"Сценарий: {name} ({Host}:{Port}, keyspace {Keyspace}, 10 keep-alive соединений, "
            + $"подача {rate} RPS в течение {during.TotalMinutes:0} мин)"
        );

        var run = name.EndsWith("set", StringComparison.Ordinal)
            ? new Func<IScenarioContext, Task<IResponse>>(ExecuteSet)
            : ExecuteGet;

        var scenario = Scenario.Create(name, run)
            .WithWarmUpDuration(TimeSpan.FromSeconds(seconds: 10))
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: rate,
                    interval: TimeSpan.FromSeconds(seconds: 1),
                    during: during
                )
            );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithTestSuite("FastPaymentIdemCache")
            .WithTestName(name)
            .WithReportFolder("docs/results/nbomber")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv, ReportFormat.Md)
            .Run();
    }

    private static Task<IResponse> ExecuteSet(IScenarioContext context)
    {
        var key = $"key_{Random.Shared.Next(minValue: 1, Keyspace + 1)}";

        return ExecuteCommand(client => client.SetAsync(key, Payload), IsSetResponse);
    }

    private static Task<IResponse> ExecuteGet(IScenarioContext context)
    {
        var key = $"key_{Random.Shared.Next(minValue: 1, Keyspace + 1)}";

        return ExecuteCommand(client => client.GetAsync(key), IsGetResponse);
    }

    private static async Task<IResponse> ExecuteCommand(
        Func<FastPaymentIdemCacheClient, Task<byte[]>> send,
        Func<byte[], bool> isValidResponse
    )
    {
        if (!await ConnectionGate.WaitAsync(TimeSpan.FromSeconds(seconds: 10)))
        {
            return Response.Fail();
        }

        var client = Connections.TryTake(out var free) ? free : new FastPaymentIdemCacheClient(Host, Port);

        try
        {
            if (!client.IsConnected)
            {
                await client.ConnectAsync();
            }

            if (isValidResponse(await send(client)))
            {
                Connections.Add(client);

                return Response.Ok();
            }

            // Ответ не по протоколу — потоку больше не доверяем
            client.Dispose();

            return Response.Fail();
        }
        catch
        {
            client.Dispose();

            return Response.Fail();
        }
        finally
        {
            ConnectionGate.Release();
        }
    }

    private static bool IsSetResponse(byte[] response) => Encoding.UTF8.GetString(response).StartsWith("OK", StringComparison.Ordinal);

    private static bool IsGetResponse(byte[] response)
    {
        var value = Encoding.UTF8.GetString(response);

        // GET-попадание приходит JSON-ом, промах — ответом "(nil)"
        return value.StartsWith(value: '{') || value.StartsWith("(nil)", StringComparison.Ordinal);
    }

    public class UserPaymentOperation
    {
        public Guid TransactionId { get; set; }

        public DateTime TransactionDate { get; set; }

        public long UserId { get; set; }
    }
}
