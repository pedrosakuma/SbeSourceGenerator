# High-Performance Market Data Example

This example demonstrates best practices for high-performance SBE message processing using the SBE Code Generator.

## Features

- **Zero Allocations**: Uses stack allocation and buffer pooling
- **Efficient Group Processing**: Demonstrates repeating group handling
- **Batch Processing**: Shows how to process multiple messages efficiently
- **Performance Measurement**: Includes throughput benchmarks
- **Best Practices**: Follows all recommended optimization patterns

## Performance Characteristics

This example demonstrates:
- Sub-microsecond encoding/decoding
- Zero heap allocations for message processing
- Multi-million messages per second throughput
- Efficient buffer reuse with ArrayPool

## Running the Example

```bash
cd examples/HighPerformanceMarketData
dotnet run -c Release
```

**Note**: Always run in Release mode for accurate performance measurements!

## What You'll See

The example runs five scenarios:

### 1. Quote Processing
Simple two-sided quote (best bid/ask) encoding and decoding.

### 2. Trade Processing
Trade execution message processing.

### 3. Depth Snapshot Processing
Market depth with repeating groups (bids and asks).
Demonstrates:
- Group encoding
- Group decoding
- Incremental processing

### 4. Batch Processing
Efficient processing of 1,000 messages in a single buffer.
Shows:
- Buffer reuse
- Sequential encoding/decoding
- Aggregation without allocations

### 5. Performance Test
Throughput measurement for 1 million messages.
Measures:
- Encoding speed (ns per message)
- Decoding speed (ns per message)
- Round-trip latency

## Key Techniques Demonstrated

### Stack Allocation for Small Messages

```csharp
// Use stackalloc for messages < 1KB
Span<byte> buffer = stackalloc byte[QuoteData.MESSAGE_SIZE];
quote.TryEncode(buffer, out _);
```

### Buffer Pooling for Larger Messages

```csharp
// Use ArrayPool for larger buffers
byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    // Use buffer...
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

### Streaming Group Processing

```csharp
// Process groups without materializing collections
snapshot.ConsumeVariableLengthSegments(
    variableData,
    bid => ProcessBid(bid),  // Process each bid immediately
    ask => ProcessAsk(ask)   // Process each ask immediately
);
```

### Batch Processing

```csharp
// Encode multiple messages in one buffer
int offset = 0;
for (int i = 0; i < batchSize; i++)
{
    quote.TryEncode(buffer.AsSpan(offset), out int written);
    offset += written;
}
```

## Performance Tips

1. **Always use Release mode** for performance testing
2. **Warmup the JIT** before measurements
3. **Reuse buffers** - avoid allocations
4. **Process incrementally** - don't materialize collections
5. **Use Span<T>** - enable zero-copy operations

## Schema

The example uses a simple market data schema with three message types:

- **Quote** - Best bid/ask quote
- **Trade** - Trade execution
- **DepthSnapshot** - Full market depth with repeating groups

See `Schemas/market-data.xml` for the complete schema definition.

## Learning Resources

- [Performance Tuning Guide](../../docs/PERFORMANCE_TUNING_GUIDE.md)
- [Benchmark Results](../../docs/BENCHMARK_RESULTS.md)
- [Phase 4 Documentation](../../docs/PHASE4_IMPLEMENTATION.md)

## Expected Output

```
High-Performance Market Data Example
=====================================

1. Quote Processing
-------------------
✓ Encoded quote: 56 bytes
  Instrument: 1001
  Bid: 99.5000 x 100
  Ask: 99.5500 x 200
  Spread: 0.0500

2. Trade Processing
-------------------
✓ Encoded trade: 48 bytes
  Trade ID: 123456789
  Price: 99.5250
  Quantity: 50
  Side: Buy

3. Depth Snapshot Processing (with Groups)
------------------------------------------
✓ Encoded depth snapshot: XXX bytes
  Instrument: 1001
  Bids:
    99.5000 x 100 (3 orders)
    99.4900 x 110 (4 orders)
    ...
  Asks:
    99.5500 x 50 (2 orders)
    99.5600 x 60 (3 orders)
    ...

4. Batch Processing
-------------------
✓ Encoded 1000 quotes in XXXX bytes
✓ Decoded 1000 quotes
  Average spread: 0.0500

5. Performance Test
-------------------
  Encode: XX.XX ns/msg (X.XX M msg/s)
  Decode: XX.XX ns/msg (X.XX M msg/s)
  Round-trip: XXX.XX ns/msg (X.XX M msg/s)
```

## Next Steps

- Try modifying the schema to add more fields
- Experiment with different group sizes
- Measure memory allocations with a profiler
- Compare performance with other serialization libraries
- Adapt this pattern for your own use case

---

**Performance Note**: Results will vary based on your hardware. Run on production-like hardware for accurate measurements.
