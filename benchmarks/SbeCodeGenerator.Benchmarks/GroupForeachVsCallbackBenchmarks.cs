using BenchmarkDotNet.Attributes;
using Benchmark.Messages.V0;

namespace SbeCodeGenerator.Benchmarks;

/// <summary>
/// Issue #156 follow-up: compare the foreach-style group enumerator (v1.5.0+) against
/// the existing <c>ReadGroups</c> callback API for decoding messages with simple top-level groups.
///
/// Workload (held identical across variants): iterate every entry of every group and accumulate
/// <c>Price + Quantity</c> into a checksum, returning the result so the JIT can't elide the work.
///
/// Variants:
///   - Callback           : current API; lambdas capture a local <c>sum</c> -> closure allocation per call.
///   - Foreach            : v1.5.0 zero-alloc enumerator; pure stack state via ref struct.
///   - Foreach_EarlyBreak : foreach + break after first entry per group; demonstrates skipping cost.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class GroupForeachVsCallbackBenchmarks
{
    private byte[] _encoded = null!;

    [Params(10, 50, 100)]
    public int GroupSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var bids = new MarketDataData.BidsData[GroupSize];
        var asks = new MarketDataData.AsksData[GroupSize];
        for (int i = 0; i < GroupSize; i++)
        {
            bids[i] = new MarketDataData.BidsData { Price = 1_000_000 - i * 100, Quantity = 100 + i };
            asks[i] = new MarketDataData.AsksData { Price = 1_010_000 + i * 100, Quantity = 50 + i };
        }

        var buffer = new byte[64 * 1024];
        var header = new MarketDataData { InstrumentId = 42, Timestamp = 1234567890UL };
        MarketDataData.TryEncode(header, buffer, bids, asks, out int written);
        _encoded = new byte[written];
        Array.Copy(buffer, _encoded, written);
    }

    [Benchmark(Baseline = true, Description = "Decode via ReadGroups (callback)")]
    public long Decode_Callback()
    {
        long sum = 0;
        if (MarketDataData.TryParse(_encoded, out var reader))
        {
            reader.ReadGroups(
                (in MarketDataData.BidsData b) => sum += b.Price.Value + b.Quantity.Value,
                (in MarketDataData.AsksData a) => sum += a.Price.Value + a.Quantity.Value
            );
        }
        return sum;
    }

    [Benchmark(Description = "Decode via foreach enumerator")]
    public long Decode_Foreach()
    {
        long sum = 0;
        if (MarketDataData.TryParse(_encoded, out var reader))
        {
            foreach (ref readonly var b in reader.Bids)
                sum += b.Price.Value + b.Quantity.Value;
            foreach (ref readonly var a in reader.Asks)
                sum += a.Price.Value + a.Quantity.Value;
        }
        return sum;
    }

    [Benchmark(Description = "Decode via foreach + early break (first entry only)")]
    public long Decode_Foreach_EarlyBreak()
    {
        long sum = 0;
        if (MarketDataData.TryParse(_encoded, out var reader))
        {
            foreach (ref readonly var b in reader.Bids)
            {
                sum += b.Price.Value + b.Quantity.Value;
                break;
            }
            foreach (ref readonly var a in reader.Asks)
            {
                sum += a.Price.Value + a.Quantity.Value;
                break;
            }
        }
        return sum;
    }
}
