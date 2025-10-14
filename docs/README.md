# Documentation

This folder contains comprehensive documentation for the SBE Code Generator project.

## Architecture & Design

- **[ARCHITECTURE_DIAGRAMS.md](./ARCHITECTURE_DIAGRAMS.md)** - Visual diagrams of the system architecture
- **[GENERATOR_DECOMPOSITION_SUMMARY.md](./GENERATOR_DECOMPOSITION_SUMMARY.md)** - How the generator is decomposed into modular components
- **[sbe-generator.md](./sbe-generator.md)** - ⭐ **Developer guide**: Pipeline, data structures, and extension points

## Implementation Details

- **[SBE_FEATURE_COMPLETENESS.md](./SBE_FEATURE_COMPLETENESS.md)** - Current feature implementation status
- **[SBE_IMPLEMENTATION_ROADMAP.md](./SBE_IMPLEMENTATION_ROADMAP.md)** - Future development plans
- **[SBE_CHECKLIST.md](./SBE_CHECKLIST.md)** - Implementation checklist

## Validation Patterns ⭐ NEW

- **[VALIDATION_PATTERNS.md](./VALIDATION_PATTERNS.md)** - Design discussion and comparison of all validation patterns
- **[VALIDATION_PATTERNS_EXAMPLES.md](./VALIDATION_PATTERNS_EXAMPLES.md)** - Practical real-world examples and best practices
- **[VALIDATION_CONSTRAINTS.md](./VALIDATION_CONSTRAINTS.md)** - Feature overview and API reference
- **[VALIDATION_EXAMPLE.md](./VALIDATION_EXAMPLE.md)** - Quick start example

The validation system now supports three complementary patterns:
1. **Validate()** - Traditional throwing validation (fail-fast)
2. **TryValidate()** - Non-throwing validation with error messages (user-friendly)
3. **CreateValidated()** - Factory method pattern with validation (fluent)

## Advanced Features

- **[BLOCK_LENGTH_EXTENSION.md](./BLOCK_LENGTH_EXTENSION.md)** - Schema evolution and forward compatibility

## Testing

- **[TESTING_GUIDE.md](./TESTING_GUIDE.md)** - How to test the generator

## Deployment

- **[CICD_PIPELINE.md](./CICD_PIPELINE.md)** - CI/CD pipeline documentation
- **[NUGET_SETUP_GUIDE.md](./NUGET_SETUP_GUIDE.md)** - Publishing to NuGet

## Quick Links

- Back to [Main README](../README.md)
- [Contributing Guidelines](../CONTRIBUTING.md)
- [Examples](../examples/README.md)
