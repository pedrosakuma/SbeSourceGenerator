# Code Generation Examples: Write Support

This document shows examples of how generated code would look with payload writing support.

## Example 1: Simple Message (Before and After)

### Current Generated Code (Read-Only)

```csharp
using System;
using System.Runtime.InteropServices;
using Test.Schema.Runtime;

namespace Test.Schema;

/// <summary>
/// Trade message
/// (MessageDefinition)
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public partial struct TradeData
{
    /// <summary>
    /// Message Id
    /// (ConstantMessageFieldDefinition)
    /// </summary>
    public const int MESSAGE_ID = 1;
    
    /// <summary>
    /// Message Size
    /// (ConstantMessageFieldDefinition)
    /// </summary>
    public const int MESSAGE_SIZE = 25;
    
    /// <summary>
    /// Trade ID
    /// (MessageFieldDefinition)
    /// </summary>
    [FieldOffset(0)]
    public long TradeId;
    
    /// <summary>
    /// Trade price
    /// (MessageFieldDefinition)
    /// </summary>
    [FieldOffset(8)]
    public long Price;
    
    /// <summary>
    /// Trade quantity
    /// (MessageFieldDefinition)
    /// </summary>
    [FieldOffset(16)]
    public long Quantity;
    
    /// <summary>
    /// Trade side
    /// (MessageFieldDefinition)
    /// </summary>
    [FieldOffset(24)]
    public Side Side;
    
    // READING ONLY
    public static bool TryParse(ReadOnlySpan<byte> buffer, out TradeData message, out ReadOnlySpan<byte> variableData)
    {
        return TryParse(buffer, MESSAGE_SIZE, out message, out variableData);
    }
    
    public static bool TryParse(ReadOnlySpan<byte> buffer, int blockLength, out TradeData message, out ReadOnlySpan<byte> variableData)
    {
        var reader = new SpanReader(buffer);
        
        if (!reader.TryRead<TradeData>(out message))
        {
            variableData = default;
            return false;
        }
        
        var additionalBytes = blockLength - MESSAGE_SIZE;
        if (additionalBytes > 0)
        {
            if (!reader.TrySkip(additionalBytes))
            {
                variableData = default;
                return false;
            }
            variableData = reader.Remaining;
        }
        else
        {
            variableData = buffer.Slice(blockLength);
        }
        
        return true;
    }
}
```

### Proposed Generated Code (With Writing Support)

```csharp
using System;
using System.Runtime.InteropServices;
using Test.Schema.Runtime;

namespace Test.Schema;

/// <summary>
/// Trade message
/// (MessageDefinition)
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public partial struct TradeData
{
    /// <summary>
    /// Message Id
    /// (ConstantMessageFieldDefinition)
    /// </summary>
    public const int MESSAGE_ID = 1;
    
    /// <summary>
    /// Message Size
    /// (ConstantMessageFieldDefinition)
    /// </summary>
    public const int MESSAGE_SIZE = 25;
    
    /// <summary>
    /// Trade ID
    /// (MessageFieldDefinition)
    /// </summary>
    [FieldOffset(0)]
    public long TradeId;
    
    /// <summary>
    /// Trade price
    /// (MessageFieldDefinition)
    /// </summary>
    [FieldOffset(8)]
    public long Price;
    
    /// <summary>
    /// Trade quantity
    /// (MessageFieldDefinition)
    /// </summary>
    [FieldOffset(16)]
    public long Quantity;
    
    /// <summary>
    /// Trade side
    /// (MessageFieldDefinition)
    /// </summary>
    [FieldOffset(24)]
    public Side Side;
    
    // READING (unchanged)
    public static bool TryParse(ReadOnlySpan<byte> buffer, out TradeData message, out ReadOnlySpan<byte> variableData)
    {
        return TryParse(buffer, MESSAGE_SIZE, out message, out variableData);
    }
    
    public static bool TryParse(ReadOnlySpan<byte> buffer, int blockLength, out TradeData message, out ReadOnlySpan<byte> variableData)
    {
        var reader = new SpanReader(buffer);
        
        if (!reader.TryRead<TradeData>(out message))
        {
            variableData = default;
            return false;
        }
        
        var additionalBytes = blockLength - MESSAGE_SIZE;
        if (additionalBytes > 0)
        {
            if (!reader.TrySkip(additionalBytes))
            {
                variableData = default;
                return false;
            }
            variableData = reader.Remaining;
        }
        else
        {
            variableData = buffer.Slice(blockLength);
        }
        
        return true;
    }
    
    // NEW: WRITING METHODS
    
    /// <summary>
    /// Encodes this message to the provided buffer.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="bytesWritten">Number of bytes written on success.</param>
    /// <returns>True if encoding succeeded; otherwise, false.</returns>
    public bool TryEncode(Span<byte> buffer, out int bytesWritten)
    {
        if (buffer.Length < MESSAGE_SIZE)
        {
            bytesWritten = 0;
            return false;
        }
        
        var writer = new SpanWriter(buffer);
        writer.Write(this);
        bytesWritten = MESSAGE_SIZE;
        return true;
    }
    
    /// <summary>
    /// Encodes this message using an existing SpanWriter.
    /// Useful for composing multiple messages or adding headers.
    /// </summary>
    /// <param name="writer">The writer to use.</param>
    /// <returns>True if encoding succeeded; otherwise, false.</returns>
    public bool TryEncodeWithWriter(ref SpanWriter writer)
    {
        return writer.TryWrite(this);
    }
    
    /// <summary>
    /// Encodes this message to the provided buffer.
    /// Throws InvalidOperationException if encoding fails.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <returns>Number of bytes written.</returns>
    /// <exception cref="InvalidOperationException">Thrown when buffer is too small.</exception>
    public int Encode(Span<byte> buffer)
    {
        if (!TryEncode(buffer, out int bytesWritten))
            throw new InvalidOperationException($"Failed to encode {nameof(TradeData)}. Buffer size: {buffer.Length}, Required: {MESSAGE_SIZE}");
        
        return bytesWritten;
    }
}
```

## Example 2: Usage Patterns

### Pattern 1: Simple Encode and Send

```csharp
// Create message
var trade = new TradeData
{
    TradeId = 123456,
    Price = 9950,
    Quantity = 100,
    Side = Side.Buy
};

// Encode to buffer
var buffer = new byte[TradeData.MESSAGE_SIZE];
if (trade.TryEncode(buffer, out int bytesWritten))
{
    // Send via socket
    await socket.SendAsync(buffer.AsMemory(0, bytesWritten));
}
```

### Pattern 2: Encode Multiple Messages

```csharp
var buffer = new byte[1024];
var writer = new SpanWriter(buffer);

// Encode header
var header = new MessageHeader { /* ... */ };
writer.Write(header);

// Encode trade
var trade = new TradeData { /* ... */ };
trade.TryEncodeWithWriter(ref writer);

// Send all
int totalBytes = writer.BytesWritten;
await socket.SendAsync(buffer.AsMemory(0, totalBytes));
```

### Pattern 3: Round-Trip Testing

```csharp
// Create and encode
var original = new TradeData
{
    TradeId = 123456,
    Price = 9950,
    Quantity = 100,
    Side = Side.Buy
};

var buffer = new byte[TradeData.MESSAGE_SIZE];
original.TryEncode(buffer, out int written);

// Decode
TradeData.TryParse(buffer, out var decoded, out _);

// Verify
Assert.Equal(original.TradeId, decoded.TradeId);
Assert.Equal(original.Price, decoded.Price);
Assert.Equal(original.Quantity, decoded.Quantity);
Assert.Equal(original.Side, decoded.Side);
```

## Example 3: Message with Optional Fields

### Generated Code

```csharp
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public partial struct OrderData
{
    public const int MESSAGE_SIZE = 32;
    
    [FieldOffset(0)]
    public long OrderId;
    
    [FieldOffset(8)]
    public long Price;
    
    // Optional field - uses nullable pattern
    [FieldOffset(16)]
    private long _quantity;
    
    public long? Quantity
    {
        get => _quantity == long.MaxValue ? null : _quantity;
        set => _quantity = value ?? long.MaxValue;
    }
    
    // Reading (existing)
    public static bool TryParse(ReadOnlySpan<byte> buffer, out OrderData message, out ReadOnlySpan<byte> variableData)
    {
        // ... existing implementation
    }
    
    // Writing (new)
    public bool TryEncode(Span<byte> buffer, out int bytesWritten)
    {
        if (buffer.Length < MESSAGE_SIZE)
        {
            bytesWritten = 0;
            return false;
        }
        
        // Create temp struct with resolved optional values
        var temp = this;
        temp._quantity = _quantity; // Already has null value if not set
        
        var writer = new SpanWriter(buffer);
        writer.Write(temp);
        bytesWritten = MESSAGE_SIZE;
        return true;
    }
}
```

### Usage

```csharp
var order = new OrderData
{
    OrderId = 123,
    Price = 100,
    Quantity = 50  // Optional - can be null
};

var buffer = new byte[OrderData.MESSAGE_SIZE];
order.TryEncode(buffer, out int written);

// Later decode
OrderData.TryParse(buffer, out var decoded, out _);
Assert.Equal(50, decoded.Quantity);  // Correctly decoded

// With null quantity
order.Quantity = null;
order.TryEncode(buffer, out written);
OrderData.TryParse(buffer, out decoded, out _);
Assert.Null(decoded.Quantity);  // Correctly decoded as null
```

## Example 4: Message with Repeating Groups

### Current Schema

```xml
<sbe:message name="OrderBook" id="10">
    <field name="symbol" id="1" type="char" length="8"/>
    <field name="timestamp" id="2" type="uint64"/>
    <group name="Bids" id="3" dimensionType="GroupSizeEncoding">
        <field name="price" id="4" type="int64"/>
        <field name="quantity" id="5" type="int64"/>
    </group>
    <group name="Asks" id="6" dimensionType="GroupSizeEncoding">
        <field name="price" id="7" type="int64"/>
        <field name="quantity" id="8" type="int64"/>
    </group>
</sbe:message>
```

### Generated Code (Proposed)

```csharp
public partial struct OrderBookData
{
    public const int MESSAGE_SIZE = 16;  // symbol + timestamp
    
    [FieldOffset(0)]
    private fixed byte _symbol[8];
    
    [FieldOffset(8)]
    public ulong Timestamp;
    
    // Reading groups (existing pattern)
    public void ConsumeVariableLengthSegments(
        ReadOnlySpan<byte> variableData,
        Action<BidsGroupData> onBid,
        Action<AsksGroupData> onAsk)
    {
        // ... existing implementation
    }
    
    // NEW: Writing groups
    
    /// <summary>
    /// Encodes this message with its repeating groups.
    /// </summary>
    public bool TryEncode(
        Span<byte> buffer,
        ReadOnlySpan<BidsGroupData> bids,
        ReadOnlySpan<AsksGroupData> asks,
        out int bytesWritten)
    {
        var writer = new SpanWriter(buffer);
        
        // Write fixed fields
        if (!writer.TryWrite(this))
        {
            bytesWritten = 0;
            return false;
        }
        
        // Write Bids group
        if (!TryEncodeBidsGroup(ref writer, bids))
        {
            bytesWritten = 0;
            return false;
        }
        
        // Write Asks group
        if (!TryEncodeAsksGroup(ref writer, asks))
        {
            bytesWritten = 0;
            return false;
        }
        
        bytesWritten = writer.BytesWritten;
        return true;
    }
    
    private static bool TryEncodeBidsGroup(ref SpanWriter writer, ReadOnlySpan<BidsGroupData> entries)
    {
        // Write group header
        var header = new GroupSizeEncoding
        {
            BlockLength = (ushort)BidsGroupData.ENTRY_SIZE,
            NumInGroup = (uint)entries.Length
        };
        
        if (!writer.TryWrite(header))
            return false;
        
        // Write entries
        foreach (var entry in entries)
        {
            if (!writer.TryWrite(entry))
                return false;
        }
        
        return true;
    }
    
    private static bool TryEncodeAsksGroup(ref SpanWriter writer, ReadOnlySpan<AsksGroupData> entries)
    {
        // Write group header
        var header = new GroupSizeEncoding
        {
            BlockLength = (ushort)AsksGroupData.ENTRY_SIZE,
            NumInGroup = (uint)entries.Length
        };
        
        if (!writer.TryWrite(header))
            return false;
        
        // Write entries
        foreach (var entry in entries)
        {
            if (!writer.TryWrite(entry))
                return false;
        }
        
        return true;
    }
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public partial struct BidsGroupData
{
    public const int ENTRY_SIZE = 16;
    
    [FieldOffset(0)]
    public long Price;
    
    [FieldOffset(8)]
    public long Quantity;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public partial struct AsksGroupData
{
    public const int ENTRY_SIZE = 16;
    
    [FieldOffset(0)]
    public long Price;
    
    [FieldOffset(8)]
    public long Quantity;
}
```

### Usage with Groups

```csharp
// Create message
var orderBook = new OrderBookData
{
    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};
// Set symbol
orderBook.SetSymbol("AAPL");

// Create bids
var bids = new BidsGroupData[]
{
    new() { Price = 15000, Quantity = 100 },
    new() { Price = 14990, Quantity = 200 },
    new() { Price = 14980, Quantity = 150 }
};

// Create asks
var asks = new AsksGroupData[]
{
    new() { Price = 15010, Quantity = 75 },
    new() { Price = 15020, Quantity = 125 }
};

// Encode
var buffer = new byte[1024];
if (orderBook.TryEncode(buffer, bids, asks, out int bytesWritten))
{
    // Send buffer
    await socket.SendAsync(buffer.AsMemory(0, bytesWritten));
}
```

## Example 5: Variable-Length Data

### Schema

```xml
<sbe:message name="TextMessage" id="20">
    <field name="msgId" id="1" type="uint32"/>
    <data name="text" id="2" type="varDataEncoding"/>
</sbe:message>
```

### Generated Code

```csharp
public partial struct TextMessageData
{
    public const int MESSAGE_SIZE = 4;  // Just msgId
    
    [FieldOffset(0)]
    public uint MsgId;
    
    // Reading (existing)
    public static bool TryParse(ReadOnlySpan<byte> buffer, out TextMessageData message, out ReadOnlySpan<byte> variableData)
    {
        // ... existing
    }
    
    // NEW: Writing with varData
    public bool TryEncode(Span<byte> buffer, ReadOnlySpan<byte> textData, out int bytesWritten)
    {
        var writer = new SpanWriter(buffer);
        
        // Write fixed fields
        if (!writer.TryWrite(this))
        {
            bytesWritten = 0;
            return false;
        }
        
        // Write varData length (uint16)
        if (!writer.TryWrite((ushort)textData.Length))
        {
            bytesWritten = 0;
            return false;
        }
        
        // Write varData content
        if (!writer.TryWriteBytes(textData))
        {
            bytesWritten = 0;
            return false;
        }
        
        bytesWritten = writer.BytesWritten;
        return true;
    }
}
```

### Usage

```csharp
var message = new TextMessageData { MsgId = 12345 };
var text = "Hello, SBE!"u8;

var buffer = new byte[1024];
if (message.TryEncode(buffer, text, out int bytesWritten))
{
    // Send
    await socket.SendAsync(buffer.AsMemory(0, bytesWritten));
}

// Decode
TextMessageData.TryParse(buffer, out var decoded, out var varData);
// Extract text varData
var reader = new SpanReader(varData);
if (reader.TryRead<ushort>(out var textLength) &&
    reader.TryReadBytes(textLength, out var textBytes))
{
    var decodedText = Encoding.UTF8.GetString(textBytes);
    Console.WriteLine(decodedText);  // "Hello, SBE!"
}
```

---

## Summary

The examples above demonstrate:

1. ✅ **Non-breaking changes** - All existing TryParse methods remain unchanged
2. ✅ **Symmetric API** - TryEncode mirrors TryParse
3. ✅ **Zero allocations** - All operations use Span/ref struct
4. ✅ **Composable** - Can use SpanWriter for complex scenarios
5. ✅ **Safe** - Bounds checking prevents buffer overruns
6. ✅ **Complete** - Supports all SBE features (optional, groups, varData)

All examples follow existing code generation patterns and conventions established in the project.
