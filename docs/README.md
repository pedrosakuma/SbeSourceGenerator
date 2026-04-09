# Documentation

This folder contains documentation for the SBE Code Generator project.

## Architecture & Design

- **[sbe-generator.md](./sbe-generator.md)** - ⭐ **Developer guide**: Pipeline, data structures, and extension points
- **[ARCHITECTURE_DIAGRAMS.md](./ARCHITECTURE_DIAGRAMS.md)** - Visual diagrams of the system architecture

## Implementation Status

- **[SBE_FEATURE_COMPLETENESS.md](./SBE_FEATURE_COMPLETENESS.md)** - Current feature implementation status
- **[SBE_IMPLEMENTATION_ROADMAP.md](./SBE_IMPLEMENTATION_ROADMAP.md)** - Development roadmap
- **[SBE_FEATURE_GAPS.md](./SBE_FEATURE_GAPS.md)** - Known feature gaps
- **[SBE_GENERATORS_COMPARISON.md](./SBE_GENERATORS_COMPARISON.md)** - Comparison with other SBE generators

## Feature Documentation

### Schema & Versioning
- **[SCHEMA_VERSIONING.md](./SCHEMA_VERSIONING.md)** - Schema evolution with sinceVersion
- **[BYTE_ORDER.md](./BYTE_ORDER.md)** - Endianness support

### Parsing (SpanReader)
- **[SPAN_READER_README.md](./SPAN_READER_README.md)** - SpanReader API reference and usage
- **[SPAN_READER_DESIGN_RATIONALE.md](./SPAN_READER_DESIGN_RATIONALE.md)** - Design decisions and tradeoffs
- **[SPAN_READER_EXTENSIBILITY.md](./SPAN_READER_EXTENSIBILITY.md)** - Advanced features and custom parsing patterns
- **[SPAN_READER_INTEGRATION.md](./SPAN_READER_INTEGRATION.md)** - How SpanReader is integrated into parsing flows
- **[SPAN_READER_IMPLEMENTATION_SUMMARY.md](./SPAN_READER_IMPLEMENTATION_SUMMARY.md)** - Implementation details

### Encoding (SpanWriter)
- **[FLUENT_ENCODER_API.md](./FLUENT_ENCODER_API.md)** - Fluent encoding API
- **[WRITING_SUPPORT_README.md](./WRITING_SUPPORT_README.md)** - Writing support overview

### Validation
- **[VALIDATION_PATTERNS.md](./VALIDATION_PATTERNS.md)** - Design comparison of all validation patterns
- **[VALIDATION_PATTERNS_EXAMPLES.md](./VALIDATION_PATTERNS_EXAMPLES.md)** - Practical real-world examples
- **[VALIDATION_CONSTRAINTS.md](./VALIDATION_CONSTRAINTS.md)** - Feature overview and API reference
- **[VALIDATION_EXAMPLE.md](./VALIDATION_EXAMPLE.md)** - Quick start example

### Performance
- **[PERFORMANCE_TUNING_GUIDE.md](./PERFORMANCE_TUNING_GUIDE.md)** - Optimization best practices

## Operations

- **[TESTING_GUIDE.md](./TESTING_GUIDE.md)** - How to test the generator
- **[CICD_PIPELINE.md](./CICD_PIPELINE.md)** - CI/CD pipeline documentation
- **[NUGET_SETUP_GUIDE.md](./NUGET_SETUP_GUIDE.md)** - Publishing to NuGet

## Quick Links

- Back to [Main README](../README.md)
- [Contributing Guidelines](../CONTRIBUTING.md)
- [Examples](../examples/README.md)
