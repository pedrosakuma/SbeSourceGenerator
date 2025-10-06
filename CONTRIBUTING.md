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

### Debugging Source Generators

Source generators can be tricky to debug. Here are some approaches:

**Method 1: Debugger.Launch()**
```csharp
// Add this in your generator code
#if DEBUG
    if (!System.Diagnostics.Debugger.IsAttached)
    {
        System.Diagnostics.Debugger.Launch();
    }
#endif
```

**Method 2: Build and inspect generated files**
```bash
# Build and check generated files
dotnet build
# Generated files are in: obj/Debug/net9.0/generated/
```

**Method 3: Unit test the generators**
```csharp
// Create unit tests that call generators directly
var generator = new TypesCodeGenerator();
var result = generator.Generate(ns, xmlDoc, context, default);
```

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
