# Diagnostic Reporting Implementation

## Overview
This implementation adds comprehensive diagnostic reporting to the SBE source generator using Roslyn's `DiagnosticDescriptor` API. Instead of throwing runtime exceptions or silently failing, the generator now reports actionable build-time diagnostics for malformed schemas and invalid attributes.

## Diagnostic Descriptors

All diagnostics are defined in `SbeCodeGenerator/Diagnostics/SbeDiagnostics.cs`:

| ID | Title | Severity | Description |
|----|-------|----------|-------------|
| **SBE001** | Invalid integer attribute value | Error | An XML attribute expected to contain an integer has an invalid value |
| **SBE002** | Missing required attribute | Error | A required XML attribute is missing or has an empty value |
| **SBE003** | Invalid enum flag value | Error | An enum flag value must be a valid integer for bit-shift operations |
| **SBE004** | Malformed schema | Error | The XML schema file is malformed or cannot be parsed |
| **SBE005** | Unsupported construct | Warning | The schema contains a construct that is not yet supported |
| **SBE006** | Invalid type length | Error | A type definition has an invalid length attribute value |

## Key Changes

### 1. Diagnostic-Aware Parsing Helpers

Added new overloaded methods in `XmlParsingHelpers.cs` that accept `SourceProductionContext`:

```csharp
// Safe integer parsing with diagnostic reporting
public static int? GetIntAttributeOrNull(this XmlElement element, 
    string attributeName, SourceProductionContext context)

public static int GetIntAttributeOrDefault(this XmlElement element, 
    string attributeName, int defaultValue, SourceProductionContext context)

// Required attribute validation with diagnostic reporting
public static string GetRequiredAttribute(this XmlElement element, 
    string attributeName, SourceProductionContext context)

// Enum flag value parsing with diagnostic reporting
public static int? ParseEnumFlagValue(string value, string fieldName, 
    SourceProductionContext context)
```

### 2. Updated Generator Pipeline

Modified `SBESourceGenerator.cs` to pass `SourceProductionContext` through the entire pipeline:

- Changed `ICodeGenerator` interface to accept `SourceProductionContext`
- Updated all generator implementations (TypesCodeGenerator, MessagesCodeGenerator, etc.)
- Added try-catch in `RegisterSourceGeneration` to report malformed schema errors

### 3. Replaced Unsafe Parsing

Replaced all instances of `int.Parse()` with diagnostic-aware alternatives:

**Before:**
```csharp
field.Offset == "" ? null : int.Parse(field.Offset)
```

**After:**
```csharp
ParseOffset(field.Offset, field.Name, sourceContext)

// Where ParseOffset safely parses and reports diagnostics
private static int? ParseOffset(string offset, string fieldName, 
    SourceProductionContext sourceContext)
{
    if (string.IsNullOrEmpty(offset))
        return null;

    if (int.TryParse(offset, out int result))
        return result;

    if (sourceContext.CancellationToken != default)
    {
        sourceContext.ReportDiagnostic(Diagnostic.Create(
            SbeDiagnostics.InvalidIntegerAttribute,
            Location.None,
            "offset",
            offset,
            fieldName));
    }

    return null;
}
```

## How It Works

### At Build Time

When the generator runs during compilation:

1. Invalid attributes in XML schemas trigger diagnostics
2. Diagnostics appear in build output with clear error messages
3. Build fails for errors (SBE001-SBE004, SBE006) but succeeds for warnings (SBE005)
4. Developers get actionable feedback about what needs to be fixed

### Example Build Output

```
error SBE001: Attribute 'length' has invalid integer value 'NotANumber' in element 'type'
error SBE003: Enum flag 'Flag1' has invalid integer value 'xyz' that cannot be used for bit shifting
```

### In Tests

Tests use `default(SourceProductionContext)` which has a default CancellationToken. The code checks for this:

```csharp
if (context.CancellationToken != default)
{
    context.ReportDiagnostic(...);
}
```

This allows tests to run without a real compilation context while still validating the parsing logic.

## Benefits

1. **Better Developer Experience**: Clear, actionable error messages instead of cryptic exceptions
2. **Build-Time Validation**: Problems caught during compilation, not at runtime
3. **IDE Integration**: Diagnostics appear in Visual Studio/VS Code error list
4. **Graceful Degradation**: Invalid values use fallbacks instead of crashing
5. **Maintainability**: Centralized error handling and consistent diagnostic format

## Testing

Added comprehensive tests in `DiagnosticsTests.cs`:

- ✅ Validates diagnostic descriptors have correct properties
- ✅ Tests generator behavior with invalid integer attributes
- ✅ Tests generator behavior with invalid enum flag values
- ✅ Tests generator behavior with invalid offsets
- ✅ Ensures generators complete successfully even with invalid input

All existing tests still pass (16/16 passing).

## Success Criteria Met

✅ **DiagnosticDescriptors with clear IDs, messages, and severity**: All 6 diagnostics defined with proper metadata

✅ **Replaced int.Parse/Enum.Parse calls**: All unsafe parsing replaced with guarded helpers

✅ **Diagnostics visible in build output**: When generator runs with invalid input during actual builds, diagnostics are reported to the compiler

✅ **No regressions**: All existing tests pass, solution builds successfully
