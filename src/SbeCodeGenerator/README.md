# SBE Source Generator

`SbeSourceGenerator` is a Roslyn incremental source generator that converts Simple Binary Encoding (SBE) XML schemas into strongly typed C# data structures, enums and message parsers. It enables consuming real-time market data feeds without hand-writing SBE decoding code.

## Getting Started

1. Install the package:
   ```bash
   dotnet add package SbeSourceGenerator --version 0.1.0-preview.1
   ```

2. Add SBE schema files (`*.xml`) to your project as `AdditionalFiles`:
   ```xml
   <ItemGroup>
     <AdditionalFiles Include="Schemas\*.xml" />
   </ItemGroup>
   ```

3. Build the project. The generator creates classes under the namespace inferred from the schema filename. Generated code is accessible at compile time and emitted into the `obj` folder.

## Features

- Generates blittable structs for SBE composites, messages and groups.
- Produces helper types for enums, sets, optional values and decimal conversions.
- Provides `TryParse` helpers on messages and composites to simplify decoding flows without unsafe casts.
- Includes diagnostics (`SBE00x`) to highlight malformed schemas or unsupported constructs.

## Requirements

- .NET 6.0 SDK or newer for consumer projects.
- SBE XML schema compliant with FIX Simple Binary Encoding 1.0 namespace (`http://fixprotocol.io/2016/sbe`).

## Diagnostics

The generator surfaces Roslyn diagnostics prefixed with `SBE`. Refer to `AnalyzerReleases.Shipped.md` for current rule set and severities.

## Contributing

Issues and pull requests are welcome at [github.com/pedrosakuma/PcapSbePocConsole](https://github.com/pedrosakuma/PcapSbePocConsole).

## License

Distributed under the MIT License. See the `LICENSE.txt` file for more information.
