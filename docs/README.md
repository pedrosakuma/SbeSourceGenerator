# Documentation

This folder contains documentation for the SBE Code Generator project.

## Architecture & Design

- **[sbe-generator.md](./sbe-generator.md)** — Developer guide: pipeline, data structures, extension points
- **[ARCHITECTURE_DIAGRAMS.md](./ARCHITECTURE_DIAGRAMS.md)** — Visual diagrams of the system architecture

## Feature Documentation

### Schema & Versioning
- **[SCHEMA_VERSIONING.md](./SCHEMA_VERSIONING.md)** — Schema evolution with sinceVersion
- **[BYTE_ORDER.md](./BYTE_ORDER.md)** — Endianness support

### Parsing (SpanReader)
- **[SPAN_READER_README.md](./SPAN_READER_README.md)** — SpanReader API reference, usage patterns, and extensibility (custom parsers, schema evolution)

### Validation
- **[VALIDATION_CONSTRAINTS.md](./VALIDATION_CONSTRAINTS.md)** — Validation API reference, patterns, and quick-start example

### Performance
- **[PERFORMANCE_TUNING_GUIDE.md](./PERFORMANCE_TUNING_GUIDE.md)** — Optimization best practices

## Operations

- **[TESTING_GUIDE.md](./TESTING_GUIDE.md)** — How to test the generator
- **[CICD_PIPELINE.md](./CICD_PIPELINE.md)** — CI/CD pipeline and NuGet publishing

## Source Code Reference

- **[Diagnostics README](../src/SbeCodeGenerator/Diagnostics/README.md)** — Diagnostic descriptor reference (SBE001–SBE018)
- **[Helpers README](../src/SbeCodeGenerator/Helpers/README.md)** — XML parsing helper utilities

## Quick Links

- [Main README](../README.md)
- [Contributing Guidelines](../CONTRIBUTING.md)
- [Changelog](../CHANGELOG.md)
