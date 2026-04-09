# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[Unreleased]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.5.0...HEAD
[0.5.0]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.1.0-preview.1...v0.2.0
[0.1.0-preview.1]: https://github.com/pedrosakuma/SbeSourceGenerator/releases/tag/v0.1.0-preview.1
