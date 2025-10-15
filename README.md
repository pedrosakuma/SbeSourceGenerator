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

⚠️ **Partially Implemented**:
- Schema versioning (metadata parsed, evolution not fully implemented)
- Deprecated field handling (parsed but not marked in code)

📋 **Planned Features**:
- Nested groups (groups within groups)
- Extended varData types (VarString16, VarString32)
- Schema evolution with sinceVersion
- Custom encoding/decoding hooks

See [SBE_FEATURE_COMPLETENESS.md](./docs/SBE_FEATURE_COMPLETENESS.md) for detailed feature status.

## Quick Start

### 1. Install

Add the SBE source generator to your project:

```xml
<ItemGroup>
  <ProjectReference Include="..\SbeCodeGenerator\SbeSourceGenerator.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

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

**Messages with Repeating Groups:**
```csharp
// Create message with groups
var orderBook = new OrderBookData { InstrumentId = 42 };
var bids = new[] {
    new BidsData { Price = 1000, Quantity = 100 },
    new BidsData { Price = 1010, Quantity = 101 }
};

// Encode with groups
Span<byte> buffer = stackalloc byte[1024];
orderBook.BeginEncoding(buffer, out var writer);
OrderBookData.TryEncodeBids(ref writer, bids);

// Decode with groups
OrderBookData.TryParse(buffer, out var decoded, out var variableData);
decoded.ConsumeVariableLengthSegments(
    variableData,
    bid => Console.WriteLine($"Bid: {bid.Price}"),
    ask => Console.WriteLine($"Ask: {ask.Price}")
);
```

**Messages with Variable-Length Data:**
```csharp
// Encode message with varData
var order = new NewOrderData { OrderId = 123, Price = 9950 };
var symbolBytes = Encoding.UTF8.GetBytes("AAPL");

Span<byte> buffer = stackalloc byte[512];
order.BeginEncoding(buffer, out var writer);
NewOrderData.TryEncodeSymbol(ref writer, symbolBytes);

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

See [ARCHITECTURE_DIAGRAMS.md](./ARCHITECTURE_DIAGRAMS.md) for detailed architecture diagrams.

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

**Current Status**: 214 tests (105 unit + 109 integration), all passing ✅

See [TESTING_GUIDE.md](./docs/TESTING_GUIDE.md) for testing guidelines.

## Documentation

- **[CI/CD Pipeline](./docs/CICD_PIPELINE.md)** - CI/CD configuration and NuGet publishing
- **[SBE Feature Completeness](./docs/SBE_FEATURE_COMPLETENESS.md)** - Detailed feature implementation status
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
│   ├── PcapSbePocConsole/       # Basic SBE encoding/decoding
│   ├── PcapMarketReplayConsole/ # Market data replay
│   └── SbeBinanceConsole/       # Binance market data processing
└── docs/                        # Documentation files
```

## Examples

The repository includes several example projects in the `examples/` folder:

1. **PcapSbePocConsole** - Basic SBE encoding/decoding with B3 market data
2. **PcapMarketReplayConsole** - PCAP-based market data replay
3. **SbeBinanceConsole** - Binance market data processing

## Performance

The generated code is designed for high performance:

- Zero-copy deserialization where possible
- Struct-based value types (no heap allocations)
- Explicit memory layout for cache efficiency
- Blittable types for P/Invoke scenarios

**Benchmark Infrastructure**: Comprehensive benchmarks using BenchmarkDotNet are available in the `benchmarks/` directory.

See [Performance Tuning Guide](./docs/PERFORMANCE_TUNING_GUIDE.md) for optimization best practices and [Benchmark Results](./docs/BENCHMARK_RESULTS.md) for detailed performance analysis.

## Compliance

**SBE 1.0 Specification Compliance**: ~70-75%

The generator implements the core SBE features needed for most use cases. See the [Feature Completeness](./docs/SBE_FEATURE_COMPLETENESS.md) document for what's currently supported and what's planned.

## Contributing

Contributions are welcome! Priority areas:

**High Impact, Easy**:
- Add more tests
- Improve documentation
- Add example schemas

**High Impact, Medium**:
- Implement variable-length data support
- Add validation constraints
- Improve diagnostics

See [Implementation Roadmap](./SBE_IMPLEMENTATION_ROADMAP.md) for more opportunities.

### Development Setup

1. Clone the repository
2. Open `SbeCodeGenerator.sln` in Visual Studio 2022+ or VS Code
3. Build the solution: `dotnet build`
4. Run tests: `dotnet test`

## Requirements

- .NET 9.0 SDK or later
- C# 12.0 language features
- Roslyn Source Generators support

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
**Version**: Pre-1.0 (see roadmap for 1.0 timeline)  
**Last Updated**: 2025-10-15
