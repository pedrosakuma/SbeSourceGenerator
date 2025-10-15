# Custom Encoding/Decoding Hooks Examples

This directory contains examples demonstrating the custom encoding/decoding hooks feature.

## Overview

The custom hooks feature allows you to inject custom logic at various points in the encoding/decoding lifecycle:

- **PreEncoding**: Validate or transform data before encoding
- **PostEncoding**: Process encoded data (checksums, encryption, etc.)
- **PreDecoding**: Validate or prepare data before decoding
- **PostDecoding**: Validate or transform decoded data

## Examples

### CustomEncodingHooksExample.cs

This file contains code examples showing:

1. **Validation Hooks** - Validate data before encoding and after decoding
2. **Reusable Hooks Pattern** - Create cached, reusable hook instances for performance

## Usage

The examples show the pattern for using hooks with generated SBE messages. When you generate code from your SBE schema, you'll get message types with `TryEncode` and `TryParse` methods that accept optional hooks:

```csharp
var hooks = new EncodingHooks<MyMessage>
{
    PreEncoding = (ref MyMessage msg) => 
    {
        // Your validation/transformation logic
        return true; // Return false to abort encoding
    },
    PostDecoding = (ref MyMessage msg) =>
    {
        // Your validation/transformation logic
        return true; // Return false to indicate error
    }
};

// Use with encoding
MyMessage.TryEncode(ref message, buffer, hooks);

// Use with decoding
MyMessage.TryParse(buffer, blockLength, out var decoded, out var varData, hooks);
```

## See Also

- [CUSTOM_ENCODING_HOOKS.md](../CUSTOM_ENCODING_HOOKS.md) - Complete documentation
- [SBE_IMPLEMENTATION_ROADMAP.md](../SBE_IMPLEMENTATION_ROADMAP.md) - Implementation roadmap
