# Fluent Encoder API

This document demonstrates the new fluent encoder API for improved usability when encoding SBE messages with groups and variable-length data.

## Problem with Traditional API

The traditional API required manual management of `SpanWriter` and multiple static method calls:

```csharp
// Old way - error-prone and verbose
var orderBook = new OrderBookData { InstrumentId = 42 };
var bids = new[] { /* ... */ };
var asks = new[] { /* ... */ };

Span<byte> buffer = stackalloc byte[1024];
if (!orderBook.BeginEncoding(buffer, out var writer))
{
    // Handle error
}

if (!OrderBookData.TryEncodeBids(ref writer, bids))
{
    // Handle error
}

if (!OrderBookData.TryEncodeAsks(ref writer, asks))
{
    // Handle error
}

int bytesWritten = writer.BytesWritten;
```

### Issues with Traditional API:
- **Error-prone**: Easy to forget to encode a group or encode in wrong order
- **Not discoverable**: Users must know the static method names
- **Verbose**: Requires manual error checking at each step
- **Writer lifecycle**: Manual management of `SpanWriter` reference

## New Fluent Encoder API

The new fluent API provides a type-safe, discoverable, and error-resistant approach:

```csharp
// New way - fluent and discoverable
var orderBook = new OrderBookData { InstrumentId = 42 };
var bids = new[] { /* ... */ };
var asks = new[] { /* ... */ };

Span<byte> buffer = stackalloc byte[1024];
var encoder = orderBook.CreateEncoder(buffer)
    .WithBids(bids)
    .WithAsks(asks);

int bytesWritten = encoder.BytesWritten;
```

### Benefits:
- **Type-safe**: Compiler helps you discover the available `With*` methods via IntelliSense
- **Fluent**: Method chaining makes the code more readable
- **Error-resistant**: The encoder manages the writer lifecycle internally
- **Backward compatible**: Traditional API still available for advanced scenarios

## Usage Examples

### Example 1: Encoding Groups

```csharp
using System;

var orderBook = new OrderBookData { InstrumentId = 123 };

var bids = new[]
{
    new OrderBookData.BidsData { Price = 100, Quantity = 10 },
    new OrderBookData.BidsData { Price = 101, Quantity = 11 }
};

var asks = new[]
{
    new OrderBookData.AsksData { Price = 200, Quantity = 20 }
};

Span<byte> buffer = stackalloc byte[1024];

// Fluent encoding
var encoder = orderBook.CreateEncoder(buffer)
    .WithBids(bids)
    .WithAsks(asks);

Console.WriteLine($"Encoded {encoder.BytesWritten} bytes");
```

### Example 2: Encoding Variable-Length Data

```csharp
using System;
using System.Text;

var order = new NewOrderData
{
    OrderId = 456,
    Price = 9950,
    Quantity = 100,
    Side = OrderSide.Buy,
    OrderType = OrderType.Limit
};

var symbolBytes = Encoding.UTF8.GetBytes("AAPL");

Span<byte> buffer = stackalloc byte[512];

// Fluent encoding with varData
var encoder = order.CreateEncoder(buffer)
    .WithSymbol(symbolBytes);

Console.WriteLine($"Encoded order with symbol, {encoder.BytesWritten} bytes");
```

### Example 3: Error Handling with TryWith*

For scenarios where you want explicit error handling without exceptions:

```csharp
var orderBook = new OrderBookData { InstrumentId = 42 };
var bids = new[] { /* ... */ };

Span<byte> buffer = stackalloc byte[1024];

var encoder = orderBook.CreateEncoder(buffer);

if (!encoder.TryWithBids(bids))
{
    Console.WriteLine("Failed to encode bids");
    return;
}

// Continue encoding...
int bytesWritten = encoder.BytesWritten;
```

### Example 4: Comparison - Both APIs Produce Identical Results

```csharp
// Traditional API
Span<byte> bufferOld = stackalloc byte[1024];
orderBook.BeginEncoding(bufferOld, out var writer);
OrderBookData.TryEncodeBids(ref writer, bids);
OrderBookData.TryEncodeAsks(ref writer, asks);

// Fluent API
Span<byte> bufferNew = stackalloc byte[1024];
var encoder = orderBook.CreateEncoder(bufferNew)
    .WithBids(bids)
    .WithAsks(asks);

// Both produce identical binary output
Assert.Equal(writer.BytesWritten, encoder.BytesWritten);
Assert.True(bufferOld.SequenceEqual(bufferNew));
```

## API Reference

### CreateEncoder Method

```csharp
public {MessageName}Encoder CreateEncoder(Span<byte> buffer)
```

Creates a fluent encoder for the message. Returns an encoder that can be used to chain encoding calls.

### With{GroupName} Methods

```csharp
public {MessageName}Encoder With{GroupName}(ReadOnlySpan<{GroupName}Data> entries)
```

Encodes a group and returns this encoder for method chaining. Throws `InvalidOperationException` if encoding fails.

### TryWith{GroupName} Methods

```csharp
public bool TryWith{GroupName}(ReadOnlySpan<{GroupName}Data> entries)
```

Attempts to encode a group. Returns `true` if successful, `false` otherwise. Does not throw exceptions.

### With{VarDataName} Methods

```csharp
public {MessageName}Encoder With{VarDataName}(ReadOnlySpan<byte> data)
```

Encodes a variable-length data field and returns this encoder for method chaining. Throws `InvalidOperationException` if encoding fails.

### TryWith{VarDataName} Methods

```csharp
public bool TryWith{VarDataName}(ReadOnlySpan<byte> data)
```

Attempts to encode a variable-length data field. Returns `true` if successful, `false` otherwise. Does not throw exceptions.

### BytesWritten Property

```csharp
public int BytesWritten { get; }
```

Gets the total number of bytes written to the buffer, including the message header and all encoded groups/varData.

## Migration Guide

To migrate from the traditional API to the fluent API:

### Before (Traditional):
```csharp
orderBook.BeginEncoding(buffer, out var writer);
OrderBookData.TryEncodeBids(ref writer, bids);
OrderBookData.TryEncodeAsks(ref writer, asks);
int bytes = writer.BytesWritten;
```

### After (Fluent):
```csharp
var bytes = orderBook.CreateEncoder(buffer)
    .WithBids(bids)
    .WithAsks(asks)
    .BytesWritten;
```

## Performance

The fluent API has **zero performance overhead** compared to the traditional API:
- Both use the same underlying `SpanWriter`
- No heap allocations (uses `ref struct`)
- Identical generated IL code for the encoding operations

## When to Use Each API

### Use Fluent API (Recommended):
- ✅ Most common scenarios
- ✅ When you want discoverable, type-safe encoding
- ✅ When encoding all groups/varData in sequence
- ✅ For improved code readability

### Use Traditional API:
- ⚙️ Advanced scenarios requiring manual writer control
- ⚙️ When sharing a single writer across multiple messages
- ⚙️ When you need fine-grained control over the encoding process
- ⚙️ Custom encoding logic that doesn't fit the fluent pattern
