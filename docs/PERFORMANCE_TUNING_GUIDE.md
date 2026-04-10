# Performance Tuning Guide

This guide provides best practices and techniques for optimizing the performance of applications using the SBE Code Generator.

## Table of Contents

1. [Performance Principles](#performance-principles)
2. [Memory Management](#memory-management)
3. [Encoding Optimization](#encoding-optimization)
4. [Decoding Optimization](#decoding-optimization)
5. [Group Processing](#group-processing)
6. [Profiling and Measurement](#profiling-and-measurement)
7. [Common Pitfalls](#common-pitfalls)

## Performance Principles

### Zero-Copy Operations

The generated SBE code is designed for zero-copy operations:

```csharp
// ✅ Good: Zero-copy parsing
ReadOnlySpan<byte> buffer = GetNetworkData();
if (TradeData.TryParse(buffer, out var trade, out _))
{
    ProcessTrade(trade); // No copying
}

// ❌ Bad: Unnecessary copying
byte[] bufferCopy = buffer.ToArray(); // Heap allocation!
```

### Struct-Based Value Types

All generated messages are structs for optimal performance:

```csharp
// ✅ Messages are value types - no heap allocations
var trade = new TradeData 
{ 
    TradeId = 123, 
    Price = 9950 
};

// Stack allocated - very fast
Span<byte> buffer = stackalloc byte[TradeData.MESSAGE_SIZE];
trade.TryEncode(buffer, out _);
```

### Span<T> and Memory<T>

Always prefer `Span<T>` and `ReadOnlySpan<T>`:

```csharp
// ✅ Good: Stack allocation for small buffers
Span<byte> buffer = stackalloc byte[1024];

// ✅ Good: ArrayPool for larger buffers
byte[] pooledBuffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
try
{
    Span<byte> span = pooledBuffer.AsSpan();
    // Use span...
}
finally
{
    ArrayPool<byte>.Shared.Return(pooledBuffer);
}

// ❌ Bad: Direct array allocation for repeated operations
byte[] buffer = new byte[1024]; // Heap allocation every time
```

## Memory Management

### Buffer Reuse

Reuse buffers for repeated encoding/decoding:

```csharp
// ✅ Good: Reuse buffer
byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    for (int i = 0; i < 1000; i++)
    {
        var trade = CreateTrade(i);
        trade.TryEncode(buffer, out int written);
        SendData(buffer.AsSpan(0, written));
    }
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}

// ❌ Bad: Allocate per message
for (int i = 0; i < 1000; i++)
{
    byte[] buffer = new byte[TradeData.MESSAGE_SIZE]; // 1000 allocations!
    var trade = CreateTrade(i);
    trade.TryEncode(buffer, out _);
    SendData(buffer);
}
```

### Stack Allocation Guidelines

Use stack allocation for small, fixed-size buffers:

```csharp
// ✅ Good: Stack allocation for simple messages
Span<byte> buffer = stackalloc byte[512]; // < 1KB is safe

// ⚠️ Caution: Large stack allocations
Span<byte> largeBuffer = stackalloc byte[64 * 1024]; // May cause stack overflow

// ✅ Better: Use heap/pool for large buffers
byte[] largeBuffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
```

**Rule of Thumb**: Stack allocate up to 1KB, use ArrayPool for larger buffers.

## Encoding Optimization

### Batch Encoding

Encode multiple messages in a single buffer:

```csharp
public void EncodeBatch(ReadOnlySpan<TradeData> trades, Span<byte> buffer, out int totalWritten)
{
    var writer = new SpanWriter(buffer);
    
    foreach (ref readonly var trade in trades)
    {
        trade.TryEncode(writer.RemainingSpan, out int written);
        writer.Advance(written);
    }
    
    totalWritten = writer.Position;
}
```

### Pre-compute Message Sizes

Calculate sizes beforehand to avoid multiple encoding passes:

```csharp
// ✅ Good: Pre-compute total size
int totalSize = CalculateTotalSize(orders);
Span<byte> buffer = stackalloc byte[totalSize];

// ❌ Bad: Encode to measure, then encode again
byte[] tempBuffer = new byte[4096];
order.TryEncode(tempBuffer, out int size);
byte[] finalBuffer = new byte[size]; // Second allocation
order.TryEncode(finalBuffer, out _); // Second encoding
```

### Avoid Unnecessary Field Access

```csharp
// ✅ Good: Access field once
long price = trade.Price;
if (price > minPrice && price < maxPrice)
{
    ProcessPrice(price);
}

// ❌ Bad: Multiple field accesses
if (trade.Price > minPrice && trade.Price < maxPrice)
{
    ProcessPrice(trade.Price); // Three accesses to same field
}
```

## Decoding Optimization

### Use TryParse for Validation

The `TryParse` pattern is optimized for performance:

```csharp
// ✅ Good: TryParse with minimal validation
if (TradeData.TryParse(buffer, out var trade, out var variableData))
{
    ProcessTrade(trade);
}

// ⚠️ Slower: Additional validation
if (TradeData.TryParse(buffer, out var trade, out _))
{
    trade.Validate(); // Extra overhead
    ProcessTrade(trade);
}
```

### Minimize Copies

```csharp
// ✅ Good: Work directly with parsed data
if (TradeData.TryParse(buffer, out var trade, out _))
{
    ProcessTradeInPlace(ref trade); // Pass by reference
}

// ❌ Bad: Create copy
if (TradeData.TryParse(buffer, out var trade, out _))
{
    var tradeCopy = trade; // Unnecessary copy
    ProcessTrade(tradeCopy);
}
```

### Process Incrementally

For large messages, process groups incrementally:

```csharp
// ✅ Good: Process groups as they're consumed (v0.9.0 uses in delegates)
MarketDataData.TryParse(buffer, out var data, out var variableData);
data.ConsumeVariableLengthSegments(
    variableData,
    (in BidData bid) => ProcessBid(in bid),    // Zero-copy via in
    (in AskData ask) => ProcessAsk(in ask)     // Zero-copy via in
);

// ❌ Bad: Collect all groups first
var bids = new List<BidData>(); // Heap allocations
var asks = new List<AskData>();
data.ConsumeVariableLengthSegments(
    variableData,
    (in BidData bid) => bids.Add(bid),
    (in AskData ask) => asks.Add(ask)
);
ProcessBids(bids);
ProcessAsks(asks);
```

## Group Processing

### Streaming Group Processing

Process repeating groups without materializing collections:

```csharp
// ✅ Good: Streaming processing
long totalVolume = 0;
data.ConsumeVariableLengthSegments(
    variableData,
    (in BidData bid) => totalVolume += bid.Quantity,
    (in AskData ask) => totalVolume += ask.Quantity
);

// ❌ Bad: Materialize groups
var allBids = new List<BidData>();
var allAsks = new List<AskData>();
data.ConsumeVariableLengthSegments(
    variableData,
    (in BidData bid) => allBids.Add(bid),
    (in AskData ask) => allAsks.Add(ask)
);
long totalVolume = allBids.Sum(b => b.Quantity) + allAsks.Sum(a => a.Quantity);
```

### Early Exit

Exit group processing early when possible:

```csharp
// ✅ Good: Early exit
bool found = false;
data.ConsumeVariableLengthSegments(
    variableData,
    bid => 
    {
        if (bid.Price == targetPrice)
        {
            found = true;
            return false; // Stop processing
        }
        return true;
    },
    ask => true
);

// ❌ Bad: Process all groups
var match = allBids.FirstOrDefault(b => b.Price == targetPrice);
```

### Pre-allocate for Known Sizes

When group sizes are known, pre-allocate:

```csharp
// ✅ Good: Pre-allocate when size is known
Span<BidData> bids = stackalloc BidData[expectedBidCount];
int bidIndex = 0;

data.ConsumeVariableLengthSegments(
    variableData,
    bid => bids[bidIndex++] = bid,
    ask => { }
);

// Process bids array directly
ProcessBids(bids.Slice(0, bidIndex));
```

## Profiling and Measurement

### Use BenchmarkDotNet

Always measure performance with BenchmarkDotNet:

```csharp
[MemoryDiagnoser]
public class MyBenchmarks
{
    private byte[] _buffer;
    private TradeData _trade;

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new byte[1024];
        _trade = new TradeData { /* ... */ };
    }

    [Benchmark]
    public bool EncodeTrade()
    {
        return _trade.TryEncode(_buffer, out _);
    }
}
```

### Monitor Allocations

Track allocations with memory profiler:

```csharp
// ✅ Target: 0 allocations
// Gen0: -
// Gen1: -
// Allocated: -
```

### Profile Hot Paths

Focus optimization on hot paths:

```csharp
// Identify hot paths with profiler
// Optimize in order:
// 1. Most frequently called code
// 2. Code in critical loops
// 3. Code with most allocations
```

## Common Pitfalls

### 1. Boxing Value Types

```csharp
// ❌ Bad: Boxing
object obj = trade; // Boxing - heap allocation
ProcessObject(obj);

// ✅ Good: Keep as value type
ProcessTrade(trade);
```

### 2. LINQ in Hot Paths

```csharp
// ❌ Bad: LINQ allocates
var total = trades.Sum(t => t.Price); // Allocations

// ✅ Good: Manual loop
long total = 0;
foreach (var trade in trades)
    total += trade.Price;
```

### 3. String Allocations

```csharp
// ❌ Bad: String allocation for each message
string symbol = Encoding.UTF8.GetString(symbolBytes);

// ✅ Good: Work with bytes directly
ProcessSymbolBytes(symbolBytes);
```

### 4. Defensive Copying

```csharp
// ❌ Bad: Non-readonly property accessed through in/ref causes defensive copy
public struct Trade
{
    public long Price { get => _price; }
}

void ProcessTrade(in Trade trade)
{
    var p = trade.Price; // Defensive copy of entire struct!
}

// ✅ Good: readonly prevents defensive copy (v0.9.0 generated code)
public struct Trade
{
    public readonly long Price { get => _price; }
}

void ProcessTrade(in Trade trade)
{
    var p = trade.Price; // Direct field load — no copy
}
```

> **Note**: On .NET 9 RyuJIT, the JIT elides defensive copies for trivial getters even without `readonly`. However, `readonly` is enforced at compile time and protects against Mono, NativeAOT, and alternative JITs. All v0.9.0 generated properties are `readonly`.

### 5. Closure Allocations

```csharp
// ❌ Bad: Closure allocation
int threshold = 100;
data.ConsumeVariableLengthSegments(
    variableData,
    bid => bid.Quantity > threshold, // Closure - allocation
    ask => true
);

// ✅ Good: Use local function or avoid capture
bool ProcessBid(BidData bid, int threshold) 
    => bid.Quantity > threshold;

data.ConsumeVariableLengthSegments(
    variableData,
    bid => ProcessBid(bid, 100),
    ask => true
);
```

## Performance Checklist

Before deployment, verify:

- [ ] No heap allocations in hot paths (Gen0/Gen1 = 0)
- [ ] Buffer reuse via ArrayPool or stack allocation
- [ ] Span<T> used instead of arrays where possible
- [ ] No unnecessary copies of value types
- [ ] Groups processed incrementally
- [ ] Early exits implemented where applicable
- [ ] Benchmarks show acceptable performance
- [ ] Profiling confirms no hotspots

## Recommended Tools

1. **BenchmarkDotNet** - Performance benchmarking
2. **dotMemory** - Memory profiling
3. **PerfView** - CPU profiling
4. **Rider** - Memory allocations tracking

## Performance Goals

Target metrics for well-optimized code:

- **Simple Message Encode/Decode**: < 100ns
- **Memory Allocations**: 0 bytes per operation
- **Throughput**: 1M+ messages/second
- **GC Pressure**: Minimal (Gen0/Gen1 = 0)

## References

- [.NET Performance Tips](https://docs.microsoft.com/en-us/dotnet/framework/performance/)
- [Span<T> Performance](https://docs.microsoft.com/en-us/archive/msdn-magazine/2018/january/csharp-all-about-span-exploring-a-new-net-mainstay)
- [Memory Management](https://docs.microsoft.com/en-us/dotnet/standard/automatic-memory-management)

---

**Last Updated**: 2025-10-16
