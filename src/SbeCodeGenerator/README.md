# SBE Source Generator

[![NuGet](https://img.shields.io/nuget/v/SbeSourceGenerator.svg)](https://www.nuget.org/packages/SbeSourceGenerator/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

A Roslyn incremental source generator that converts [FIX Simple Binary Encoding](https://github.com/FIXTradingCommunity/fix-simple-binary-encoding) (SBE) XML schemas into high-performance, zero-allocation C# structs. Ideal for real-time market data, trading systems, and any latency-sensitive binary protocol.

## Getting Started

1. Install the package:
   ```bash
   dotnet add package SbeSourceGenerator
   ```

2. Add your SBE schema files as `AdditionalFiles`:
   ```xml
   <ItemGroup>
     <AdditionalFiles Include="Schemas\*.xml" />
   </ItemGroup>
   ```

3. Build. Generated code is available at compile time — no runtime reflection, no code-behind files.

## Quick Example

```csharp
// Decode
if (TradeData.TryParse(buffer, out var trade, out var variableData))
{
    Console.WriteLine($"Trade {trade.TradeId} @ {trade.Price}");
}

// Encode (fluent API)
var bytes = trade.CreateEncoder(buffer)
    .WithLegs(legs)
    .BytesWritten;
```

## Features

- **Zero-copy blittable structs** — `[StructLayout(Explicit)]` with `[FieldOffset]`, directly overlay on buffers
- **`TryParse` / `TryEncode`** — safe decode/encode without unsafe casts
- **Zero-copy decode** — `TryReadBlock<T>` and `ReadBlockRef<T>` read directly from spans
- **`in` delegate callbacks** — group iteration passes structs by readonly reference (no copies)
- **Fluent encoder API** — type-safe `CreateEncoder().WithGroups().WithVarData()` chaining
- **Schema evolution** — `TryReadBlock` handles `blockLength` mismatches (forward/backward compat)
- **Big-endian support** — automatic byte swapping for big-endian schemas
- **Enums, sets, composites** — full SBE type system with optional field support
- **Nested groups** — recursive group generation to any depth
- **SpanReader / SpanWriter** — embedded sequential binary reader/writer (no manual offset tracking)
- **Cross-schema coexistence** — multiple schemas generate isolated namespaces, no conflicts
- **AOT / trimming compatible** — no reflection, no dynamic code generation
- **Roslyn diagnostics** — `SBE001`–`SBE006` for schema validation at build time

## Requirements

- **.NET 6.0+** for consumer projects (generator itself targets netstandard2.0)
- SBE XML schema using the FIX SBE 1.0 namespace (`http://fixprotocol.io/2016/sbe`)

## Performance

All generated code targets zero allocations on hot paths:
- Structs are stack-allocated value types
- `readonly` property getters prevent defensive copies through `in`/`ref readonly`
- Group callbacks use `in` delegates — no boxing, no struct copies
- SpanReader/SpanWriter are `ref struct` with `[MethodImpl(AggressiveInlining)]`

## Documentation

Full documentation at [github.com/pedrosakuma/SbeSourceGenerator](https://github.com/pedrosakuma/SbeSourceGenerator):

- [Schema Versioning Guide](https://github.com/pedrosakuma/SbeSourceGenerator/blob/main/docs/SCHEMA_VERSIONING.md)
- [SpanReader API](https://github.com/pedrosakuma/SbeSourceGenerator/blob/main/docs/SPAN_READER_README.md)
- [Fluent Encoder API](https://github.com/pedrosakuma/SbeSourceGenerator/blob/main/docs/FLUENT_ENCODER_API.md)
- [Performance Tuning](https://github.com/pedrosakuma/SbeSourceGenerator/blob/main/docs/PERFORMANCE_TUNING_GUIDE.md)

## Contributing

Issues and pull requests are welcome at [github.com/pedrosakuma/SbeSourceGenerator](https://github.com/pedrosakuma/SbeSourceGenerator).

## License

Distributed under the MIT License. See `LICENSE.txt` for details.
