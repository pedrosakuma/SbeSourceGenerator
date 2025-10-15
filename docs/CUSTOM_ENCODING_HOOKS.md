# Custom Encoding/Decoding Hooks

## Overview

The SBE Code Generator now provides extensibility points for custom encoding and decoding logic through a comprehensive hook system. This allows users to inject custom serialization strategies without forking the codebase.

## Features

- **Pre/Post Encoding Hooks**: Intercept messages before and after encoding
- **Pre/Post Decoding Hooks**: Intercept messages before and after decoding  
- **Field-Level Hooks**: Customize encoding/decoding for specific fields
- **Backward Compatible**: All existing code continues to work unchanged
- **Zero Overhead**: Hooks are optional and only incur cost when used

## Quick Start

### Basic Usage

```csharp
using MySchema.Runtime;

// Define custom hooks
var hooks = new EncodingHooks<TradeData>
{
    PreEncoding = (ref TradeData msg) =>
    {
        // Validate before encoding
        if (msg.Price <= 0)
            return false;
        return true;
    },
    PostDecoding = (ref TradeData msg) =>
    {
        // Transform or validate after decoding
        if (msg.Quantity < 0)
            return false;
        return true;
    }
};

// Encode with hooks
var message = new TradeData { Price = 100, Quantity = 50 };
Span<byte> buffer = stackalloc byte[TradeData.MESSAGE_SIZE];
if (TradeData.TryEncode(ref message, buffer, hooks))
{
    // Message encoded successfully
}

// Decode with hooks
if (TradeData.TryParse(buffer, TradeData.MESSAGE_SIZE, out var decoded, out var varData, hooks))
{
    // Message decoded and validated successfully
}
```

## Hook Types

### Message-Level Hooks

#### Pre-Encoding Hook
Invoked before a message is encoded. Can modify the message or abort encoding.

```csharp
public delegate bool MessagePreEncodingHook<TMessage>(ref TMessage message) 
    where TMessage : struct;
```

**Use Cases:**
- Validation before encoding
- Applying transformations (e.g., rounding prices)
- Calculating checksums or signatures
- Enforcing business rules

**Example:**
```csharp
var hooks = new EncodingHooks<OrderData>
{
    PreEncoding = (ref OrderData msg) =>
    {
        // Round price to 2 decimal places
        msg.Price = Math.Round(msg.Price, 2);
        
        // Ensure quantity is positive
        if (msg.Quantity <= 0)
            return false;
            
        return true;
    }
};
```

#### Post-Encoding Hook
Invoked after a message has been encoded. Can inspect or modify the encoded buffer.

```csharp
public delegate void MessagePostEncodingHook<TMessage>(ref TMessage message, Span<byte> buffer) 
    where TMessage : struct;
```

**Use Cases:**
- Adding checksums or CRC
- Encrypting sensitive portions
- Logging encoded messages
- Compression

**Example:**
```csharp
var hooks = new EncodingHooks<OrderData>
{
    PostEncoding = (ref OrderData msg, Span<byte> buffer) =>
    {
        // Calculate and append checksum
        byte checksum = CalculateChecksum(buffer.Slice(0, OrderData.MESSAGE_SIZE));
        buffer[OrderData.MESSAGE_SIZE] = checksum;
    }
};
```

#### Pre-Decoding Hook
Invoked before a message is decoded. Can validate or transform the buffer.

```csharp
public delegate bool MessagePreDecodingHook(ReadOnlySpan<byte> buffer);
```

**Use Cases:**
- Validating checksums before decoding
- Decrypting data
- Checking buffer integrity
- Logging raw messages

**Example:**
```csharp
var hooks = new EncodingHooks<OrderData>
{
    PreDecoding = (ReadOnlySpan<byte> buffer) =>
    {
        // Verify checksum before decoding
        if (buffer.Length < OrderData.MESSAGE_SIZE + 1)
            return false;
            
        byte expectedChecksum = buffer[OrderData.MESSAGE_SIZE];
        byte actualChecksum = CalculateChecksum(buffer.Slice(0, OrderData.MESSAGE_SIZE));
        
        return expectedChecksum == actualChecksum;
    }
};
```

#### Post-Decoding Hook
Invoked after a message has been decoded. Can validate or transform the decoded message.

```csharp
public delegate bool MessagePostDecodingHook<TMessage>(ref TMessage message) 
    where TMessage : struct;
```

**Use Cases:**
- Validating decoded values
- Applying domain-specific transformations
- Setting computed fields
- Enforcing invariants

**Example:**
```csharp
var hooks = new EncodingHooks<OrderData>
{
    PostDecoding = (ref OrderData msg) =>
    {
        // Validate price range
        if (msg.Price < 0.01 || msg.Price > 1000000)
            return false;
            
        // Validate quantity range
        if (msg.Quantity < 1 || msg.Quantity > 1000000)
            return false;
            
        return true;
    }
};
```

### Field-Level Hooks

Field-level hooks allow fine-grained control over individual field serialization:

```csharp
public delegate bool FieldEncodingHook<T>(string fieldName, ref T value);
public delegate bool FieldDecodingHook<T>(string fieldName, ref T value);
```

**Note:** Field-level hooks are defined for future extensibility. Currently, message-level hooks provide the primary extensibility mechanism. Field-level hooks can be implemented in partial classes when needed.

## Advanced Scenarios

### Encryption/Decryption

```csharp
var encryptionKey = GetEncryptionKey();

var hooks = new EncodingHooks<SensitiveData>
{
    PostEncoding = (ref SensitiveData msg, Span<byte> buffer) =>
    {
        // Encrypt sensitive portions (e.g., after offset 8)
        var sensitiveData = buffer.Slice(8, 16);
        Encrypt(sensitiveData, encryptionKey);
    },
    PreDecoding = (ReadOnlySpan<byte> buffer) =>
    {
        // Decrypt before parsing
        var mutableBuffer = buffer.ToArray();
        var sensitiveData = mutableBuffer.AsSpan(8, 16);
        Decrypt(sensitiveData, encryptionKey);
        return true;
    }
};
```

### Custom Compression

```csharp
var hooks = new EncodingHooks<LargeMessage>
{
    PostEncoding = (ref LargeMessage msg, Span<byte> buffer) =>
    {
        // Compress variable-length data
        // (Implementation depends on compression library)
    },
    PreDecoding = (ReadOnlySpan<byte> buffer) =>
    {
        // Decompress before decoding
        // (Implementation depends on compression library)
        return true;
    }
};
```

### Audit Logging

```csharp
var logger = GetLogger();

var hooks = new EncodingHooks<TradeData>
{
    PreEncoding = (ref TradeData msg) =>
    {
        logger.LogInformation($"Encoding trade: {msg.TradeId}");
        return true;
    },
    PostDecoding = (ref TradeData msg) =>
    {
        logger.LogInformation($"Decoded trade: {msg.TradeId}, Price: {msg.Price}");
        return true;
    }
};
```

### Schema Evolution with Custom Transformation

```csharp
var hooks = new EncodingHooks<OrderV2>
{
    PostDecoding = (ref OrderV2 msg) =>
    {
        // Transform V1 data to V2 format
        if (msg.Version == 1)
        {
            // Apply default values for new V2 fields
            msg.NewField = GetDefaultValue();
        }
        return true;
    }
};
```

## Using Partial Classes for Custom Logic

Since generated messages are `partial struct`, you can extend them with custom methods:

```csharp
// In your own file
namespace MySchema
{
    public partial struct TradeData
    {
        // Custom validation method
        public bool IsValid()
        {
            return Price > 0 && Quantity > 0;
        }
        
        // Custom transformation
        public void Normalize()
        {
            Price = Math.Round(Price, 2);
        }
        
        // Custom encoding with built-in validation
        public static bool TryEncodeValidated(ref TradeData message, Span<byte> buffer)
        {
            if (!message.IsValid())
                return false;
                
            var hooks = new EncodingHooks<TradeData>
            {
                PreEncoding = (ref TradeData msg) =>
                {
                    msg.Normalize();
                    return true;
                }
            };
            
            return TryEncode(ref message, buffer, hooks);
        }
    }
}
```

## Performance Considerations

### Zero Overhead When Not Used

Hooks are completely optional. If you don't provide hooks, the standard code path is used with no overhead:

```csharp
// No hooks - zero overhead
TradeData.TryParse(buffer, out var msg, out var varData);

// With hooks - minimal overhead (one delegate call per hook)
TradeData.TryParse(buffer, TradeData.MESSAGE_SIZE, out msg, out varData, hooks);
```

### Delegate Caching

For optimal performance, cache hook instances rather than creating them repeatedly:

```csharp
// Good - create once, reuse many times
private static readonly EncodingHooks<TradeData> _tradeHooks = new()
{
    PostDecoding = ValidateTrade
};

// Use cached hooks in hot path
TradeData.TryParse(buffer, TradeData.MESSAGE_SIZE, out var msg, out var varData, _tradeHooks);
```

### Aggressive Inlining

The hook infrastructure uses `AggressiveInlining` to minimize call overhead:

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static bool TryEncode<TMessage>(ref TMessage message, Span<byte> buffer, 
    EncodingHooks<TMessage>? hooks = null)
```

## API Reference

### EncodingHooks<TMessage> Class

Container for all hook delegates for a message type.

**Properties:**
- `PreEncoding`: Hook invoked before encoding
- `PostEncoding`: Hook invoked after encoding  
- `PreDecoding`: Hook invoked before decoding
- `PostDecoding`: Hook invoked after decoding

### EncodingHooksHelper Class

Static helper methods for encoding/decoding with hooks.

**Methods:**
- `TryEncode<TMessage>(ref TMessage message, Span<byte> buffer, EncodingHooks<TMessage>? hooks = null)`
- `TryDecode<TMessage>(ReadOnlySpan<byte> buffer, out TMessage message, EncodingHooks<TMessage>? hooks = null)`

### Generated Message Methods

Each generated message includes:

**Existing Methods (unchanged):**
- `TryParse(ReadOnlySpan<byte> buffer, out TMessage message, out ReadOnlySpan<byte> variableData)`
- `TryParse(ReadOnlySpan<byte> buffer, int blockLength, out TMessage message, out ReadOnlySpan<byte> variableData)`
- `TryParseWithReader(ref SpanReader reader, int blockLength, out TMessage message)`

**New Methods:**
- `TryParse(ReadOnlySpan<byte> buffer, int blockLength, out TMessage message, out ReadOnlySpan<byte> variableData, EncodingHooks<TMessage>? hooks)` - Parse with custom hooks
- `TryEncode(ref TMessage message, Span<byte> buffer, EncodingHooks<TMessage>? hooks = null)` - Encode with custom hooks

## Migration Guide

### From Direct MemoryMarshal Usage

**Before:**
```csharp
var message = new TradeData { Price = 100 };
Span<byte> buffer = stackalloc byte[TradeData.MESSAGE_SIZE];
MemoryMarshal.Write(buffer, ref message);
```

**After (with hooks):**
```csharp
var message = new TradeData { Price = 100 };
Span<byte> buffer = stackalloc byte[TradeData.MESSAGE_SIZE];
var hooks = new EncodingHooks<TradeData>
{
    PreEncoding = ValidateAndNormalize
};
TradeData.TryEncode(ref message, buffer, hooks);
```

### From Custom Wrapper Classes

**Before:**
```csharp
public class TradeEncoder
{
    public byte[] Encode(TradeData trade)
    {
        // Custom validation
        if (!Validate(trade))
            throw new Exception();
            
        // Encode
        var buffer = new byte[TradeData.MESSAGE_SIZE];
        MemoryMarshal.Write(buffer, ref trade);
        return buffer;
    }
}
```

**After:**
```csharp
// Use hooks instead
var hooks = new EncodingHooks<TradeData>
{
    PreEncoding = (ref TradeData msg) => Validate(msg)
};

// Encode directly with hooks
Span<byte> buffer = stackalloc byte[TradeData.MESSAGE_SIZE];
TradeData.TryEncode(ref message, buffer, hooks);
```

## Examples

### Complete Example: Validated Trading System

```csharp
using System;
using MySchema;
using MySchema.Runtime;

public class TradingSystem
{
    private static readonly EncodingHooks<TradeData> _hooks = new()
    {
        PreEncoding = (ref TradeData trade) =>
        {
            // Validate trade before encoding
            if (trade.Price <= 0 || trade.Price > 1000000)
                return false;
            if (trade.Quantity <= 0 || trade.Quantity > 1000000)
                return false;
                
            // Normalize price to 2 decimals
            trade.Price = Math.Round(trade.Price, 2);
            return true;
        },
        PostDecoding = (ref TradeData trade) =>
        {
            // Validate decoded trade
            if (trade.Price <= 0 || trade.Quantity <= 0)
                return false;
                
            return true;
        }
    };
    
    public bool SendTrade(TradeData trade, Stream network)
    {
        Span<byte> buffer = stackalloc byte[TradeData.MESSAGE_SIZE];
        
        // Encode with validation
        if (!TradeData.TryEncode(ref trade, buffer, _hooks))
            return false;
            
        // Send to network
        network.Write(buffer);
        return true;
    }
    
    public bool ReceiveTrade(Stream network, out TradeData trade)
    {
        Span<byte> buffer = stackalloc byte[TradeData.MESSAGE_SIZE];
        network.Read(buffer);
        
        // Decode with validation
        return TradeData.TryParse(buffer, TradeData.MESSAGE_SIZE, 
            out trade, out _, _hooks);
    }
}
```

## Testing

The hook system includes comprehensive tests:

- `EncodingHooksTests.cs` - Unit tests for hook generation
- All tests verify backward compatibility
- Snapshot tests ensure generated code remains consistent

Run tests:
```bash
dotnet test tests/SbeCodeGenerator.Tests/SbeCodeGenerator.Tests.csproj
```

## Limitations

1. **Field-level hooks**: Currently defined but require manual implementation in partial classes
2. **Async operations**: Hooks are synchronous (SBE is designed for synchronous, low-latency scenarios)
3. **Thread safety**: Hook instances should be thread-safe if shared across threads

## Best Practices

1. **Cache hook instances**: Create once, reuse many times
2. **Keep hooks lightweight**: Avoid expensive operations in hot paths
3. **Validate early**: Use PreEncoding/PostDecoding for validation
4. **Use partial classes**: Extend generated types for domain-specific logic
5. **Document custom hooks**: Make hook behavior clear for maintainers
6. **Test thoroughly**: Test both success and failure paths with hooks

## Troubleshooting

### Hook Returns False But No Error Message

Hooks return `bool` to indicate success/failure. Consider adding logging:

```csharp
PreEncoding = (ref TradeData msg) =>
{
    if (msg.Price <= 0)
    {
        Log.Warning("Invalid price: {Price}", msg.Price);
        return false;
    }
    return true;
}
```

### Performance Degradation

- Ensure hooks are cached, not recreated
- Profile hook implementations
- Consider whether hooks are needed in hot paths
- Use conditional compilation for debug-only hooks

### Compilation Errors After Update

- Rebuild the generator project
- Clean and rebuild consuming projects
- Check that all necessary using directives are present

## See Also

- [SBE_IMPLEMENTATION_ROADMAP.md](./SBE_IMPLEMENTATION_ROADMAP.md) - Phase 3.1
- [SBE_FEATURE_COMPLETENESS.md](./SBE_FEATURE_COMPLETENESS.md) - Custom Encoding section
- [SPAN_READER_EXTENSIBILITY.md](./SPAN_READER_EXTENSIBILITY.md) - SpanReader extensibility
