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

## Type System Enhancements ⭐

Progressive enhancement of generated types with constructors, readonly modifiers, and conversions:

### Phase 1: TypeDefinition (✅ Complete)
- **[PHASE1_IMPLEMENTATION.md](./PHASE1_IMPLEMENTATION.md)** - Technical implementation details
- **[PHASE1_SUMMARY.md](./PHASE1_SUMMARY.md)** - Executive summary
- **[MIGRATION_GUIDE_PHASE1.md](./MIGRATION_GUIDE_PHASE1.md)** - Migration guide

**Delivered:** Readonly structs, constructors, and implicit/explicit conversions for TypeDefinition

### Phase 2: Review and Planning (✅ Complete - Historical)
- **[PHASE2_README.md](./PHASE2_README.md)** - ⭐ Documentation index and quick start
- **[PHASE2_IMPLEMENTATION.md](./PHASE2_IMPLEMENTATION.md)** - Technical analysis and recommendations
- **[PHASE2_SUMMARY.md](./PHASE2_SUMMARY.md)** - Executive summary
- **[PHASE2_COMPLETE.md](./PHASE2_COMPLETE.md)** - Phase 2 completion summary
- **[MIGRATION_GUIDE_PHASE2.md](./MIGRATION_GUIDE_PHASE2.md)** - Future migration recommendations

**Note:** Phase 2 was a planning phase. The recommendations were implemented in Phase 3.

### Phase 3: Readonly Ref Structs (✅ Complete)
- **[PHASE3_README.md](./PHASE3_README.md)** - ⭐ **START HERE** - Documentation index
- **[PHASE3_IMPLEMENTATION.md](./PHASE3_IMPLEMENTATION.md)** - Technical implementation details
- **[PHASE3_SUMMARY.md](./PHASE3_SUMMARY.md)** - Executive summary
- **[PHASE3_COMPLETE.md](./PHASE3_COMPLETE.md)** - Phase 3 completion summary
- **[MIGRATION_GUIDE_PHASE3.md](./MIGRATION_GUIDE_PHASE3.md)** - Migration guide

**Delivered:** Readonly ref structs with constructors for variable-length data types

### Feasibility Study (Foundation)
- **[FEASIBILITY_STUDY_AUTO_CONSTRUCTORS_READONLY_CONVERSIONS.md](./FEASIBILITY_STUDY_AUTO_CONSTRUCTORS_READONLY_CONVERSIONS.md)** - Original comprehensive study
- **[FEASIBILITY_STUDY_SUMMARY_PT.md](./FEASIBILITY_STUDY_SUMMARY_PT.md)** - Portuguese executive summary

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

## SpanReader Integration ⭐ NEW

Progressive enhancement of parsing flows with automatic offset management:

- **[SPAN_READER_INTEGRATION.md](./SPAN_READER_INTEGRATION.md)** - ⭐ **Integration summary**: How SpanReader was integrated into SBE parsing flows
- **[SPAN_READER_README.md](./SPAN_READER_README.md)** - SpanReader API reference and usage examples
- **[SPAN_READER_DESIGN_RATIONALE.md](./SPAN_READER_DESIGN_RATIONALE.md)** - ⭐ **Design decisions**: Comprehensive analysis of interface patterns, tradeoffs, and rationale
- **[SPAN_READER_EXTENSIBILITY.md](./SPAN_READER_EXTENSIBILITY.md)** - Advanced features and custom parsing patterns
- **[SPAN_READER_IMPLEMENTATION_SUMMARY.md](./SPAN_READER_IMPLEMENTATION_SUMMARY.md)** - Original prototype implementation details
- **[SPAN_READER_EVALUATION.md](./SPAN_READER_EVALUATION.md)** - Initial evaluation and feasibility study

**Delivered:** Automatic offset management in generated parsing code, eliminating manual offset tracking errors

## Testing

- **[TESTING_GUIDE.md](./TESTING_GUIDE.md)** - How to test the generator

## Deployment

- **[CICD_PIPELINE.md](./CICD_PIPELINE.md)** - CI/CD pipeline documentation
- **[NUGET_SETUP_GUIDE.md](./NUGET_SETUP_GUIDE.md)** - Publishing to NuGet

## Quick Links

- Back to [Main README](../README.md)
- [Contributing Guidelines](../CONTRIBUTING.md)
- [Examples](../examples/README.md)
