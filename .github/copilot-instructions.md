# Copilot Instructions for SbeSourceGenerator

## Build, Test, Lint

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run only unit tests
dotnet test tests/SbeCodeGenerator.Tests/

# Run only integration tests
dotnet test tests/SbeCodeGenerator.IntegrationTests/

# Run a single test by name
dotnet test --filter "FullyQualifiedName~TypesCodeGeneratorTests.Generate_WithSimpleEnum_ProducesEnumCode"

# Run a test class
dotnet test --filter "FullyQualifiedName~TypesCodeGeneratorTests"
```

There is no separate linter — the build enforces `EnforceExtendedAnalyzerRules`.

## Architecture

This is a **Roslyn incremental source generator** (`IIncrementalGenerator`) that converts FIX SBE XML schemas into C# structs with explicit memory layout. The generator targets **netstandard2.0** (required for Roslyn analyzers) while tests and examples target **net9.0**.

### Generation Pipeline

```
XML schema (*.xml via AdditionalFiles)
  → SBESourceGenerator (entry point, namespace derivation, SchemaContext creation)
    → TypesCodeGenerator   (enums, types, composites, sets, derived constants on decimal composites)
    → MessagesCodeGenerator (messages, fields, groups, varData, per-message {Msg}VersionMap when multi-version)
    → DispatcherGenerator   (per-schema ISbeMessageHandler + zero-cost SbeDispatcher.Dispatch<T>)
    → UtilitiesCodeGenerator (SpanReader, SpanWriter, endian helpers)
    → ValidationGenerator   (optional validation)
  → sourceContext.AddSource() per generated file
```

Each generator implements `ICodeGenerator` and returns `IEnumerable<(string name, string content)>`.

### Key Abstractions

- **`SchemaContext`** — Per-schema mutable state container. Tracks type names, lengths, enum primitive types, optional types, composite field types, and byte order. Passed through the entire pipeline. Not global — one instance per schema file.
- **`IFileContentGenerator`** — Implemented by all definition types (`EnumDefinition`, `CompositeDefinition`, `MessageDefinition`, field definitions). Each renders itself to a `StringBuilder` via `AppendFileContent(sb, tabs)`.
- **`IBlittable` / `IBlittableMessageField`** — Track byte sizes and field offsets for `[StructLayout(LayoutKind.Explicit)]` struct generation. Offsets are auto-calculated sequentially in `FileContentGeneratorExtensions.SumFieldLength()`.
- **Schema DTOs** (`SchemaFieldDto`, `SchemaMessageDto`, etc.) — Immutable `internal record` types parsed from XML by `SchemaReader`. Produced in a single forward-only pass.
- **`TypeTranslator`** — Maps SBE primitive types to C# types (e.g., `int8` → `sbyte`, `uint16` → `ushort`) and resolves null sentinel values for optional fields.

### Schema → Namespace Derivation

The generated namespace comes from the schema's `package` attribute (dot-separated, PascalCased), falling back to the file path. A version suffix (`.V{version}`) is appended when the schema specifies one.

## Conventions

### Naming

- **Generators**: `*CodeGenerator.cs` (implement `ICodeGenerator`)
- **Definition builders**: `*Definition.cs` (implement `IFileContentGenerator`)
- **Schema DTOs**: `Schema*Dto.cs` (immutable `internal record`)
- **Tests**: `*Tests.cs`, method naming: `Method_Scenario_ExpectedBehavior`
- **Generated C# identifiers**: PascalCase for types/properties, `SCREAMING_SNAKE_CASE` for constants (`MESSAGE_SIZE`, `BLOCK_LENGTH`)

### Code Generation Style

Generated C# code is built via `StringBuilder` with tab-aware helpers from `StringBuilderExtensions`. The `tabs` parameter controls indentation depth — use `tabs++` / `--tabs` around braces.

### Type Registration Pattern

When adding a new type to the generator, always register it in `SchemaContext`:
```csharp
context.GeneratedTypeNames[originalName] = generatedName;
context.CustomTypeLengths[originalName] = length;
```

### Testing

- **Unit tests** call generators directly with inline XML schemas, assert on generated string content.
- **Snapshot tests** use **Verify.Xunit** — golden files live in `tests/SbeCodeGenerator.Tests/Snapshots/` as `*.verified.txt`. When generation output changes intentionally, copy `.received.txt` to `.verified.txt`.
- **Integration tests** reference the generator as an analyzer, include real XML schemas via `<AdditionalFiles>`, and validate that generated types compile and work correctly (including `unsafe` struct layout and Span-based encoding/decoding).

### Diagnostics

Custom diagnostics use IDs `SBE001`–`SBE011` defined in `SbeDiagnostics.cs`. New diagnostics should follow the same pattern and be added to `AnalyzerReleases.Unshipped.md`.

### Adding a New Schema Construct

1. Create a DTO record in `Schema/` (e.g., `SchemaXyzDto.cs`)
2. Add parsing logic in `SchemaReader` (`ReadXyz()` method)
3. Create a definition builder in `Generators/Types/` or `Generators/Fields/` implementing `IFileContentGenerator` (and `IBlittable` if fixed-size)
4. Wire it into the appropriate generator (`TypesCodeGenerator` or `MessagesCodeGenerator`)
5. Add unit tests with inline XML and snapshot tests for regression

### Documentation

When making changes that affect user-facing behavior, update the relevant documentation:

- **README.md** — Feature list, usage examples, diagnostics table, version references
- **CHANGELOG.md** — Add entry under appropriate section (Added/Changed/Removed/Fixed)
- **docs/** — Update relevant feature docs if the change modifies existing behavior
- **src/SbeCodeGenerator/Diagnostics/README.md** — When adding or modifying diagnostics
- **.github/copilot-instructions.md** — When adding new abstractions, conventions, or changing the pipeline
