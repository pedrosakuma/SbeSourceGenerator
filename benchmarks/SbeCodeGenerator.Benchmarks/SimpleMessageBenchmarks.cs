using BenchmarkDotNet.Attributes;
using Benchmark.Messages.V0;

namespace SbeCodeGenerator.Benchmarks;

/// <summary>
/// Benchmarks for simple message encoding and decoding.
/// Tests the baseline performance of the SBE code generator.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class SimpleMessageBenchmarks
{
    private byte[] _buffer = null!;
    private SimpleOrderData _order;
    private const int BufferSize = 1024;

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new byte[BufferSize];
        _order = new SimpleOrderData
        {
            OrderId = 123456789UL,
            Price = 995000, // 99.5000
            Quantity = 100,
            Side = Side.Buy
        };
    }

    [Benchmark(Description = "Encode Simple Message")]
    public bool EncodeSimpleMessage()
    {
        return _order.TryEncode(_buffer, out _);
    }

    [Benchmark(Description = "Decode Simple Message")]
    public bool DecodeSimpleMessage()
    {
        // First encode
        _order.TryEncode(_buffer, out _);
        
        // Then decode
        return SimpleOrderData.TryParse(_buffer, out _);
    }

    [Benchmark(Description = "Round-trip Simple Message")]
    public bool RoundTripSimpleMessage()
    {
        // Encode
        if (!_order.TryEncode(_buffer, out _))
            return false;
        
        // Decode
        if (!SimpleOrderData.TryParse(_buffer, out var reader))
            return false;
        
        // Verify
        ref readonly var decoded = ref reader.Data;
        return decoded.OrderId == _order.OrderId &&
               decoded.Price.Value == _order.Price.Value &&
               decoded.Quantity.Value == _order.Quantity.Value &&
               decoded.Side == _order.Side;
    }
}
