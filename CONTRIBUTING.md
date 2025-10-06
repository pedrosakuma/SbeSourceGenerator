# Contributing to SBE Code Generator

Thank you for your interest in contributing to the SBE Code Generator! This document provides guidelines for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How to Contribute](#how-to-contribute)
- [Development Setup](#development-setup)
- [Building and Testing](#building-and-testing)
- [Coding Standards](#coding-standards)
- [Pull Request Process](#pull-request-process)
- [Priority Areas](#priority-areas)

## Code of Conduct

This project follows a simple code of conduct:

- Be respectful and professional
- Welcome newcomers and help them get started
- Focus on constructive feedback
- Respect different viewpoints and experiences

## Getting Started

1. **Read the Documentation**
   - [README.md](./README.md) - Project overview
   - [SBE_FEATURE_COMPLETENESS.md](./SBE_FEATURE_COMPLETENESS.md) - Current feature status
   - [SBE_IMPLEMENTATION_ROADMAP.md](./SBE_IMPLEMENTATION_ROADMAP.md) - Future plans
   - [ARCHITECTURE_DIAGRAMS.md](./docs/ARCHITECTURE_DIAGRAMS.md) - System design
   - [sbe-generator.md](./docs/sbe-generator.md) - Generator architecture and extension guide ⭐ **Start here for development**

2. **Explore the Codebase**
   - Browse the code in `src/SbeCodeGenerator/`
   - Look at tests in `tests/SbeCodeGenerator.Tests/`
   - Check out examples in `examples/` folder
   - Read documentation in `docs/`

3. **Find an Issue**
   - Check [open issues](https://github.com/pedrosakuma/PcapSbePocConsole/issues)
   - Look for issues labeled `good first issue` or `help wanted`
   - Review the [Implementation Roadmap](./SBE_IMPLEMENTATION_ROADMAP.md) for larger features

## How to Contribute

There are many ways to contribute:

### 📝 Documentation
- Improve existing documentation
- Add code examples
- Create tutorials
- Fix typos and clarify explanations

### 🧪 Testing
- Add more unit tests
- Create integration tests
- Add edge case tests
- Improve test coverage

### 🐛 Bug Fixes
- Fix reported bugs
- Add regression tests
- Improve error handling

### ✨ Features
- Implement features from the roadmap
- Add new diagnostics
- Improve code generation

### 📊 Examples
- Create example schemas
- Build sample applications
- Share real-world use cases

## Development Setup

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download) or later
- [Git](https://git-scm.com/)
- A code editor:
  - [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended for Windows)
  - [Visual Studio Code](https://code.visualstudio.com/)
  - [JetBrains Rider](https://www.jetbrains.com/rider/)

### Clone and Build

```bash
# Clone the repository
git clone https://github.com/pedrosakuma/PcapSbePocConsole.git
cd PcapSbePocConsole

# Build the solution
dotnet build

# Run tests
dotnet test

# Run a specific test project
dotnet test SbeCodeGenerator.Tests
```

### IDE Setup

**Visual Studio 2022**:
1. Open `SbeSourceGenerator.sln`
2. Restore NuGet packages
3. Build solution (Ctrl+Shift+B)
4. Run tests (Ctrl+R, A)

**Visual Studio Code**:
1. Install C# extension
2. Open the folder
3. Run build task (Ctrl+Shift+B)
4. Use Test Explorer for tests

## Building and Testing

### Build Commands

```bash
# Clean build
dotnet clean
dotnet build --no-incremental

# Release build
dotnet build -c Release

# Build specific project
dotnet build src/SbeCodeGenerator/SbeSourceGenerator.csproj
```

### Testing Commands

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~TypesCodeGeneratorTests"

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

### Local Testing Workflow

When making changes to the generator, follow this workflow to validate your changes:

#### 1. Build the Generator

```bash
# Build the generator project
dotnet build src/SbeCodeGenerator/SbeSourceGenerator.csproj

# Or build the entire solution
dotnet build
```

#### 2. Run Unit Tests

```bash
# Run all unit tests
dotnet test tests/SbeCodeGenerator.Tests/

# Run specific test class
dotnet test --filter "FullyQualifiedName~TypesCodeGeneratorTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~TypesCodeGeneratorTests.Generate_WithSimpleEnum_ProducesEnumCode"

# Run with detailed output
dotnet test tests/SbeCodeGenerator.Tests/ --logger "console;verbosity=detailed"
```

#### 3. Run Integration Tests

```bash
# Run integration tests (these compile generated code)
dotnet test tests/SbeCodeGenerator.IntegrationTests/

# Clean and rebuild integration tests to ensure fresh generation
dotnet clean tests/SbeCodeGenerator.IntegrationTests/
dotnet build tests/SbeCodeGenerator.IntegrationTests/
dotnet test tests/SbeCodeGenerator.IntegrationTests/
```

#### 4. Test with Example Projects

Test your changes against real-world schemas:

```bash
# Clean and rebuild an example project
dotnet clean examples/PcapSbePocConsole/
dotnet build examples/PcapSbePocConsole/

# Inspect generated files
ls -la examples/PcapSbePocConsole/obj/Debug/net9.0/generated/SbeSourceGenerator/

# Run the example
dotnet run --project examples/PcapSbePocConsole/
```

#### 5. Validate Generated Code

Enable generated file output to inspect the code:

```xml
<!-- Add to example project .csproj -->
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Then check generated files:

```bash
# After building with EmitCompilerGeneratedFiles=true
find examples/PcapSbePocConsole/Generated -name "*.cs" | head -10

# View a generated file
cat examples/PcapSbePocConsole/Generated/SbeSourceGenerator/SbeSourceGenerator.SBESourceGenerator/B3.Market.Data.Messages/Enums/SomeEnum.cs
```

#### 6. Snapshot Testing Workflow

If you're making changes that affect code generation output:

```bash
# Run snapshot tests
dotnet test tests/SbeCodeGenerator.Tests/

# If output changed intentionally, review diffs
ls tests/SbeCodeGenerator.Tests/**/*.received.txt

# Compare received vs verified files
diff tests/SbeCodeGenerator.Tests/Snapshots/TypesCodeGenerator.Generate_WithSimpleEnum.verified.txt \
     tests/SbeCodeGenerator.Tests/Snapshots/TypesCodeGenerator.Generate_WithSimpleEnum.received.txt

# If changes are correct, update snapshots by copying .received.txt to .verified.txt
# Or use the Verify test framework's interactive mode
```

#### 7. Test Coverage Analysis

```bash
# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage"

# Coverage reports are in: tests/*/TestResults/*/coverage.cobertura.xml

# To generate HTML report, install ReportGenerator:
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate coverage report
reportgenerator \
  -reports:"tests/**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html

# Open the report
open coverage-report/index.html  # macOS
xdg-open coverage-report/index.html  # Linux
```

### Testing Checklist for Changes

Before submitting a PR, ensure:

- [ ] **Unit tests pass**: `dotnet test tests/SbeCodeGenerator.Tests/`
- [ ] **Integration tests pass**: `dotnet test tests/SbeCodeGenerator.IntegrationTests/`
- [ ] **Example projects build**: `dotnet build examples/PcapSbePocConsole/`
- [ ] **No new warnings**: `dotnet build --no-incremental 2>&1 | grep "warning"`
- [ ] **Snapshot tests updated**: If generation changed, snapshots are updated
- [ ] **Generated code compiles**: Integration tests verify this
- [ ] **Manual testing done**: Run example projects to verify behavior

### Debugging Source Generators

Source generators can be tricky to debug. Here are some approaches:

#### Method 1: Debugger.Launch()

Attach debugger when generator runs:

```csharp
// Add this in your generator code (remove before committing)
#if DEBUG
    if (!System.Diagnostics.Debugger.IsAttached)
    {
        System.Diagnostics.Debugger.Launch();
    }
#endif
```

Then build a project that uses the generator:

```bash
dotnet build examples/PcapSbePocConsole/
# Debugger will prompt to attach
```

#### Method 2: Inspect Generated Files

```bash
# Build with generated file output
dotnet build examples/PcapSbePocConsole/

# Check generated files location
ls -la examples/PcapSbePocConsole/obj/Debug/net9.0/generated/SbeSourceGenerator/SbeSourceGenerator.SBESourceGenerator/

# View generated content
cat examples/PcapSbePocConsole/obj/Debug/net9.0/generated/SbeSourceGenerator/SbeSourceGenerator.SBESourceGenerator/B3.Market.Data.Messages/Enums/*.cs
```

#### Method 3: Unit Test the Generators

Create unit tests that call generators directly:

```csharp
[Fact]
public void Debug_MyGeneratorChange()
{
    // Arrange
    var xml = @"<messageSchema>...</messageSchema>";
    var doc = new XmlDocument();
    doc.LoadXml(xml);
    var context = new SchemaContext();
    var generator = new TypesCodeGenerator();
    
    // Act - Set breakpoint here
    var results = generator.Generate("Test.Namespace", doc, context, default);
    
    // Assert
    var resultList = results.ToList();
    // Inspect results in debugger
}
```

#### Method 4: Add Temporary Logging

```csharp
// Add to generator code (remove before committing)
System.IO.File.AppendAllText(
    "/tmp/generator-debug.log", 
    $"Processing type: {typeName}\n"
);
```

Then check the log:

```bash
tail -f /tmp/generator-debug.log
```

### Testing Different Scenarios

#### Testing Edge Cases

```bash
# Create a test schema with edge cases
cat > /tmp/test-edge-cases.xml << 'EOF'
<messageSchema package="test" id="1">
    <types>
        <enum name="EmptyEnum" encodingType="uint8">
            <!-- Empty enum -->
        </enum>
        <type name="MaxLengthType" primitiveType="char" length="1000000"/>
    </types>
</messageSchema>
EOF

# Add to a test project and build
# Monitor for errors or warnings
```

#### Testing Performance

```bash
# Time the build with generator
time dotnet build examples/PcapSbePocConsole/

# Compare before and after changes
# Significant slowdowns may indicate performance regression
```

#### Testing Error Handling

Create invalid schemas to verify diagnostics:

```bash
# Create invalid schema
cat > /tmp/invalid-schema.xml << 'EOF'
<messageSchema>
    <types>
        <enum name="BadEnum" encodingType="invalidType">
            <validValue name="Val1">0</validValue>
        </enum>
    </types>
</messageSchema>
EOF

# Build should report diagnostic
# Check build output for expected error message
```

### Common Testing Issues

#### Issue: Tests Pass But Generated Code Doesn't Work

**Solution**: Run integration tests which compile generated code:

```bash
dotnet test tests/SbeCodeGenerator.IntegrationTests/
```

#### Issue: Snapshot Tests Failing

**Solution**: Review diffs and update if changes are intentional:

```bash
# Review what changed
diff tests/SbeCodeGenerator.Tests/Snapshots/*.verified.txt \
     tests/SbeCodeGenerator.Tests/Snapshots/*.received.txt

# If correct, copy received to verified
cp tests/SbeCodeGenerator.Tests/Snapshots/*.received.txt \
   tests/SbeCodeGenerator.Tests/Snapshots/*.verified.txt
```

#### Issue: Generated Files Not Updating

**Solution**: Clean and rebuild:

```bash
# Clean all generated files
dotnet clean

# Rebuild
dotnet build

# Or manually delete obj/ and bin/ directories
rm -rf **/obj **/bin
dotnet build
```

#### Issue: Can't See Generated Files

**Solution**: Enable `EmitCompilerGeneratedFiles`:

```xml
<!-- In .csproj -->
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

### Advanced Testing Topics

For more advanced testing scenarios, see:

- [TESTING_GUIDE.md](./docs/TESTING_GUIDE.md) - Comprehensive testing documentation
- [sbe-generator.md](./docs/sbe-generator.md) - Generator architecture and extension guide

## Coding Standards

### C# Style Guidelines

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and small
- Use records for immutable DTOs

### Project Conventions

**File Organization**:
```
SbeCodeGenerator/
├── Diagnostics/          # Diagnostic descriptors
├── Generators/           # Code generators
│   ├── Fields/          # Field-level generators
│   └── Types/           # Type-level generators
├── Helpers/             # Utility classes
└── Schema/              # DTOs and parsing
```

**Naming**:
- Generators: `*CodeGenerator.cs` or `*Generator.cs`
- Definitions: `*Definition.cs`
- DTOs: `*Dto.cs`
- Tests: `*Tests.cs`

**Code Style**:
```csharp
// Good: Clear, focused method
private static string ToNativeType(string sbeType)
{
    return sbeType switch
    {
        "int8" => "sbyte",
        "int16" => "short",
        _ => sbeType
    };
}

// Good: XML documentation
/// <summary>
/// Generates code for SBE composite types.
/// </summary>
/// <param name="ns">Namespace for generated code</param>
/// <param name="typeNode">XML element containing composite definition</param>
/// <returns>Generated composite code</returns>
private static IEnumerable<(string name, string content)> GenerateComposite(
    string ns, 
    XmlElement typeNode)
{
    // Implementation
}
```

### Testing Standards

**Test Structure**:
```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var input = CreateTestInput();
    var expected = CreateExpectedOutput();
    
    // Act
    var actual = MethodUnderTest(input);
    
    // Assert
    Assert.Equal(expected, actual);
}
```

**Test Coverage Requirements**:
- New features must include tests
- Bug fixes must include regression tests
- Aim for 90%+ code coverage
- Test both success and failure cases

## Pull Request Process

### Before Submitting

1. **Create an Issue** (for non-trivial changes)
   - Describe the problem or feature
   - Discuss the approach
   - Get feedback before coding

2. **Create a Branch**
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/issue-number-description
   ```

3. **Make Your Changes**
   - Follow coding standards
   - Add tests
   - Update documentation
   - Keep commits focused

4. **Test Thoroughly**
   ```bash
   # Build
   dotnet build
   
   # Run all tests
   dotnet test
   
   # Check for warnings
   dotnet build --no-incremental | grep warning
   ```

5. **Update Documentation**
   - Update relevant .md files
   - Add XML comments to new code
   - Update feature completeness if needed

### Submitting the PR

1. **Push Your Branch**
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create Pull Request**
   - Use a clear title
   - Reference related issues
   - Describe what changed and why
   - List any breaking changes
   - Add screenshots if UI-related

3. **PR Description Template**
   ```markdown
   ## Description
   Brief description of changes
   
   ## Related Issues
   Fixes #123
   
   ## Changes Made
   - Added X feature
   - Fixed Y bug
   - Updated Z documentation
   
   ## Testing
   - [ ] Unit tests added/updated
   - [ ] Integration tests added/updated
   - [ ] Manual testing performed
   
   ## Checklist
   - [ ] Code follows project style
   - [ ] Tests pass locally
   - [ ] Documentation updated
   - [ ] No breaking changes (or documented)
   ```

### Review Process

1. **Automated Checks**
   - Build must succeed
   - All tests must pass
   - No new warnings introduced

2. **Code Review**
   - Maintainer will review your code
   - Address feedback promptly
   - Be open to suggestions

3. **Merge**
   - Once approved, a maintainer will merge
   - Your changes will be in the next release

## Priority Areas

Want to make a big impact? Focus on these areas:

### 🔥 High Priority, Easy to Moderate

1. **Add More Tests**
   - Test edge cases
   - Add integration tests
   - Improve coverage

2. **Improve Documentation**
   - Add examples
   - Create tutorials
   - Fix unclear sections

3. **Add Diagnostics**
   - New validation rules
   - Better error messages
   - More helpful warnings

4. **Deprecated Field Marking**
   - Add [Obsolete] attributes
   - Update generators
   - Add tests

### 🚀 High Priority, Moderate to Hard

1. **Variable-Length Data Support**
   - Implement `<data>` element parsing
   - Generate varData accessors
   - Add comprehensive tests

2. **Schema Versioning**
   - Implement `sinceVersion` handling
   - Support schema evolution
   - Add version validation

3. **Validation Constraints**
   - Parse min/max values
   - Generate validation code
   - Add runtime checks

### 💡 Nice to Have

1. **Performance Optimizations**
   - Benchmark generated code
   - Optimize hot paths
   - Reduce allocations

2. **Tooling**
   - Visual Studio extension
   - Schema validator
   - Code snippets

3. **Examples**
   - More sample schemas
   - Real-world applications
   - Best practices guide

See [SBE_IMPLEMENTATION_ROADMAP.md](./SBE_IMPLEMENTATION_ROADMAP.md) for the complete roadmap.

## Questions?

- **General questions**: Open a [discussion](https://github.com/pedrosakuma/PcapSbePocConsole/discussions)
- **Bug reports**: Create an [issue](https://github.com/pedrosakuma/PcapSbePocConsole/issues)
- **Feature requests**: Create an [issue](https://github.com/pedrosakuma/PcapSbePocConsole/issues) with the "enhancement" label

## License

By contributing, you agree that your contributions will be licensed under the same license as the project (see [LICENSE.txt](./LICENSE.txt)).

---

Thank you for contributing to the SBE Code Generator! 🎉
