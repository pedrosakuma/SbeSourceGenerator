using BenchmarkDotNet.Attributes;
using Benchmark.Messages.V0;

namespace SbeCodeGenerator.Benchmarks;

/// <summary>
/// Benchmarks for messages with repeating groups.
/// Tests the performance of group encoding and decoding with varying group sizes.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class RepeatingGroupBenchmarks
{
    private byte[] _buffer = null!;
    private MarketDataData _marketData;
    private MarketDataData.BidsData[] _smallBids = null!;
    private MarketDataData.AsksData[] _smallAsks = null!;
    private MarketDataData.BidsData[] _mediumBids = null!;
    private MarketDataData.AsksData[] _mediumAsks = null!;
    private MarketDataData.BidsData[] _largeBids = null!;
    private MarketDataData.AsksData[] _largeAsks = null!;
    
    private const int BufferSize = 64 * 1024; // 64KB buffer

    [Params(10, 50, 100)]
    public int GroupSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new byte[BufferSize];
        _marketData = new MarketDataData
        {
            InstrumentId = 42,
            Timestamp = 1234567890UL
        };

        // Setup small groups (10 items)
        _smallBids = CreateBids(10);
        _smallAsks = CreateAsks(10);

        // Setup medium groups (50 items)
        _mediumBids = CreateBids(50);
        _mediumAsks = CreateAsks(50);

        // Setup large groups (100 items)
        _largeBids = CreateBids(100);
        _largeAsks = CreateAsks(100);
    }

    private MarketDataData.BidsData[] CreateBids(int count)
    {
        var bids = new MarketDataData.BidsData[count];
        for (int i = 0; i < count; i++)
        {
            bids[i] = new MarketDataData.BidsData
            {
                Price = 1000000 - (i * 100),
                Quantity = 100 + i
            };
        }
        return bids;
    }

    private MarketDataData.AsksData[] CreateAsks(int count)
    {
        var asks = new MarketDataData.AsksData[count];
        for (int i = 0; i < count; i++)
        {
            asks[i] = new MarketDataData.AsksData
            {
                Price = 1010000 + (i * 100),
                Quantity = 50 + i
            };
        }
        return asks;
    }

    [Benchmark(Description = "Encode Message with Groups")]
    public bool EncodeWithGroups()
    {
        var bids = GroupSize switch
        {
            10 => _smallBids,
            50 => _mediumBids,
            _ => _largeBids
        };

        var asks = GroupSize switch
        {
            10 => _smallAsks,
            50 => _mediumAsks,
            _ => _largeAsks
        };

        return MarketDataData.TryEncode(_marketData, _buffer, bids, asks, out _);
    }

    [Benchmark(Description = "Decode Message with Groups")]
    public int DecodeWithGroups()
    {
        var bids = GroupSize switch
        {
            10 => _smallBids,
            50 => _mediumBids,
            _ => _largeBids
        };

        var asks = GroupSize switch
        {
            10 => _smallAsks,
            50 => _mediumAsks,
            _ => _largeAsks
        };

        MarketDataData.TryEncode(_marketData, _buffer, bids, asks, out _);

        int bidCount = 0;
        int askCount = 0;

        if (MarketDataData.TryParse(_buffer, out var reader))
        {
            reader.ReadGroups(
                (in MarketDataData.BidsData _) => bidCount++,
                (in MarketDataData.AsksData _) => askCount++
            );
        }

        return bidCount + askCount;
    }
}
