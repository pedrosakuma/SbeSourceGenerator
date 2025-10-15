using BenchmarkDotNet.Attributes;
using Benchmark.Messages;

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
    private BidsData[] _smallBids = null!;
    private AsksData[] _smallAsks = null!;
    private BidsData[] _mediumBids = null!;
    private AsksData[] _mediumAsks = null!;
    private BidsData[] _largeBids = null!;
    private AsksData[] _largeAsks = null!;
    
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

    private BidsData[] CreateBids(int count)
    {
        var bids = new BidsData[count];
        for (int i = 0; i < count; i++)
        {
            bids[i] = new BidsData
            {
                Price = 1000000 - (i * 100),
                Quantity = 100 + i
            };
        }
        return bids;
    }

    private AsksData[] CreateAsks(int count)
    {
        var asks = new AsksData[count];
        for (int i = 0; i < count; i++)
        {
            asks[i] = new AsksData
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

        _marketData.BeginEncoding(_buffer, out var writer);
        MarketDataData.TryEncodeBids(ref writer, bids);
        MarketDataData.TryEncodeAsks(ref writer, asks);
        return true;
    }

    [Benchmark(Description = "Decode Message with Groups")]
    public int DecodeWithGroups()
    {
        // First encode
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

        _marketData.BeginEncoding(_buffer, out var writer);
        MarketDataData.TryEncodeBids(ref writer, bids);
        MarketDataData.TryEncodeAsks(ref writer, asks);

        // Then decode and count
        int bidCount = 0;
        int askCount = 0;

        if (MarketDataData.TryParse(_buffer, out var decoded, out var variableData))
        {
            decoded.ConsumeVariableLengthSegments(
                variableData,
                bid => bidCount++,
                ask => askCount++
            );
        }

        return bidCount + askCount;
    }
}
