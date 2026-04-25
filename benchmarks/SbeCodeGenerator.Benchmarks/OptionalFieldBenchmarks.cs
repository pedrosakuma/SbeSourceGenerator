using BenchmarkDotNet.Attributes;
using Benchmark.Messages.V0;

namespace SbeCodeGenerator.Benchmarks;

/// <summary>
/// Benchmarks for messages with optional fields.
/// Tests the performance impact of optional field handling.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class OptionalFieldBenchmarks
{
    private byte[] _buffer = null!;
    private OrderWithOptionalsData _orderWithOptionals;
    private OrderWithOptionalsData _orderWithoutOptionals;
    private const int BufferSize = 1024;

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new byte[BufferSize];
        
        _orderWithOptionals = new OrderWithOptionalsData
        {
            OrderId = 123456789UL,
            Price = 995000,
            Quantity = 100,
            Side = Side.Buy,
        };
        _orderWithOptionals.SetStopPrice(1000000);
        _orderWithOptionals.SetClientOrderId(987654321UL);

        _orderWithoutOptionals = new OrderWithOptionalsData
        {
            OrderId = 123456789UL,
            Price = 995000,
            Quantity = 100,
            Side = Side.Buy
            // No optional fields set
        };
    }

    [Benchmark(Description = "Encode Message with Optionals Set")]
    public bool EncodeWithOptionals()
    {
        return _orderWithOptionals.TryEncode(_buffer, out _);
    }

    [Benchmark(Description = "Encode Message with Optionals Null")]
    public bool EncodeWithoutOptionals()
    {
        return _orderWithoutOptionals.TryEncode(_buffer, out _);
    }

    [Benchmark(Description = "Decode Message with Optionals Set")]
    public bool DecodeWithOptionals()
    {
        _orderWithOptionals.TryEncode(_buffer, out _);
        return OrderWithOptionalsData.TryParse(_buffer, out _);
    }

    [Benchmark(Description = "Decode Message with Optionals Null")]
    public bool DecodeWithoutOptionals()
    {
        _orderWithoutOptionals.TryEncode(_buffer, out _);
        return OrderWithOptionalsData.TryParse(_buffer, out _);
    }
}
