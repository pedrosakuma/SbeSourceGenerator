# SBE Code Generator - Architecture and Extension Guide

This document provides a comprehensive guide for developers who want to understand, extend, or contribute to the SBE (Simple Binary Encoding) Code Generator for C#.

## Table of Contents

- [Overview](#overview)
- [Generator Pipeline](#generator-pipeline)
- [Core Data Structures](#core-data-structures)
- [Code Generators](#code-generators)
- [Extension Points](#extension-points)
- [Testing Your Changes](#testing-your-changes)
- [Common Patterns](#common-patterns)
- [Troubleshooting](#troubleshooting)

## Overview

The SBE Code Generator is a C# source generator that transforms SBE XML schemas into high-performance C# code for encoding and decoding binary messages. It's designed with modularity and extensibility in mind, following the Single Responsibility Principle.

### Key Design Principles

- **Separation of Concerns**: Each generator handles a specific category of code generation
- **Incremental Generation**: Uses Roslyn's incremental generator API for efficient builds
- **Schema-Scoped State**: Each schema has its own context to avoid cross-schema pollution
- **Diagnostic Reporting**: Provides helpful error messages during generation
- **Zero Runtime Dependencies**: Generated code has no runtime dependencies on the generator

## Generator Pipeline

The code generation pipeline consists of several stages that transform XML schemas into C# source code:

```
┌─────────────────────────────────────────────────────────────────┐
│                    Generation Pipeline                           │
└─────────────────────────────────────────────────────────────────┘

Stage 1: XML Schema Collection
    │
    │   AdditionalTextsProvider
    │   Filter files ending with .xml
    │
    ▼
┌──────────────────────────┐
│ XML Schema Files (.xml)  │
└────────────┬─────────────┘
             │
Stage 2: Schema Parsing & Context Creation
    │
    │   Load XML Document
    │   Create SchemaContext (per-schema state)
    │   Extract namespace from filename
    │
    ▼
┌──────────────────────────┐
│  Parsed XML + Context    │
└────────────┬─────────────┘
             │
Stage 3: Specialized Code Generation
    │
    ├─────────────────────┬────────────────────────────┐
    │                     │                            │
    ▼                     ▼                            ▼
┌──────────┐    ┌──────────────┐            ┌──────────┐
│  Types   │    │   Messages   │            │Utilities │
│Generator │    │  Generator   │            │Generator │
└────┬─────┘    └──────┬───────┘            └────┬─────┘
     │                 │                         │
     │  Enums         │  Messages               │  Extensions
     │  Sets          │  Fields                 │
     │  Composites    │  Groups                 │
     │  Types         │  Variable data helpers  │
     │                 │  TryParse APIs         │
     └─────────────────┴────────────────────────┘
                       │
Stage 4: Source Registration
    │
    │   sourceContext.AddSource(name, content)
    │
    ▼
┌─────────────────────────────┐
│  Generated C# Source Files  │
│  Added to Compilation       │
└─────────────────────────────┘
```

### Pipeline Details

#### Stage 1: XML Schema Collection

```csharp
private static IncrementalValuesProvider<AdditionalText> CollectXmlSchemaFiles(
    IncrementalGeneratorInitializationContext initContext)
{
    return initContext.AdditionalTextsProvider
        .Where(file => file.Path.EndsWith(".xml"));
}
```

- **Input**: All additional files in the project
- **Output**: Filtered collection of XML schema files
- **Purpose**: Identify SBE schema files to process

#### Stage 2: Schema Parsing

```csharp
string path = text.Path;
string ns = GetNamespaceFromPath(path);  // Extract namespace from filename
var d = new XmlDocument();
d.Load(path);

var context = new SchemaContext();  // Per-schema state container
```

- **Input**: XML file path
- **Output**: Loaded XmlDocument and initialized SchemaContext
- **Purpose**: Prepare schema for processing
- **Namespace Derivation**: Filename `market-data-messages.xml` → `Market.Data.Messages`

#### Stage 3: Code Generation

```csharp
var typesGenerator = new TypesCodeGenerator();
var messagesGenerator = new MessagesCodeGenerator();
var utilitiesGenerator = new UtilitiesCodeGenerator();

foreach (var item in typesGenerator.Generate(ns, d, context, sourceContext))
    sourceContext.AddSource(item.name, item.content);
foreach (var item in messagesGenerator.Generate(ns, d, context, sourceContext))
    sourceContext.AddSource(item.name, item.content);
foreach (var item in utilitiesGenerator.Generate(ns, d, context, sourceContext))
    sourceContext.AddSource(item.name, item.content);
```

- **Input**: Namespace, XmlDocument, SchemaContext
- **Output**: Collection of (name, content) tuples representing generated files
- **Purpose**: Transform XML schema elements into C# code

#### Stage 4: Source Registration

- **Input**: Generated source code files
- **Output**: Sources added to compilation
- **Purpose**: Make generated code available to the compiler

## Core Data Structures

### SchemaContext

The `SchemaContext` class maintains per-schema state during code generation:

```csharp
public class SchemaContext
{
    // Maps enum names to their underlying primitive types (e.g., "OrderSide" -> "uint8")
    public Dictionary<string, string> EnumPrimitiveTypes { get; }
    
    // Maps custom type names to their byte lengths (e.g., "Price" -> 8)
    public Dictionary<string, int> CustomTypeLengths { get; }
    
    // Maps constant names to their type indicators
    public Dictionary<string, byte> CustomConstantTypes { get; }
}
```

**Why SchemaContext?**
- Avoids global state mutations
- Enables concurrent schema processing
- Simplifies testing with isolated state
- Prevents cross-schema contamination

### Schema DTOs (Data Transfer Objects)

DTOs represent parsed XML elements as immutable C# records:

#### SchemaFieldDto
```csharp
public record SchemaFieldDto(
    string Name,
    string Description,
    string PrimitiveType,
    string Presence,        // "optional", "required", "constant"
    string Length,
    string NullValue,
    string ValueRef,
    string InnerText,
    string Id,
    string Offset,
    string SemanticType
);
```

#### SchemaTypeDto
```csharp
public record SchemaTypeDto(
    string Name,
    string Description,
    string PrimitiveType,
    string Length,
    string Presence,
    string SemanticType,
    string NullValue,
    string MinValue,
    string MaxValue
);
```

#### SchemaEnumDto
```csharp
public record SchemaEnumDto(
    string Name,
    string EncodingType,
    string Description,
    List<SchemaEnumChoiceDto> Choices
);

public record SchemaEnumChoiceDto(
    string Name,
    string Description,
    string InnerText  // Numeric value
);
```

#### SchemaCompositeDto
```csharp
public record SchemaCompositeDto(
    string Name,
    string Description,
    string SemanticType,
    List<SchemaTypeDto> Types
);
```

#### SchemaMessageDto
```csharp
public record SchemaMessageDto(
    string Name,
    string Id,
    string Description,
    string SemanticType,
    string Deprecated,
    List<SchemaFieldDto> Fields,
    List<SchemaFieldDto> Constants,
    List<SchemaGroupDto> Groups,
    List<SchemaDataDto> Data
);
```

#### SchemaGroupDto
```csharp
public record SchemaGroupDto(
    string Name,
    string Id,
    string Description,
    string DimensionType,
    List<SchemaFieldDto> Fields,
    List<SchemaGroupDto> NestedGroups
);
```

### ICodeGenerator Interface

The core abstraction for all code generators:

```csharp
public interface ICodeGenerator
{
    /// <summary>
    /// Generates source code from an XML document and schema context.
    /// </summary>
    /// <param name="ns">The namespace for the generated code</param>
    /// <param name="xmlDocument">The XML schema document to process</param>
    /// <param name="context">The schema context for tracking types and metadata</param>
    /// <param name="sourceContext">The source production context for reporting diagnostics</param>
    /// <returns>An enumerable of tuples containing the file name and content</returns>
    IEnumerable<(string name, string content)> Generate(
        string ns,
        XmlDocument xmlDocument,
        SchemaContext context,
        SourceProductionContext sourceContext
    );
}
```

**Key Characteristics:**
- Returns file name and content pairs
- Receives shared SchemaContext for cross-generator communication
- Can report diagnostics via SourceProductionContext
- Generator implementations are stateless (state lives in SchemaContext)

## Code Generators

### TypesCodeGenerator

**Responsibility**: Generate all SBE type definitions

**Handles:**
- Simple types (type aliases like `Price`, `Quantity`)
- Enums (enumeration types with nullable variants)
- Sets (bitflag types)
- Composites (structured types with multiple fields)

**Example Flow:**

```csharp
public IEnumerable<(string name, string content)> Generate(
    string ns, 
    XmlDocument xmlDocument, 
    SchemaContext context, 
    SourceProductionContext sourceContext)
{
    var typeNodes = xmlDocument.SelectNodes("//types/*");
    foreach (XmlElement typeNode in typeNodes)
    {
        var generatedType = typeNode.Name switch
        {
            "composite" => GenerateComposite(ns, typeNode, context, sourceContext),
            "enum" => GenerateEnum(ns, typeNode, context, sourceContext),
            "type" => GenerateType(ns, typeNode, context, sourceContext),
            "set" => GenerateSet(ns, typeNode, context, sourceContext),
            _ => Enumerable.Empty<(string name, string content)>()
        };
        foreach (var item in generatedType)
            yield return item;
    }
}
```

**Generated Output Structure:**
```
{namespace}\Enums\{EnumName}.cs
{namespace}\Sets\{SetName}.cs
{namespace}\Types\{TypeName}.cs
{namespace}\Composites\{CompositeName}.cs
```

**Key Methods:**
- `GenerateType()`: Simple type aliases
- `GenerateEnum()`: Enum types with null handling
- `GenerateSet()`: Flag enums for bitfields
- `GenerateComposite()`: Structured types with semantic variants

### MessagesCodeGenerator

**Responsibility**: Generate SBE message definitions

**Handles:**
- Message structures
- Message fields (regular, optional, constant)
- Message groups (repeating groups)
- Message data fields (variable-length data)
- Parsing helpers (`TryParse`, variable-length callbacks)

**Example Flow:**

```csharp
public IEnumerable<(string name, string content)> Generate(
    string ns, 
    XmlDocument xmlDocument, 
    SchemaContext context, 
    SourceProductionContext sourceContext)
{
    var messageNodes = xmlDocument.SelectNodes("//message");
    foreach (XmlElement messageNode in messageNodes)
    {
        var messageDto = SchemaParser.ParseMessage(messageNode, context);
        
        // Generate message struct
        StringBuilder sb = new StringBuilder();
        new UsingsGenerator(ns).AppendFileContent(sb);
        // ... generate message code ...
        yield return ($"{ns}\\Messages\\{messageDto.Name}", sb.ToString());
    }
}
```

**Generated Output Structure:**
```
{namespace}\Messages\{MessageName}.cs
```
```

### UtilitiesCodeGenerator

**Responsibility**: Generate utility code and extensions

**Handles:**
- `NumberExtensions` class for byte order operations
- Future utility code

**Example:**

```csharp
public IEnumerable<(string name, string content)> Generate(
    string ns, 
    XmlDocument xmlDocument, 
    SchemaContext context, 
    SourceProductionContext sourceContext)
{
    StringBuilder sb = new StringBuilder();
    new NumberExtensions(ns).AppendFileContent(sb);
    yield return ($"Utilities\\NumberExtensions", sb.ToString());
}
```

## Extension Points

### Adding a New Generator

To add a new generator to the pipeline:

#### 1. Create Generator Class

```csharp
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Generates validation code for SBE messages.
    /// </summary>
    public class ValidationCodeGenerator : ICodeGenerator
    {
        public IEnumerable<(string name, string content)> Generate(
            string ns, 
            XmlDocument xmlDocument, 
            SchemaContext context, 
            SourceProductionContext sourceContext)
        {
            // Parse schema elements
            var messageNodes = xmlDocument.SelectNodes("//message");
            
            foreach (XmlElement messageNode in messageNodes)
            {
                var messageName = messageNode.GetAttribute("name");
                
                // Generate validation code
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
                sb.AppendLine($"    public static class {messageName}Validator");
                sb.AppendLine("    {");
                sb.AppendLine($"        public static bool Validate({messageName} message)");
                sb.AppendLine("        {");
                sb.AppendLine("            // Validation logic here");
                sb.AppendLine("            return true;");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine("}");
                
                yield return ($"{ns}\\Validators\\{messageName}Validator", sb.ToString());
            }
        }
    }
}
```

#### 2. Register in SBESourceGenerator

```csharp
private static void RegisterSourceGeneration(
    IncrementalGeneratorInitializationContext initContext, 
    IncrementalValuesProvider<AdditionalText> xmlFiles)
{
    initContext.RegisterSourceOutput(xmlFiles, (sourceContext, text) =>
    {
        try
        {
            string path = text.Path;
            string ns = GetNamespaceFromPath(path);
            var d = new XmlDocument();
            d.Load(path);
            
            var context = new SchemaContext();
            
            var typesGenerator = new TypesCodeGenerator();
            var messagesGenerator = new MessagesCodeGenerator();
            var utilitiesGenerator = new UtilitiesCodeGenerator();
            var validationGenerator = new ValidationCodeGenerator();  // Add new generator
            
            foreach (var item in typesGenerator.Generate(ns, d, context, sourceContext))
                sourceContext.AddSource(item.name, item.content);
            foreach (var item in messagesGenerator.Generate(ns, d, context, sourceContext))
                sourceContext.AddSource(item.name, item.content);
            foreach (var item in utilitiesGenerator.Generate(ns, d, context, sourceContext))
                sourceContext.AddSource(item.name, item.content);
            foreach (var item in validationGenerator.Generate(ns, d, context, sourceContext))  // Register
                sourceContext.AddSource(item.name, item.content);
        }
        catch (Exception ex)
        {
            // Error handling...
        }
    });
}
```

#### 3. Add Tests

```csharp
using Xunit;
using System.Xml;
using SbeSourceGenerator.Generators;

namespace SbeCodeGenerator.Tests
{
    public class ValidationCodeGeneratorTests
    {
        [Fact]
        public void Generate_WithMessage_ProducesValidatorCode()
        {
            // Arrange
            var xml = @"
                <messageSchema>
                    <message name='OrderMessage' id='1'>
                        <field name='orderId' id='1' type='uint64'/>
                    </message>
                </messageSchema>";
            
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var context = new SchemaContext();
            var generator = new ValidationCodeGenerator();
            
            // Act
            var results = generator.Generate("Test.Namespace", doc, context, default);
            
            // Assert
            var resultList = results.ToList();
            Assert.Single(resultList);
            Assert.Contains("OrderMessageValidator", resultList[0].content);
        }
    }
}
```

### Adding Custom Schema DTOs

If you need to parse new schema elements:

#### 1. Define the DTO

```csharp
namespace SbeSourceGenerator.Schema
{
    /// <summary>
    /// Represents a constraint element from the schema.
    /// </summary>
    public record SchemaConstraintDto(
        string MinValue,
        string MaxValue,
        string Pattern
    );
}
```

#### 2. Add Parser Method

```csharp
// In SchemaParser.cs
public static SchemaConstraintDto ParseConstraint(XmlElement constraintElement)
{
    return new SchemaConstraintDto(
        MinValue: constraintElement.GetAttributeOrEmpty("minValue"),
        MaxValue: constraintElement.GetAttributeOrEmpty("maxValue"),
        Pattern: constraintElement.GetAttributeOrEmpty("pattern")
    );
}
```

#### 3. Use in Generator

```csharp
var constraintNode = typeNode.SelectSingleNode("constraint");
if (constraintNode is XmlElement constraintElement)
{
    var constraint = SchemaParser.ParseConstraint(constraintElement);
    // Generate validation code based on constraint
}
```

### Adding Helper Methods

Common helper methods can be added to dedicated classes:

```csharp
namespace SbeSourceGenerator.Helpers
{
    public static class TypeHelpers
    {
        public static bool IsPrimitiveType(string typeName)
        {
            return typeName switch
            {
                "char" or "int8" or "int16" or "int32" or "int64" 
                    or "uint8" or "uint16" or "uint32" or "uint64" => true,
                _ => false
            };
        }
        
        public static string ToNativeType(string sbeType)
        {
            return sbeType switch
            {
                "char" => "byte",
                "int8" => "sbyte",
                "uint8" => "byte",
                "int16" => "short",
                "uint16" => "ushort",
                "int32" => "int",
                "uint32" => "uint",
                "int64" => "long",
                "uint64" => "ulong",
                _ => sbeType
            };
        }
    }
}
```

### Reporting Diagnostics

Use the `SourceProductionContext` to report issues:

```csharp
// Define diagnostic descriptor
private static readonly DiagnosticDescriptor InvalidConstraintRule = new DiagnosticDescriptor(
    id: "SBE007",
    title: "Invalid Constraint",
    messageFormat: "Invalid constraint value '{0}' for field '{1}'",
    category: "SbeCodeGenerator",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true
);

// Report during generation
if (!IsValidConstraint(constraint))
{
    sourceContext.ReportDiagnostic(Diagnostic.Create(
        InvalidConstraintRule,
        Location.None,
        constraint.Value,
        fieldName
    ));
}
```

## Testing Your Changes

### Unit Testing Generators

Create focused tests for your generator:

```csharp
[Fact]
public void Generate_WithSimpleType_ProducesTypeCode()
{
    // Arrange
    var xml = @"
        <messageSchema>
            <types>
                <type name='Price' primitiveType='int64'/>
            </types>
        </messageSchema>";
    
    var doc = new XmlDocument();
    doc.LoadXml(xml);
    var context = new SchemaContext();
    var generator = new TypesCodeGenerator();
    
    // Act
    var results = generator.Generate("Test.Namespace", doc, context, default);
    
    // Assert
    var resultList = results.ToList();
    Assert.NotEmpty(resultList);
    Assert.Contains("Price", resultList[0].content);
}
```

### Snapshot Testing

Use Verify to capture and compare generated output:

```csharp
[Fact]
public Task Generate_WithComplexMessage_MatchesSnapshot()
{
    // Arrange
    var schemaPath = "TestData/complex-schema.xml";
    var doc = new XmlDocument();
    doc.Load(schemaPath);
    var context = new SchemaContext();
    var generator = new MessagesCodeGenerator();
    
    // Act
    var results = generator.Generate("Test.Namespace", doc, context, default);
    var combined = string.Join("\n---\n", results.Select(r => r.content));
    
    // Assert - Verify will create/compare snapshot
    return Verify(combined);
}
```

### Integration Testing

Test the entire generation pipeline:

```csharp
// Create a test project with schema file
// Build and verify generated code compiles
[Fact]
public void GeneratedCode_Compiles()
{
    // This is handled by SbeCodeGenerator.IntegrationTests project
    // See tests/SbeCodeGenerator.IntegrationTests/
}
```

### Testing Workflow

1. **Write the Test First**: Define expected behavior
2. **Run Test (should fail)**: Verify test catches the issue
3. **Implement Generator**: Add your code
4. **Run Test (should pass)**: Verify implementation works
5. **Add Edge Cases**: Test boundary conditions
6. **Run All Tests**: Ensure no regressions

## Common Patterns

### Pattern 1: Parsing and Generating

```csharp
// Parse XML element to DTO
var dto = SchemaParser.ParseField(fieldElement);

// Use DTO to generate code
var fieldDefinition = new FieldDefinition(
    name: dto.Name,
    type: ToNativeType(dto.PrimitiveType),
    offset: int.Parse(dto.Offset)
);

// Append to output
StringBuilder sb = new StringBuilder();
fieldDefinition.AppendFileContent(sb);
```

### Pattern 2: Updating SchemaContext

```csharp
// Register type information for later lookup
if (generator is IBlittable blittableType)
{
    context.CustomTypeLengths[typeName] = blittableType.Length;
}

// Look up type information
if (context.CustomTypeLengths.TryGetValue(typeName, out int length))
{
    // Use the length...
}
```

### Pattern 3: Conditional Generation

```csharp
// Only generate if conditions are met
if (message.Fields.Any() || message.Groups.Any())
{
    yield return GenerateMessageStruct(message);
}

// Skip invalid or deprecated elements
if (field.Presence == "constant")
{
    continue;  // Constants handled separately
}
```

### Pattern 4: Builder Pattern for Code Generation

```csharp
var sb = new StringBuilder();
sb.AppendLine($"namespace {ns}");
sb.AppendLine("{");
sb.AppendLine($"    public struct {messageName}");
sb.AppendLine("    {");

foreach (var field in fields)
{
    sb.AppendLine($"        public {field.Type} {field.Name};");
}

sb.AppendLine("    }");
sb.AppendLine("}");

return sb.ToString();
```

## Troubleshooting

### Issue: Generated Code Doesn't Appear

**Symptoms:**
- Code generation runs but files aren't visible
- IntelliSense doesn't recognize generated types

**Solutions:**
1. Check that XML schema is marked as `AdditionalFiles` in `.csproj`:
   ```xml
   <ItemGroup>
     <AdditionalFiles Include="schema.xml" />
   </ItemGroup>
   ```

2. Enable `EmitCompilerGeneratedFiles` to see output:
   ```xml
   <PropertyGroup>
     <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
   </PropertyGroup>
   ```

3. Check `obj/Debug/net9.0/generated/` for generated files

### Issue: Diagnostics Not Appearing

**Symptoms:**
- Generator reports diagnostic but it doesn't show in IDE

**Solutions:**
1. Ensure diagnostic severity is appropriate (Error/Warning)
2. Check that diagnostic is created correctly:
   ```csharp
   sourceContext.ReportDiagnostic(Diagnostic.Create(
       descriptor,
       Location.None,
       args
   ));
   ```
3. Verify `SourceProductionContext` is not default

### Issue: Tests Failing After Changes

**Symptoms:**
- Snapshot tests fail
- Generated code doesn't match expected output

**Solutions:**
1. Review the diff in `.received.txt` vs `.verified.txt` files
2. If changes are intentional, update snapshots:
   ```bash
   dotnet test
   # Review diffs, then accept changes by copying .received.txt to .verified.txt
   ```
3. For integration tests, check compiler output for errors

### Issue: SchemaContext Not Shared

**Symptoms:**
- Type information not available in later generators
- Null reference exceptions when looking up types

**Solutions:**
1. Ensure same `SchemaContext` instance is passed to all generators
2. Verify types are registered before they're looked up:
   ```csharp
   context.CustomTypeLengths[typeName] = length;  // Register
   var length = context.CustomTypeLengths[typeName];  // Lookup
   ```

### Issue: Generator Performance

**Symptoms:**
- Build times are slow
- Generator runs on every keystroke

**Solutions:**
1. Use incremental generation properly
2. Minimize allocations in hot paths
3. Cache computed values in SchemaContext
4. Profile with dotnet-trace if needed

### Issue: Debugging Generator

**Symptoms:**
- Hard to understand what's happening during generation
- Need to inspect intermediate state

**Solutions:**
1. Use `Debugger.Launch()` in generator code (DEBUG only):
   ```csharp
   #if DEBUG
       if (!System.Diagnostics.Debugger.IsAttached)
       {
           System.Diagnostics.Debugger.Launch();
       }
   #endif
   ```

2. Write unit tests that call generator directly

3. Check generated files in `obj/Debug/net9.0/generated/`

4. Add temporary logging (remove before committing):
   ```csharp
   System.IO.File.AppendAllText("debug.log", $"Processing {typeName}\n");
   ```

## Additional Resources

- [ARCHITECTURE_DIAGRAMS.md](./ARCHITECTURE_DIAGRAMS.md) - Visual architecture diagrams
- [TESTING_GUIDE.md](./TESTING_GUIDE.md) - Comprehensive testing guide
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Contribution guidelines
- [SBE Specification](https://github.com/FIXTradingCommunity/fix-simple-binary-encoding) - Official SBE spec

## Summary

The SBE Code Generator is designed for extensibility:

1. **Pipeline**: XML → Parse → Generate → Output
2. **Data Structures**: DTOs for schema elements, SchemaContext for state
3. **Generators**: Modular implementations of ICodeGenerator
4. **Extension**: Add new generators, DTOs, or helpers as needed
5. **Testing**: Unit tests, snapshot tests, and integration tests

By following these patterns and leveraging the existing infrastructure, you can extend the generator to support new features, optimize performance, or improve diagnostics.
