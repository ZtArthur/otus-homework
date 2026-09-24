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
        private UserPaymentOperation _operation = null!;

        [GlobalSetup]
        public void Setup()
        {
            _operation = new UserPaymentOperation
            {
                TransactionId = Guid.NewGuid(),
                TransactionDate = DateTime.Now,
                UserId = 1031
            };
        }

        [Benchmark(Baseline = true)]
        public byte[] SystemTextJson()
        {
            using var ms = new MemoryStream();
            JsonSerializer.Serialize(ms, _operation);

            return ms.ToArray();
        }

        [Benchmark]
        public byte[] GenerateBinary()
        {
            using var ms = new MemoryStream();
            _operation.SerializeToBinary(ms);

            return ms.ToArray();
        }
    }
}