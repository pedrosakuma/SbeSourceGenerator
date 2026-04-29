# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Direct properties for top-level `<data>` (varData) on `{Msg}DataReader`** (#162): Each top-level varData field now also exposes a property returning the variable-length composite (e.g. `VarStringEncoding`) computed directly from the buffer. Zero allocation, no callback, no closure capture:
  ```csharp
  if (NewOrderData.TryParse(buffer, out var reader))
  {
      foreach (ref readonly var leg in reader.Legs) { /* ... */ }
      var coid = reader.ClientOrderId.VarData; // ReadOnlySpan<byte>, zero-alloc
  }
  ```
  Each access is **stateless** — the start offset is recomputed from the buffer by chaining the existing `Skip{Group}` helpers (O(1) per group) plus `SkipData{Prev}` for any prior varData. Safe regardless of access order, before/after iterating groups, and across multiple reads. For repeated reads, cache the returned value locally.

  **Gating**: emitted only when *all* top-level groups are simple (no nested groups, no group-level varData) — same gate as the foreach enumerators added in #156. Also valid for messages with only varData and no groups. When a varData name collides with a reserved member of the reader struct (e.g. `<data name="data">` clashing with the `Data` block accessor), the direct property is silently skipped for that field; it remains reachable via `ReadGroups`. `ReadGroups` continues to work unchanged for all cases.

## [1.5.0] - 2026-04-25

### Added — DevEx P0 features

- **Foreach-style enumerators on `{Msg}DataReader` for top-level groups** (#156): Each top-level group now also exposes a property returning a `ref struct` enumerator usable with `foreach`. Zero allocation, no closure capture, `ref readonly Current` for zero-copy access:
  ```csharp
  if (OrderBookData.TryParse(buffer, out var reader))
  {
      foreach (ref readonly var bid in reader.Bids) { /* ... */ }
      foreach (ref readonly var ask in reader.Asks) { /* ... */ }
  }
  ```
  The enumerators are **independent per-group views** — each access computes its own start offset by skipping prior groups, so out-of-order access (`Asks` before `Bids`), early `break`, and repeated iteration are all safe and don't corrupt one another. `ReadGroups` continues to work unchanged (idempotent) for messages that need group-level varData or nested groups.

  **Gating**: emitted only when *all* top-level groups are simple (no nested groups, no group-level varData). Complex messages keep `ReadGroups` only.

### Performance
- The new enumerator API is allocation-free and aggressively inlined. `MoveNext` is a small loop with a single dimension read; `Current` is a `ref readonly` into the original buffer. Designed to JIT down to the same shape as a hand-written for-loop over `MemoryMarshal.AsRef`.

## [1.4.0] - 2026-04-25

### Added — DevEx P0 features

- **`AsTrimmedSpan()` on InlineArray char types** (#155): Like `AsSpan()` but additionally strips trailing space (0x20) padding — matches the behavior of `ToString().Trim()` without the two allocations. Lets hot-path consumers compare/log FIX-style space-padded fields (`Symbol`, `Currency`, `IsinNumber`, …) zero-alloc.
- **`ISpanFormattable` on InlineArray char types and decimal composites** (#153): Generated char and decimal-composite types now implement `ISpanFormattable` — `TryFormat(Span<char>, out int, ReadOnlySpan<char>, IFormatProvider?)` plus `ToString(string?, IFormatProvider?)`. Char types delegate to the schema's `characterEncoding` and `AsTrimmedSpan()`. Decimal composites use the schema's `Decimals` constant for the default format (e.g. `F4` / `F8`), so `string.Create(...)` and `Utf8.TryWrite(...)` emit correctly-scaled strings without allocating.
- **Static header read helpers on the message header composite** (#156): The composite designated by `headerType` (default `messageHeader`) now exposes `TryReadTemplateId(ReadOnlySpan<byte>, out ushort)` and `TryReadHeader(ReadOnlySpan<byte>, out blockLength, out templateId, out schemaId, out version)`. Single source of truth for header layout — consumers no longer hardcode `SbeHeaderSize = 8` or compute offsets manually.

### Performance
- All three additions are emit-time only with no allocation on the new code paths. `AsTrimmedSpan()` and `TryReadTemplateId` are aggressively inlined.

## [1.3.0] - 2026-04-25

### Added
- **Raw buffer access on `{Message}DataReader`** (#151): The generated zero-copy reader now exposes `Buffer` (full source `ReadOnlySpan<byte>`), `Block` (slice sized to the wire `blockLength`), and `BlockLength` as `readonly` properties. Lets dispatcher-based consumers forward or buffer the original bytes (e.g. snapshot-heal flows) without re-parsing or maintaining a parallel manual switch just to capture the body. The data was already in the reader's private `_buffer` field; this only adds public accessors — no codegen size or runtime cost change.

## [1.2.1] - 2026-04-25

### Fixed
- **Unsuffixed `{Message}Data` alias collapsed to baseline V0 instead of widest version** (#149): The `effectiveVersion` widening logic introduced in v1.1.4 (#143) was inadvertently dropped while wiring the per-version blockLength compute for the new `{Msg}VersionMap` (#146). Restored: the unsuffixed canonical struct again includes every field added across the schema's version history (matches the natural reader expectation and v1.1.4 behavior). The per-version `{Msg}V{N}Data` aliases and the `{Msg}VersionMap` entries continue to use each version's own blockLength.

## [1.2.0] - 2026-04-25

### Added
- **Inlinable extension methods for `[Flags]` set enums** (#144): Generated set types now ship a sibling `{Set}Extensions` static class with `Has(flag)` plus per-choice `Is{Flag}()` predicates, all marked `[MethodImpl(AggressiveInlining)]`. Lets call sites read like `flags.IsOddLot()` without losing the `(v & X) != 0` codegen.
- **Derived numeric constants for primitive type wrappers and decimal composites** (#145): Wrapped primitive types now expose `MinValue` / `MaxValue` constants when declared on the schema. Decimal composites (mantissa + constant exponent) additionally expose `Decimals`, `Multiplier` (double), `MultiplierDecimal`, and `Divisor` (long) — all `const`, single source of truth for formatting and scaling.
- **Per-message `{Msg}VersionMap`** (#146): Messages with multiple versions now generate a static class mapping each version's wire `blockLength` to its version number via a small `(int BlockLength, int Version)[]` array and an aggressively-inlined `TryGetVersion`.
- **Zero-cost `SbeDispatcher`** (#147): Each schema generates an `ISbeMessageHandler` interface (one `On{Msg}` method per template) and a `SbeDispatcher.Dispatch<T>(buffer, ref handler) where T : struct, ISbeMessageHandler` static method. The struct constraint lets the JIT specialize and devirtualize, so dispatch compiles down to a header-parse plus a `switch` on `templateId`.

### Performance
- All four additions are emit-time only and do not alter generated hot-path code; encode / decode / round-trip benchmarks remain within noise of the prior release.

### Changed
- **Generated XML doc summaries no longer include the generator class origin**: removed the trailing `/// (TypeDefinition)` / `/// (CompositeDefinition)` / etc. lines that leaked the source generator's internal class names into user-facing IntelliSense.

## [1.1.2] - 2026-04-15

### Fixed
- **Remaining CS8656 on optional type Value property** (#139): Added `readonly` modifier to `Value` getter in `OptionalTypeDefinition`, fixing 4 remaining CS8656 errors on `LocalMktDateOptional` and `LocalMktDate32Optional`.

### Changed
- **Integration tests now treat warnings as errors**: Added `TreatWarningsAsErrors` to the integration test project to catch generated code warnings (like CS8656) as build failures, preventing future regressions.

## [1.1.1] - 2025-07-25

### Fixed
- **CS8656 regression with composite struct fields** (#139): Composite-type fields (e.g., `Price`, `Engine`, `Instrument`) now use value-returning `readonly get; set;` properties instead of `ref`-returning properties. This fixes 337 CS8656 compiler errors when `readonly` methods (like `ToString`) accessed mutable ref-returning properties. Sub-field mutation (`msg.Engine.Capacity = 5`) must now use whole-struct assignment (`msg.Engine = new Engine { Capacity = 5 }`).

## [1.1.0] - 2026-04-15

### Added
- **`ToDecimal()` on decimal composites** (#131): Composites with the SBE decimal pattern (mantissa + constant exponent) now generate a `ToDecimal()` method. Optional mantissa returns `decimal?`, non-optional returns `decimal`. Uses pre-computed decimal literal (e.g., `1e-4m`) for zero-overhead conversion.
- **`ToDateTime()` / `ToDateTimeOffset()` on timestamp composites** (#132): Composites with the SBE timestamp pattern (time + constant unit) now generate conversion methods. Supports nanosecond, microsecond, millisecond, and second time units. Optional time returns nullable types.
- **`ToDateOnly()` on LocalMktDate and MonthYear** (#133): Types with `semanticType="LocalMktDate"` generate `ToDateOnly()` (days since epoch). Composites with `semanticType="MonthYear"` generate `ToDateOnly()` using year/month/day fields.
- **`AsSpan()` and `Equals(ReadOnlySpan<byte>)` on InlineArray char types** (#134): Zero-allocation content access and comparison. `AsSpan()` returns trimmed content excluding null-termination. `Equals()` enables comparison with UTF-8 string literals (e.g., `symbol.Equals("PETR4"u8)`).
- **Named null sentinel constants** (#136): Optional fields now expose a named `{FieldName}NullValue` constant (e.g., `MantissaNullValue`, `TimeNullValue`) instead of inline magic numbers.

### Changed
- **Field visibility standardization** (#137): All struct fields now use `private` backing fields with `public` property accessors (`readonly get` + `set`).
- **`readonly` methods on generated structs** (#135): `TryEncode`, `TryEncodeWithWriter`, `Encode`, `AsSpan`, `Equals`, and `ToString` are now `readonly` instance methods to prevent defensive copies. `ConstantTypeDefinition` emits `readonly struct`.

## [1.0.2] - 2026-04-10

### Fixed
- **CS8656 defensive copy elimination**: Added `readonly` modifier to nullable composite property getters (e.g., `Mantissa`, `Time`, `Year`, `Month`, `Day`, `Week`). Eliminates 11 CS8656 warnings across 8 composites (`RatioQty`, `Percentage`, `PriceOptional`, `UTCTimestampNanos`, `PriceOffset8Optional`, `UTCTimestampSeconds`, `Fixed8`, `MaturityMonthYear`) where the `readonly ToString()` called non-readonly property getters, causing silent defensive copies.

## [1.0.1] - 2026-04-10

### Fixed
- **B3 UMDF schema compatibility**: Full support for the B3 binary market data SBE schema (v2.2.0). The following issues are now resolved:
  - **DTD declarations**: Schemas with `<!DOCTYPE>` declarations (like B3 UMDF) no longer fail with SBE004. `DtdProcessing` changed from `Prohibit` to `Ignore`.
  - **Constant type fields**: Message fields referencing constant types (e.g., `SeqNum1` → `uint32` with value `1`) now resolve to the correct primitive type and value instead of generating empty/invalid code.
  - **Named optional types in messages**: Fields using named optional types (e.g., `RptSeq`, `FirmOptional`, `QuantityOptional`) now use the underlying primitive type directly, avoiding invalid struct-to-primitive casts.
  - **Composite optional fields**: Composite types (e.g., `PriceOptional`, `Fixed8`, `Percentage`) used as optional message fields now generate regular fields instead of attempting invalid `== default` comparisons on structs.
  - **Named type optional resolution**: Regular named types (e.g., `SettlType` → `uint16`) are now correctly resolved when used as optional message fields.
- **Group-level varData variable shadowing**: When a group and its parent message both have varData fields with the same name (e.g., `Symbol`), the generated code now uses unique variable names to avoid CS0136 errors.
- **Binance example**: Migrated `ConsoleApp.cs` from deprecated `ConsumeVariableLengthSegments` API to the v1.0.0 `TryParseWithReader`/`ReadGroups` API.

## [1.0.0] - 2026-04-10

### Breaking Changes
- **Zero-copy `MessageDataReader` API**: `TryParse` now returns a `{Message}DataReader` ref struct instead of `(out T message, out ReadOnlySpan<byte> variableData)`. Access fields via `reader.Data` (zero-copy `ref readonly` into the buffer). This is a breaking change for all decode call sites.
- **`ConsumeVariableLengthSegments` → `ReadGroups`**: Group/varData processing moved from instance method on the data struct to a method on the reader. No separate `variableData` span parameter — the reader manages the buffer internally.
- **`TryParseWithReader`**: Now returns a `{Message}DataReader` instead of `(out T, out ReadOnlySpan<byte>)`. Caller uses `spanReader.TrySkip(messageReader.BytesConsumed)` to advance past the message.

### Added
- **`{Message}DataReader` ref struct**: Generated per message. Holds `ReadOnlySpan<byte>` buffer, exposes `ref readonly {Message}Data Data` via `Unsafe.As<byte, T>` for zero-copy field access.
- **`BytesConsumed` property**: Tracks total bytes consumed by the message (block + groups + varData). Available after `ReadGroups` for messages with variable-length segments.
- **`ReadGroups` method**: Replaces `ConsumeVariableLengthSegments`. Only generated for messages with groups or varData. Same `in` delegate callback pattern.

### Migration Guide

```csharp
// Before (v0.9.x):
if (CarData.TryParse(buffer, out var car, out var variableData))
{
    Console.WriteLine(car.SerialNumber);
    car.ConsumeVariableLengthSegments(variableData,
        (in FuelFiguresData fuel) => { },
        (in PerformanceFiguresData perf) => { });
}

// After (v1.0.0):
if (CarData.TryParse(buffer, out var car))
{
    Console.WriteLine(car.Data.SerialNumber);  // .Data for zero-copy access
    car.ReadGroups(
        (in FuelFiguresData fuel) => { },
        (in PerformanceFiguresData perf) => { });
}

// Simple messages (no groups):
// Before: TradeData.TryParse(buffer, out var trade, out _);  trade.Price
// After:  TradeData.TryParse(buffer, out var trade);         trade.Data.Price
```

## [0.9.1] - 2026-04-10

### Fixed
- **NuGet packaging**: Removed DLL from `lib/netstandard2.0/` (was duplicated in both `lib/` and `analyzers/`). Consumers no longer get an incorrect runtime reference to the generator assembly.
- **Zero external dependencies**: Replaced `System.Text.Json` (608KB, used only for `JsonNamingPolicy.SnakeCaseUpper`) with inline `ToScreamingSnakeCase()` implementation. Package size reduced from ~240KB to 76KB.
- **Transitive reference support**: Added `buildTransitive/SbeSourceGenerator.props` so the generator loads correctly for transitive package references.
- **CS0105 warning**: Removed duplicate `using System.Runtime.InteropServices` in generated message code.
- **CS8656 warning**: Added `readonly` to composite big-endian property getters.
- **CS0618 warning**: Generated `ToString()` and optional property accessors now suppress obsolete-member warnings when accessing deprecated fields.

### Changed
- Renamed internal `ToKebabCase()` to `ToScreamingSnakeCase()` with improved algorithm handling acronyms, digits, and underscores correctly.
- Removed unused `deprecated-test-schema.xml` from integration tests (unit tests already cover deprecated attribute generation).

## [0.9.0] - 2026-04-10

### Added
- **Zero-copy decode with `ReadBlockRef<T>`**: New `SpanReader.ReadBlockRef<T>(int blockLength)` returns `ref readonly T` directly into the buffer, avoiding any struct copies. Uses `Unsafe.NullRef<T>()` sentinel for failure — callers check with `Unsafe.IsNullRef`.
- **`TryReadBlock<T>` with schema backward compatibility**: New `SpanReader.TryReadBlock<T>(int blockLength, out T value)` replaces `TryReadGroupEntry`. When `blockLength < sizeof(T)`, performs partial read with zero-padded trailing fields (schema evolution backward compat). When `blockLength > sizeof(T)`, reads the struct and skips extra bytes (forward compat).
- **`in` delegate callbacks for group consume** (**Breaking**): Group callbacks changed from `Action<GroupData>` to custom `{GroupName}Handler(in GroupData data)` delegates. This eliminates struct copies when passing group entries to callbacks. Nested groups include parent context: `AccelerationHandler(in PerformanceFiguresData parent, in AccelerationData data)`.
- **Cross-schema reference validation**: Integration tests confirming multiple SBE schemas coexist in the same project with isolated namespaces, no type conflicts, and independent encode/decode.

### Changed
- **Simplified TryParse/TryParseWithReader**: Replaced ~30 lines of manual sizeof/skip/evolution logic with single `TryReadBlock<T>(blockLength, out message)` call. TryReadBlock handles all schema evolution cases internally.
- Generated messages now include `using System.Runtime.InteropServices` for `MemoryMarshal` access.

### Fixed
- **Zero-field group consume**: Groups with `blockLength=0` (no fields) now decode correctly. Previously, `TryRead<T>` advanced by `Unsafe.SizeOf<T>()` (always ≥1 for empty structs) instead of the wire `blockLength`.
- **Deeply nested group struct generation**: Groups nested 3+ levels deep now correctly generate their data structs. Previously only 1 level of nesting was handled, causing missing types (e.g., Binance ExchangeInfoResponse Permissions).
- **Nested group variable shadowing**: Nested group consume code now uses unique variable names per depth level (`nestedData1`, `nestedData2`) to avoid C# variable shadowing.
- **Char array composites**: Fixed char array field generation in composites to use correct `InlineArray` attribute.
- **Set/enum naming normalization**: Consistent PascalCase naming for generated set and enum types.
- **Generation order**: Types are now generated before messages, ensuring `SchemaContext` type registrations are available during message generation.

## [0.8.0] - 2026-04-10

### Added
- **Big-endian byte order support**: Schemas with `byteOrder="bigEndian"` now generate correct endian-aware code. Multi-byte fields use property-based access with `BinaryPrimitives.ReverseEndianness()` conversions. Three generation modes: passthrough (same endianness), always-reverse (opposite), and conditional (safe default). Single-byte fields (byte, sbyte, char) are always passthrough.
- **`SbeAssumeHostEndianness` MSBuild property**: Optional compile-time hint to eliminate runtime endian branching. Set to `LittleEndian` or `BigEndian` in your `.csproj` to generate branchless code when you know the target host architecture.
- **Optional float/double fields**: Float and double fields with `presence="optional"` now use IEEE 754 NaN as the null sentinel, with `float.IsNaN()` / `double.IsNaN()` for null checks.
- **Field-level nullValue override**: The `nullValue` attribute on message fields is now respected, overriding the default type-catalog sentinel value.
- **SBE012 diagnostic**: Warning when `SbeAssumeHostEndianness` has an invalid value (not `LittleEndian` or `BigEndian`).
- **SBE011 diagnostic**: Warning when set choice bit positions exceed encoding type width.

### Changed
- **SBE007 severity**: Reduced from Warning to Info — big-endian schemas are now fully supported; the diagnostic is informational only.
- Removed obsolete `EndianHelpers` generated utility class (superseded by property-based endian conversion).
- Removed out-of-scope integration tests (`SpanReaderIntegrationTests`, `ProposedFeaturesTests`, `DeprecatedFieldsIntegrationTests`) that tested internal utilities rather than SBE schema generation.

## [0.7.0] - 2026-04-09

### Removed
- **Opinionated semantic type helpers**: Removed `DecimalSemanticTypeDefinition`, `LocalMktDateSemanticTypeDefinition`, `MonthYearSemanticTypeDefinition`, `UTCTimestampSemanticTypeDefinition`, and `NumberExtensions`. These were hardcoded to CME/B3 schema naming conventions, not driven by the SBE `semanticType` attribute. The generator now focuses on producing correct SBE structs; consumers can add extension methods for domain-specific conversions.

## [0.6.0] - 2026-04-09

### Changed
- **Ancestor context in nested group callbacks**: Nested group callbacks now include parent data as additional type parameters, providing full hierarchical context. For example, `Action<PerformanceFiguresData, AccelerationData>` instead of `Action<AccelerationData>`. Scales to any nesting depth with all ancestor types known at compile time.

## [0.5.0] - 2025-07-25

### Added
- **SBE011**: Diagnostic for set choice bit positions exceeding encoding type width (#98).
- **sinceVersion/deprecated on enum and set values**: `[Obsolete]` attribute and version comments on individual enum values and set choices (#94).
- **sinceVersion on data elements**: Variable-length data fields respect `sinceVersion` for versioned message generation (#95).
- **Explicit blockLength**: Messages now respect the `blockLength` attribute from the schema, with `BLOCK_LENGTH` constant in generated code (#93).
- **characterEncoding attribute**: Fixed-size char types now use `Encoding.UTF8` for `UTF-8` and `Encoding.Latin1` for all other encodings (#96).
- **VarString16/VarString32**: Variable-width length prefix for varData fields — automatically resolves composite length field type (uint8→byte, uint16→ushort, uint32→uint) (#97).
- **`<ref>` in composites**: Composites can now embed other composites via `<ref>` elements with correct size calculation (#88).
- **Nested composites**: Inline `<composite>` and `<enum>` definitions within composites are parsed and generated recursively (#89).
- **`<data>` in groups**: Groups can contain variable-length data fields with proper callback generation and SpanReader advancement (#91).
- **Nested groups**: Groups within groups are fully supported with recursive parsing, struct generation, and ConsumeVariableLengthSegments (#90).
- **Custom headerType**: The `headerType` attribute on `<messageSchema>` is now parsed and stored in `SchemaContext` (#92).

### Changed
- `DataFieldDefinition` now tracks `LengthPrefixType` for dynamic varData encoding.
- `SchemaCompositeDto` supports `NestedComposites` and `NestedEnums` lists.
- `SchemaGroupDto` supports `Data` and `Groups` lists for nested structures.
- `GroupDefinition` supports `Datas` and `NestedGroups` with typed accessors.
- `MessageDefinition.ConsumeVariableLengthSegments` refactored to recursive `AppendGroupConsume` for nested group support.
- `TypesCodeGenerator.GenerateComposite` refactored to handle `<ref>` fields and nested types.

### Fixed
- `EnumDefinition` was using enum-level `Description` instead of per-field `Description` for XML doc comments.

## [0.4.0] - 2026-04-09

### Changed
- **Single-pass XmlReader parser**: Replaced `XmlDocument` DOM with forward-only `XmlReader` producing pre-parsed `ParsedSchema` DTOs in a single pass. Eliminates all XPath queries (`SelectNodes`), repeated attribute access, and DOM materialization.
- **LINQ elimination**: Replaced LINQ chains in `MessagesCodeGenerator` (`Where`/`Select`/`ToList`, `OrderBy`, `Take`) with `foreach` loops and pre-sized `List<T>`.
- **String interpolation removal**: Replaced `$"..."` interpolated strings in code generation with chained `StringBuilder.Append()` calls to reduce GC pressure.
- **Collection pre-sizing**: Pre-sized all `Dictionary`, `List`, and `StringBuilder` allocations across the pipeline to reduce resize operations.
- **Cast caching**: Cached `Cast<GroupDefinition>()` / `Cast<DataFieldDefinition>()` results in `MessageDefinition` to avoid repeated list allocations.
- **Dead code removal**: Removed legacy `SchemaParser.cs` (superseded by `SchemaReader`).
- Generator pipeline throughput improved ~2.4x (2.5ms → ~1.07ms per schema iteration in profiling).

## [0.3.0] - 2026-04-08

### Added
- **SBE007**: Warning for non-native byte order schemas (e.g., `bigEndian` on little-endian platforms).
- **SBE008**: Error when a type reference cannot be resolved to any known primitive or custom type.
- **SBE009**: Warning when `minValue`/`maxValue` constraints contain non-numeric values.
- **SBE010**: Warning when a primitive type fallback is used for length or null sentinel lookups.
- Generator phase isolation: each phase (Types, Messages, Utilities, Validation) runs in its own try/catch, so a failure in one does not block the others.
- Safe dictionary accessors (`GetPrimitiveLength`, `GetNullValue`) with `TryGetValue` instead of bare indexers.

### Fixed
- **XXE vulnerability**: XML loading now uses `DtdProcessing.Prohibit` and `XmlResolver = null`.
- **Group loop runaway**: `ConsumeVariableLengthSegments` now breaks on failed `TryRead` instead of silently continuing.
- **Slice guard**: Added `blockLength > buffer.Length` validation before slicing in `TryParse`.
- **Integer overflow**: `SumFieldLength()` now runs inside a `checked{}` block.
- Required schema attributes validated via `GetRequiredAttribute` (emits SBE002) instead of silently using empty strings.
- `sinceVersion` invalid values now emit SBE001 warning instead of being silently ignored.
- `dimensionType` not found on groups now emits SBE005 warning when falling back to default.
- `GetTypeLength()` emits SBE008 instead of throwing `ArgumentException` for unknown types.

## [0.2.0] - 2025-07-24

### Fixed
- **GroupSizeEncoding bug**: `GetNumInGroupType()` fallback changed from `uint` (uint32) to `ushort` (uint16) — the SBE spec default. Fixes CS0266 compilation errors in generated code for schemas defining `numInGroup` as `uint16`.
- `GroupDefinition.NumInGroupType` default parameter corrected from `"uint"` to `"ushort"`.

### Added
- Unit tests for both uint16 and uint32 `numInGroup` schemas.
- NuGet package properties: `DevelopmentDependency`, `SuppressDependenciesWhenPacking`, `SymbolPackageFormat`, Source Link.
- CI: NuGet package validation step and snupkg artifact upload.
- CD: package validation, snupkg push, GitHub Release asset upload.
- Release checklist (Portuguese) in CICD_PIPELINE.md.

### Changed
- Benchmarks re-enabled (5 files renamed from `.disabled` to `.cs`).
- README: Quick Start now shows `dotnet add package` as primary method.
- README: Feature status updated — deprecated fields, byte order, varData marked as implemented; nested groups documented as limitation.
- System.Text.Json updated from `9.0.0-rc.1` to stable `8.0.5`.
- Fixed broken relative links and old repository URLs across README, CONTRIBUTING, and docs.
- SBE compliance updated to ~92-95%.

### Known Limitations (Resolved in 0.5.0)
- ~~Nested groups (groups within groups) are not yet supported.~~ → Resolved in #90.
- ~~Extended varData types (VarString16, VarString32) not yet available.~~ → Resolved in #97.

## [0.1.0-preview.1] - 2024-10-06

### Added
- Initial preview release
- Core SBE features implementation
- Source generator for SBE schemas
- Support for primitive types, composites, enums, sets, messages, and groups
- Comprehensive test suite with snapshot and integration tests

[Unreleased]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.9.1...v1.0.0
[0.9.1]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.9.0...v0.9.1
[0.9.0]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.8.0...v0.9.0
[0.8.0]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.7.0...v0.8.0
[0.7.0]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.6.0...v0.7.0
[0.6.0]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.5.0...v0.6.0
[0.5.0]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.1.0-preview.1...v0.2.0
[0.1.0-preview.1]: https://github.com/pedrosakuma/SbeSourceGenerator/releases/tag/v0.1.0-preview.1
