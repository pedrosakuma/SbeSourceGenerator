using BenchmarkDotNet.Attributes;
using Benchmark.Messages.V0;

namespace SbeCodeGenerator.Benchmarks;

/// <summary>
/// Benchmarks for complex messages with multiple groups.
/// Tests the performance of encoding and decoding complex market data structures.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ComplexMessageBenchmarks
{
    private byte[] _buffer = null!;
    private ComplexMarketDataData _complexData;
    private ComplexMarketDataData.BidsData[] _bids = null!;
    private ComplexMarketDataData.AsksData[] _asks = null!;
    private ComplexMarketDataData.TradesData[] _trades = null!;

    private const int BufferSize = 128 * 1024;
    private const int BidCount = 50;
    private const int AskCount = 50;
    private const int TradeCount = 20;

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new byte[BufferSize];
        _complexData = new ComplexMarketDataData
        {
            InstrumentId = 42,
            Timestamp = 1234567890UL,
            Flags = 0xFF
        };

        _bids = new ComplexMarketDataData.BidsData[BidCount];
        for (int i = 0; i < BidCount; i++)
        {
            _bids[i] = new ComplexMarketDataData.BidsData
            {
                Price = 1000000 - (i * 100),
                Quantity = 100 + i,
                NumberOfOrders = (uint)(5 + (i % 10))
            };
        }

        _asks = new ComplexMarketDataData.AsksData[AskCount];
        for (int i = 0; i < AskCount; i++)
        {
            _asks[i] = new ComplexMarketDataData.AsksData
            {
                Price = 1010000 + (i * 100),
                Quantity = 50 + i,
                NumberOfOrders = (uint)(3 + (i % 8))
            };
        }

        _trades = new ComplexMarketDataData.TradesData[TradeCount];
        for (int i = 0; i < TradeCount; i++)
        {
            _trades[i] = new ComplexMarketDataData.TradesData
            {
                TradeId = (ulong)(1000000 + i),
                Price = 1005000 + (i * 50),
                Quantity = 10 + (i * 5),
                Side = i % 2 == 0 ? Side.Buy : Side.Sell
            };
        }
    }

    [Benchmark(Description = "Encode Complex Message")]
    public bool EncodeComplexMessage()
    {
        return ComplexMarketDataData.TryEncode(_complexData, _buffer, _bids, _asks, _trades, out _);
    }

    [Benchmark(Description = "Decode Complex Message")]
    public int DecodeComplexMessage()
    {
        ComplexMarketDataData.TryEncode(_complexData, _buffer, _bids, _asks, _trades, out _);

        int bidCount = 0;
        int askCount = 0;
        int tradeCount = 0;

        if (ComplexMarketDataData.TryParse(_buffer, out var reader))
        {
            reader.ReadGroups(
                (in ComplexMarketDataData.BidsData _) => bidCount++,
                (in ComplexMarketDataData.AsksData _) => askCount++,
                (in ComplexMarketDataData.TradesData _) => tradeCount++
            );
        }

        return bidCount + askCount + tradeCount;
    }

    [Benchmark(Description = "Round-trip Complex Message")]
    public bool RoundTripComplexMessage()
    {
        if (!ComplexMarketDataData.TryEncode(_complexData, _buffer, _bids, _asks, _trades, out _))
            return false;

        if (!ComplexMarketDataData.TryParse(_buffer, out var reader))
            return false;

        ref readonly var decoded = ref reader.Data;
        if (decoded.InstrumentId != _complexData.InstrumentId ||
            decoded.Timestamp != _complexData.Timestamp ||
            decoded.Flags != _complexData.Flags)
            return false;

        int bidCount = 0;
        int askCount = 0;
        int tradeCount = 0;

        reader.ReadGroups(
            (in ComplexMarketDataData.BidsData _) => bidCount++,
            (in ComplexMarketDataData.AsksData _) => askCount++,
            (in ComplexMarketDataData.TradesData _) => tradeCount++
        );

        return bidCount == BidCount && askCount == AskCount && tradeCount == TradeCount;
    }
}
