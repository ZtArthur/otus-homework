using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FastPaymentIdemCache.Benchmarks;
using FastPaymentIdemCache.Models;

BenchmarkRunner.Run<SerializationBenchmarks>();

namespace FastPaymentIdemCache.Benchmarks
{
    [MemoryDiagnoser]
    public class SerializationBenchmarks
    {
        private UserProfile _profile = null!;

        [GlobalSetup]
        public void Setup()
        {
            _profile = new UserProfile
            {
                Id = 1031,
                CreatedAt = DateTime.Now,
                Username = "user-benchmark"
            };
        }

        [Benchmark(Baseline = true)]
        public byte[] SystemTextJson()
        {
            using var ms = new MemoryStream();
            JsonSerializer.Serialize(ms, _profile);

            return ms.ToArray();
        }

        [Benchmark]
        public byte[] GenerateBinary()
        {
            using var ms = new MemoryStream();
            _profile.SerializeToBinary(ms);

            return ms.ToArray();
        }
    }
}