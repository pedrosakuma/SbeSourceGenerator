# Automated Testing for SBE Code Generator

This document describes the automated testing infrastructure for the SBE (Simple Binary Encoding) source generator.

## Overview

The test suite consists of two main components:

1. **Snapshot Tests** (`SbeCodeGenerator.Tests/SnapshotTests.cs`) - Validate generated code output against approved baselines
2. **Integration Tests** (`SbeCodeGenerator.IntegrationTests`) - Verify end-to-end code generation and compilation

## Snapshot Tests

Snapshot tests use [Verify.Xunit](https://github.com/VerifyTests/Verify) to ensure generated code remains consistent across refactorings and changes.

### What Gets Tested

- **TypesCodeGenerator**: Enums, Sets, Composites, and Custom Types
- **MessagesCodeGenerator**: Message structures and parsing helpers
- **UtilitiesCodeGenerator**: Utility classes like NumberExtensions

### Test Data

Representative test schemas are located in `SbeCodeGenerator.Tests/TestData/`:
- `test-schema-simple.xml` - A minimal SBE schema for testing basic type generation

### Approved Snapshots

Approved snapshots are stored in `SbeCodeGenerator.Tests/Snapshots/` as `.verified.txt` files. These files are committed to source control and represent the expected output.

### Updating Snapshots

When you make intentional changes to code generation logic:

1. Run the snapshot tests - they will fail and create `.received.txt` files
2. Review the `.received.txt` files to ensure the changes are correct
3. Accept the changes by copying `.received.txt` files to `.verified.txt`:
   ```bash
   cd SbeCodeGenerator.Tests/Snapshots
   for f in *.received.txt; do cp "$f" "${f%.received.txt}.verified.txt"; done
   ```
4. Or simply delete the `.verified.txt` files and re-run tests - Verify will prompt you to accept the new output
5. Commit the updated `.verified.txt` files

### Note on .received.txt Files

Files matching `*.received.*` are automatically excluded from source control via `.gitignore`. These are temporary test artifacts created during test runs.

## Integration Tests

Integration tests validate that:

1. XML schemas are properly fed to the generator via AdditionalFiles
2. Code is generated without errors  
3. Generated code compiles successfully
4. Generated types can be instantiated and used in real code

### Test Schemas

Integration test schemas are located in `SbeCodeGenerator.IntegrationTests/TestSchemas/`:
- `integration-test-schema.xml` - A comprehensive schema with enums, composites, messages, and groups

### What Gets Tested

- **Compilation**: Generated code must compile without errors
- **Type Availability**: All expected types (enums, messages, helpers) are generated
- **Instantiation**: Generated types can be created and used
- **Message Helpers**: Generated `TryParse` APIs expose decoded views of headers and messages
- **Namespace Generation**: Correct namespace derivation from file names

### Running Integration Tests

```bash
cd /path/to/PcapSbePocConsole
dotnet test SbeCodeGenerator.IntegrationTests
```

## Running All Tests

To run both snapshot and integration tests:

```bash
cd /path/to/PcapSbePocConsole
dotnet test
```

## CI Integration

The test suite is designed to be integrated into CI/CD pipelines:

- **Snapshot tests** will fail if generated output changes unexpectedly, preventing regressions
- **Integration tests** will fail if the generator produces code that doesn't compile
- Both provide fast feedback (< 1 second combined execution time)

### Example CI Configuration

```yaml
name: CI

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal
```

## Success Criteria

The automated test infrastructure meets the following success criteria:

✅ **Golden snapshot tests** verify generated sources using Verify
✅ **Integration tests** feed XML via additional files and compile generated output
✅ **CI fails** when generated output diverges unexpectedly
✅ **Representative schemas** cover key SBE features (enums, composites, messages, groups)
✅ **Fast feedback** - all tests complete in under 1 second

## Troubleshooting

### Snapshot test failures

If snapshot tests fail after making changes:
1. Review the diff between `.received.txt` and `.verified.txt` files
2. Verify the changes are intentional and correct
3. Update the approved snapshots (see "Updating Snapshots" above)

### Integration test failures

If integration tests fail:
1. Check that XML schemas in `TestSchemas/` are valid SBE schemas
2. Look for generator exceptions in the build output
3. Verify the `AdditionalFiles` in the `.csproj` include the test schemas
4. Ensure `EmitCompilerGeneratedFiles` is set to capture generator output

## Future Enhancements

Potential improvements to the test suite:

- Add snapshot tests for more complex schemas (nested groups, variable-length data)
- Add performance benchmarks comparing generator versions
- Add tests for diagnostic reporting
- Expand integration tests to cover error handling scenarios
