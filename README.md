# SBE Code Generator for C#

[![CI](https://github.com/pedrosakuma/SbeSourceGenerator/actions/workflows/ci.yml/badge.svg)](https://github.com/pedrosakuma/SbeSourceGenerator/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/SbeSourceGenerator.svg)](https://www.nuget.org/packages/SbeSourceGenerator/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SbeSourceGenerator.svg)](https://www.nuget.org/packages/SbeSourceGenerator/)

A Roslyn-based source generator that converts FIX Simple Binary Encoding (SBE) XML schemas into efficient, type-safe C# code.

## Features

✅ **Fully Implemented SBE Features**:
- All primitive types (int8, int16, int32, int64, uint8, uint16, uint32, uint64, char)
- **Message encoding/decoding** with proper field layout
- **Optional fields** with null value semantics and encoding support
- Composite types with nested fields
- Enumerations (enums)
- Bit sets (choice sets with flags)
- **Repeating groups** with dimension encoding (read and write)
- **Variable-length data (varData)** encoding and decoding
- Constant fields in messages, composites, and groups
- Automatic and manual field offset calculation
- **Byte order (endianness) handling** - Little-endian and big-endian support
- Validation constraints (min/max ranges)
- Comprehensive diagnostics and error reporting

⚠️ **Known Limitations**:
- Nested groups (groups within groups) are not yet supported
- Extended varData types (VarString16, VarString32) not yet available
- Custom encoding/decoding hooks not yet available

See [SBE_FEATURE_COMPLETENESS.md](./docs/SBE_FEATURE_COMPLETENESS.md) for detailed feature status.

## Quick Start

### 1. Install

Install the NuGet package:

```bash
dotnet add package SbeSourceGenerator
```

Or add it directly to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="SbeSourceGenerator" Version="0.4.0" />
</ItemGroup>
```

> **For local development**, use a project reference instead:
> ```xml
> <ItemGroup>
>   <ProjectReference Include="..\SbeCodeGenerator\SbeSourceGenerator.csproj" 
>                     OutputItemType="Analyzer" 
>                     ReferenceOutputAssembly="false" />
> </ItemGroup>
> ```

### 2. Add Your Schema

Add your SBE XML schema as an additional file:

```xml
<ItemGroup>
  <AdditionalFiles Include="your-schema.xml" />
</ItemGroup>
```

### 3. Build

Build your project. The generator will automatically create C# types from your schema.

### 4. Use Generated Code

**Simple Messages:**
```csharp
// Create and encode a simple message
var trade = new TradeData
{
    TradeId = 123456,
    Price = 9950,
    Quantity = 100,
    Side = Side.Buy
};

// Encode to binary format
byte[] buffer = new byte[TradeData.MESSAGE_SIZE];
if (trade.TryEncode(buffer, out int bytesWritten))
{
    // Send via network
    await socket.SendAsync(buffer.AsMemory(0, bytesWritten));
}

// Decode from binary format
if (TradeData.TryParse(receivedBuffer, out var decoded, out _))
{
    Console.WriteLine($"Trade: {decoded.TradeId}, Price: {decoded.Price}");
}
```

**Messages with Repeating Groups (Span-Based API):**
```csharp
// Create message with groups
var orderBook = new OrderBookData { InstrumentId = 42 };
var bids = new[] {
    new BidsData { Price = 1000, Quantity = 100 },
    new BidsData { Price = 1010, Quantity = 101 }
};
var asks = new[] {
    new AsksData { Price = 2000, Quantity = 200 }
};

// Encode with comprehensive TryEncode - enforces correct schema order
Span<byte> buffer = stackalloc byte[1024];
bool success = OrderBookData.TryEncode(
    orderBook,
    buffer,
    bids,  // Groups/varData in schema-defined order
    asks,  // Compiler ensures correct parameter order
    out int bytesWritten
);

// Decode with groups
OrderBookData.TryParse(buffer, out var decoded, out var variableData);
decoded.ConsumeVariableLengthSegments(
    variableData,
    bid => Console.WriteLine($"Bid: {bid.Price}"),
    ask => Console.WriteLine($"Ask: {ask.Price}")
);
```

**Messages with Repeating Groups (Zero-Allocation Callback API):**
```csharp
// For high-performance scenarios: use callbacks to avoid array allocations
var orderBook = new OrderBookData { InstrumentId = 42 };

Span<byte> buffer = stackalloc byte[1024];
bool success = OrderBookData.TryEncode(
    orderBook,
    buffer,
    bidCount: 3,
    bidsEncoder: (int index, ref BidsData item) => {
        // Populate item from your data source without allocations
        item.Price = GetBidPrice(index);
        item.Quantity = GetBidQuantity(index);
    },
    askCount: 2,
    asksEncoder: (int index, ref AsksData item) => {
        item.Price = GetAskPrice(index);
        item.Quantity = GetAskQuantity(index);
    },
    out int bytesWritten
);
```

**Messages with Variable-Length Data (Span-Based API):**
```csharp
// Encode message with varData
var order = new NewOrderData { OrderId = 123, Price = 9950 };
var symbolBytes = Encoding.UTF8.GetBytes("AAPL");

Span<byte> buffer = stackalloc byte[512];
bool success = NewOrderData.TryEncode(
    order,
    buffer,
    symbolBytes,  // VarData in schema-defined order
    out int bytesWritten
);

// Decode varData
NewOrderData.TryParse(buffer, out var decoded, out var variableData);
decoded.ConsumeVariableLengthSegments(
    variableData,
    symbol => {
        var text = Encoding.UTF8.GetString(symbol.VarData.Slice(0, symbol.Length));
        Console.WriteLine($"Symbol: {text}");
    }
);
```

## Example Schema

```xml
<?xml version="1.0" encoding="UTF-8"?>
<sbe:messageSchema xmlns:sbe="http://fixprotocol.io/2016/sbe"
                   package="trading_messages"
                   id="1"
                   version="0"
                   semanticVersion="1.0"
                   description="Trading messages"
                   byteOrder="littleEndian">
    <types>
        <composite name="MessageHeader">
            <type name="blockLength" primitiveType="uint16"/>
            <type name="templateId" primitiveType="uint16"/>
            <type name="schemaId" primitiveType="uint16"/>
            <type name="version" primitiveType="uint16"/>
        </composite>

        <type name="TradeId" primitiveType="int64"/>
        <type name="Price" primitiveType="int64"/>

        <enum name="Side" encodingType="uint8">
            <validValue name="Buy">0</validValue>
            <validValue name="Sell">1</validValue>
        </enum>
    </types>

    <sbe:message name="Trade" id="1" description="Trade message">
        <field name="tradeId" id="1" type="TradeId"/>
        <field name="price" id="2" type="Price"/>
        <field name="quantity" id="3" type="int64"/>
        <field name="side" id="4" type="Side"/>
    </sbe:message>
</sbe:messageSchema>
```

## Generated Code

The generator creates:

- **Enums**: C# enums for SBE enum types
- **Sets**: C# flag enums for SBE set types
- **Composites**: C# structs for composite types
- **Types**: Type aliases and wrappers
- **Messages**: C# structs for SBE messages
- **Groups**: Nested structs for repeating groups

All generated types use `[StructLayout(LayoutKind.Explicit)]` with `[FieldOffset]` attributes for efficient binary serialization.

## Architecture

```
SBESourceGenerator (Orchestrator)
    │
    ├── TypesCodeGenerator
    │   ├── Enums
    │   ├── Sets
    │   ├── Composites
    │   └── Types
    │
    ├── MessagesCodeGenerator
    │   └── Messages with Groups & parsing helpers
    │
    └── UtilitiesCodeGenerator
        └── Helper Extensions
```

See [ARCHITECTURE_DIAGRAMS.md](./docs/ARCHITECTURE_DIAGRAMS.md) for detailed architecture diagrams.

## Diagnostics

The generator provides comprehensive diagnostics:

| Code | Severity | Description |
|------|----------|-------------|
| SBE001 | Error | Invalid integer attribute value |
| SBE002 | Error | Missing required attribute |
| SBE003 | Error | Invalid enum flag value |
| SBE004 | Error | Malformed schema |
| SBE005 | Warning | Unsupported construct |
| SBE006 | Error | Invalid type length |
| SBE007 | Warning | Non-native byte order |
| SBE008 | Error | Unresolved type reference |
| SBE009 | Warning | Invalid numeric constraint |
| SBE010 | Warning | Unknown primitive type fallback |

See [Diagnostics README](./src/SbeCodeGenerator/Diagnostics/README.md) for details.

## Testing

### Run Unit Tests

```bash
dotnet test SbeCodeGenerator.Tests
```

### Run Integration Tests

```bash
dotnet test SbeCodeGenerator.IntegrationTests
```

### All Tests

```bash
dotnet test
```

**Current Status**: Unit and integration tests passing ✅ (see CI badge above)

See [TESTING_GUIDE.md](./docs/TESTING_GUIDE.md) for testing guidelines.

## Documentation

- **[CI/CD Pipeline](./docs/CICD_PIPELINE.md)** - CI/CD configuration and NuGet publishing
- **[SBE Feature Completeness](./docs/SBE_FEATURE_COMPLETENESS.md)** - Detailed feature implementation status
- **[SBE Generators Comparison](./docs/SBE_GENERATORS_COMPARISON.md)** - Competitive analysis vs other SBE generators
- **[Implementation Roadmap](./docs/SBE_IMPLEMENTATION_ROADMAP.md)** - Future development plans
- **[Architecture Diagrams](./docs/ARCHITECTURE_DIAGRAMS.md)** - System architecture
- **[Testing Guide](./docs/TESTING_GUIDE.md)** - How to test the generator
- **[Schema DTOs Documentation](./src/SbeCodeGenerator/Schema/README.md)** - Schema parsing infrastructure
- **[Changelog](./CHANGELOG.md)** - Version history and release notes

## Project Structure

```
SbeSourceGenerator/
├── src/
│   └── SbeCodeGenerator/          # Source generator implementation
│       ├── Diagnostics/          # Diagnostic descriptors
│       ├── Generators/           # Code generators
│       │   ├── Fields/          # Field generators
│       │   └── Types/           # Type generators
│       ├── Helpers/             # Helper utilities
│       └── Schema/              # DTOs and parsing
├── tests/
│   ├── SbeCodeGenerator.Tests/       # Unit tests
│   └── SbeCodeGenerator.IntegrationTests/  # Integration tests
├── examples/                     # Example applications
│   ├── SbeBinanceConsole/       # Binance market data processing
│   └── HighPerformanceMarketData/ # High-performance SBE patterns
├── benchmarks/                   # BenchmarkDotNet performance benchmarks
├── profiling/                    # Profiling tools and scripts
└── docs/                        # Documentation files
```

## Examples

The repository includes several example projects in the `examples/` folder:

1. **SbeBinanceConsole** - Binance market data processing with live dashboard
2. **HighPerformanceMarketData** - High-performance SBE message processing patterns

## Performance

The generated code is designed for high performance:

- Zero-copy deserialization where possible
- Struct-based value types (no heap allocations)
- Explicit memory layout for cache efficiency
- Blittable types for P/Invoke scenarios

**Benchmark Infrastructure**: Comprehensive benchmarks using BenchmarkDotNet are available in the `benchmarks/` directory.

See [Performance Tuning Guide](./docs/PERFORMANCE_TUNING_GUIDE.md) for optimization best practices.

## Compliance

The generator implements the core SBE 1.0 features needed for most use cases. Known spec gaps (nested groups, nested composites, `<ref>` in composites, custom `headerType`) are tracked as [GitHub issues](https://github.com/pedrosakuma/SbeSourceGenerator/labels/spec-compliance).

See [Feature Completeness](./docs/SBE_FEATURE_COMPLETENESS.md) for the full compliance matrix.

## Contributing

Contributions are welcome! Priority areas:

**High Impact, Easy**:
- Add more tests and example schemas
- Improve documentation

**High Impact, Medium**:
- Nested groups (groups within groups)
- Extended varData types (VarString16, VarString32)
- Custom encoding/decoding hooks

See [Implementation Roadmap](./docs/SBE_IMPLEMENTATION_ROADMAP.md) for more opportunities.

### Development Setup

1. Clone the repository
2. Open `SbeCodeGenerator.sln` in Visual Studio 2022+ or VS Code
3. Build the solution: `dotnet build`
4. Run tests: `dotnet test`

## Requirements

- .NET SDK 9.0+ (for building examples and tests)
- The generator itself targets **netstandard2.0** and works with any compatible runtime
- Roslyn Source Generators support (Visual Studio 2022+, .NET SDK 6.0+)

## References

- [FIX Simple Binary Encoding (SBE) Standard](https://www.fixtrading.org/standards/sbe/)
- [SBE GitHub Repository](https://github.com/FIXTradingCommunity/fix-simple-binary-encoding)
- [Real Logic SBE Implementation](https://github.com/real-logic/simple-binary-encoding)

## License

See [LICENSE.txt](./LICENSE.txt) for license information.

## Support

For questions, issues, or feature requests:
- Open an issue on GitHub
- Check existing documentation
- Review example projects

---

**Status**: Active Development  
**Version**: 0.4.0 (see [Changelog](./CHANGELOG.md))
