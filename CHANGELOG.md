# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

### Known Limitations
- Nested groups (groups within groups) are not yet supported.
- Extended varData types (VarString16, VarString32) not yet available.

## [0.1.0-preview.1] - 2024-10-06

### Added
- Initial preview release
- Core SBE features implementation
- Source generator for SBE schemas
- Support for primitive types, composites, enums, sets, messages, and groups
- Comprehensive test suite with snapshot and integration tests

[Unreleased]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.1.0-preview.1...v0.2.0
[0.1.0-preview.1]: https://github.com/pedrosakuma/SbeSourceGenerator/releases/tag/v0.1.0-preview.1
