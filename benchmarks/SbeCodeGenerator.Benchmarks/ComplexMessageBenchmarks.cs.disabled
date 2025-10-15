using BenchmarkDotNet.Attributes;
using Benchmark.Messages;

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
    private BidsData_[] _bids = null!;
    private AsksData_[] _asks = null!;
    private TradesData[] _trades = null!;
    
    private const int BufferSize = 128 * 1024; // 128KB buffer
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

        // Setup bids
        _bids = new BidsData_[BidCount];
        for (int i = 0; i < BidCount; i++)
        {
            _bids[i] = new BidsData_
            {
                Price = 1000000 - (i * 100),
                Quantity = 100 + i,
                NumberOfOrders = (uint)(5 + (i % 10))
            };
        }

        // Setup asks
        _asks = new AsksData_[AskCount];
        for (int i = 0; i < AskCount; i++)
        {
            _asks[i] = new AsksData_
            {
                Price = 1010000 + (i * 100),
                Quantity = 50 + i,
                NumberOfOrders = (uint)(3 + (i % 8))
            };
        }

        // Setup trades
        _trades = new TradesData[TradeCount];
        for (int i = 0; i < TradeCount; i++)
        {
            _trades[i] = new TradesData
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
        _complexData.BeginEncoding(_buffer, out var writer);
        ComplexMarketDataData.TryEncodeBids(ref writer, _bids);
        ComplexMarketDataData.TryEncodeAsks(ref writer, _asks);
        ComplexMarketDataData.TryEncodeTrades(ref writer, _trades);
        return true;
    }

    [Benchmark(Description = "Decode Complex Message")]
    public int DecodeComplexMessage()
    {
        // First encode
        _complexData.BeginEncoding(_buffer, out var writer);
        ComplexMarketDataData.TryEncodeBids(ref writer, _bids);
        ComplexMarketDataData.TryEncodeAsks(ref writer, _asks);
        ComplexMarketDataData.TryEncodeTrades(ref writer, _trades);

        // Then decode and count
        int bidCount = 0;
        int askCount = 0;
        int tradeCount = 0;

        if (ComplexMarketDataData.TryParse(_buffer, out var decoded, out var variableData))
        {
            decoded.ConsumeVariableLengthSegments(
                variableData,
                bid => bidCount++,
                ask => askCount++,
                trade => tradeCount++
            );
        }

        return bidCount + askCount + tradeCount;
    }

    [Benchmark(Description = "Round-trip Complex Message")]
    public bool RoundTripComplexMessage()
    {
        // Encode
        _complexData.BeginEncoding(_buffer, out var writer);
        ComplexMarketDataData.TryEncodeBids(ref writer, _bids);
        ComplexMarketDataData.TryEncodeAsks(ref writer, _asks);
        ComplexMarketDataData.TryEncodeTrades(ref writer, _trades);

        // Decode
        if (!ComplexMarketDataData.TryParse(_buffer, out var decoded, out var variableData))
            return false;

        // Verify basic fields
        if (decoded.InstrumentId != _complexData.InstrumentId ||
            decoded.Timestamp != _complexData.Timestamp ||
            decoded.Flags != _complexData.Flags)
            return false;

        // Count groups
        int bidCount = 0;
        int askCount = 0;
        int tradeCount = 0;

        decoded.ConsumeVariableLengthSegments(
            variableData,
            bid => bidCount++,
            ask => askCount++,
            trade => tradeCount++
        );

        return bidCount == BidCount && askCount == AskCount && tradeCount == TradeCount;
    }
}
