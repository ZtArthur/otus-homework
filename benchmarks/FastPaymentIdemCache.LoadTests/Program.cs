using System.Text;
using System.Text.Json;
using NBomber.CSharp;

namespace FastPaymentIdemCache.LoadTests;

internal class Program
{
    private static async Task Main(string[] args)
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
                    var createdAt = DateTime.Now;
                    var profile = new UserProfile
                    {
                        Id = id,
                        Username = "Marko Polo",
                        CreatedAt = createdAt
                    };

                    var profileBytes = JsonSerializer.SerializeToUtf8Bytes(profile);

                    Console.WriteLine("Sending...");

                    var responseBytes = await client.SetAsync(key, profileBytes);
                    var responseString = Encoding.UTF8.GetString(responseBytes, index: 0, responseBytes.Length);

                    Console.WriteLine("Response: {0}", responseString);

                    return responseString.StartsWith("OK", StringComparison.OrdinalIgnoreCase)
                        ? Response.Ok()
                        : Response.Fail();
                }
            )
            .WithWarmUpDuration(TimeSpan.FromSeconds(seconds: 7))
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(seconds: 1),
                    during: TimeSpan.FromSeconds(seconds: 30)
                )
            );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }

    public class UserProfile
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}