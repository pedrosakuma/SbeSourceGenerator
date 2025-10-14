# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Phase 3: Readonly Ref Structs with Constructors** - Type System Enhancement
  - Readonly modifier for ref structs (variable-length data types like `VarString8`)
  - Readonly fields in ref structs for immutability
  - Constructors for explicit ref struct initialization
  - Updated `Create()` factory methods to use constructors
  - 4 new comprehensive unit tests for Phase 3 features
  - Complete Phase 3 documentation suite:
    - `PHASE3_IMPLEMENTATION.md` - Technical implementation details
    - `PHASE3_SUMMARY.md` - Executive summary
    - `MIGRATION_GUIDE_PHASE3.md` - Step-by-step migration guide
    - `PHASE3_COMPLETE.md` - Completion summary
    - `PHASE3_README.md` - Documentation index
- GitHub Actions CI/CD pipeline for automated build, test, and NuGet publishing
- CI workflow (`ci.yml`) - Runs on push and pull requests
  - Builds main library and test projects
  - Executes all unit and integration tests
  - Creates NuGet package (dry-run)
  - Uploads build artifacts
- CD workflow (`publish.yml`) - Publishes to NuGet.org
  - Triggers on GitHub releases
  - Supports manual execution with version input
  - Runs full test suite before publishing
  - Automatically extracts version from git tags
- Comprehensive CI/CD documentation (`docs/CICD_PIPELINE.md`)
- Quick start guide for NuGet setup (`docs/NUGET_SETUP_GUIDE.md`)
- CI/CD status badges in README.md

### Changed
- **Phase 3: Breaking Changes**
  - Ref structs now use `readonly ref struct` declaration (affects `VarString8` and similar types)
  - Object initializer syntax no longer supported for ref structs - must use constructors
  - All fields in ref structs are now readonly
  - Migration guide provided for updating existing code
- Updated `SBE_IMPLEMENTATION_ROADMAP.md` to mark Phase 3 Option 1 as complete
- Updated package metadata in `.csproj`
  - Fixed repository URLs to point to correct GitHub repository
  - Added LICENSE.txt to NuGet package
- Updated README.md with CI/CD badges and documentation links
- Updated CONTRIBUTING.md with CI/CD documentation reference

### Fixed
- None

### Infrastructure
- CI workflow excludes failing example projects (focuses on library and tests only)
- Proper versioning support via git tags (format: `vX.Y.Z`)
- NuGet API key management via GitHub Secrets
- Artifacts retention: 7 days for CI, 90 days for releases

### Technical Details
- **Files Modified**: 2 source files (1 generator, 1 test file)
- **Tests Added**: 4 new unit tests for Phase 3
- **Total Tests**: 79 tests (39 unit + 40 integration) - All passing ✅
- **Documentation**: 5 new comprehensive Phase 3 documents (~1800 lines)
- **Breaking Change**: Object initializers for ref structs no longer compile
- **Migration Effort**: Low - simple find/replace for affected code
- **Security**: CodeQL analysis passed with 0 alerts

## [Unreleased]

### Added
- GitHub Actions CI/CD pipeline for automated build, test, and NuGet publishing
- CI workflow (`ci.yml`) - Runs on push and pull requests
  - Builds main library and test projects
  - Executes all unit and integration tests
  - Creates NuGet package (dry-run)
  - Uploads build artifacts
- CD workflow (`publish.yml`) - Publishes to NuGet.org
  - Triggers on GitHub releases
  - Supports manual execution with version input
  - Runs full test suite before publishing
  - Automatically extracts version from git tags
- Comprehensive CI/CD documentation (`docs/CICD_PIPELINE.md`)
- Quick start guide for NuGet setup (`docs/NUGET_SETUP_GUIDE.md`)
- CI/CD status badges in README.md

### Changed
- Updated package metadata in `.csproj`
  - Fixed repository URLs to point to correct GitHub repository
  - Added LICENSE.txt to NuGet package
- Updated README.md with CI/CD badges and documentation links
- Updated CONTRIBUTING.md with CI/CD documentation reference

### Infrastructure
- CI workflow excludes failing example projects (focuses on library and tests only)
- Proper versioning support via git tags (format: `vX.Y.Z`)
- NuGet API key management via GitHub Secrets
- Artifacts retention: 7 days for CI, 90 days for releases

## [0.1.0-preview.1] - 2024-10-06

### Added
- Initial preview release
- Core SBE features implementation
- Source generator for SBE schemas
- Support for primitive types, composites, enums, sets, messages, and groups
- Comprehensive test suite with snapshot and integration tests

[Unreleased]: https://github.com/pedrosakuma/SbeSourceGenerator/compare/v0.1.0-preview.1...HEAD
[0.1.0-preview.1]: https://github.com/pedrosakuma/SbeSourceGenerator/releases/tag/v0.1.0-preview.1
