using System.Text;
using System.Text.Json;
using NBomber.CSharp;

namespace FastPaymentIdemCache.LoadTests;

internal class Program
{
    private static Task Main()
    {
        Console.WriteLine("Starting...");

        var random = new Random();

        var scenario = Scenario.Create(
                name: "cache_service_load_scenario",
                run: async _ =>
                {
                    using var client = new FastPaymentIdemCacheClient("127.0.0.1", port: 9000);
                    await client.ConnectAsync();

                    var id = random.Next(minValue: 1, maxValue: 100_000);
                    var key = $"key_{id}";
                    var operation = new UserPaymentOperation
                    {
                        TransactionId = Guid.NewGuid(),
                        TransactionDate = DateTime.Now,
                        UserId = id
                    };

                    var operationBytes = JsonSerializer.SerializeToUtf8Bytes(operation);

                    Console.WriteLine("Sending...");

                    var responseBytes = await client.SetAsync(key, operationBytes);
                    var responseString = Encoding.UTF8.GetString(responseBytes, index: 0, responseBytes.Length);

                    Console.WriteLine("Response: {0}", responseString);

                    return responseString.StartsWith("OK", StringComparison.OrdinalIgnoreCase)
                        ? Response.Ok()
                        : Response.Fail();
                }
            )
            .WithWarmUpDuration(TimeSpan.FromSeconds(seconds: 2))
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(seconds: 1),
                    during: TimeSpan.FromSeconds(seconds: 5)
                )
            );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        return Task.CompletedTask;
    }

    public class UserPaymentOperation
    {
        public Guid TransactionId { get; set; }

        public DateTime TransactionDate { get; set; }

        public long UserId { get; set; }
    }
}