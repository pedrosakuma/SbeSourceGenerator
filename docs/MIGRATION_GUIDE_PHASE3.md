# Migration Guide: Phase 3 - Readonly Ref Structs

## Overview

This guide provides step-by-step instructions for migrating code to Phase 3, which introduces readonly ref structs with constructors for variable-length data types.

**Phase 3 Status**: ✅ Implemented

## Summary of Changes

### What Changed in Phase 3

1. **Ref structs are now readonly** - `VarString8` and similar types
2. **Fields are readonly** - Cannot be modified after construction
3. **Constructors added** - Must use constructor for initialization
4. **Factory methods updated** - `Create()` uses constructors internally

### Breaking Changes

**Object initializer syntax no longer works**:

```csharp
// ❌ This will NOT compile after Phase 3
var str = new VarString8 { Length = 10, VarData = data };

// ✅ Use constructor instead
var str = new VarString8(10, data);
```

## Migration Steps

### Step 1: Identify Affected Code

Find all usages of ref struct types in your codebase:

```bash
# Find ref struct initializations
grep -r "new VarString.*{" --include="*.cs" .

# Find any ref struct types (check your schema for variable-length types)
# Common examples: VarString8, VarString16, VarData, etc.
```

### Step 2: Update Object Initializers

Replace object initializer syntax with constructor calls:

**Before (Phase 2)**:
```csharp
var symbol = new VarString8 
{ 
    Length = 6, 
    VarData = "BTCUSD"u8 
};
```

**After (Phase 3)**:
```csharp
var symbol = new VarString8(6, "BTCUSD"u8);
```

### Step 3: Update Field Mutations

Remove any code that mutates ref struct fields (will cause compile errors):

**Before (Phase 2)**:
```csharp
var str = new VarString8 { Length = 5, VarData = data };
// Later...
str.Length = 10;  // Modify field
str.VarData = newData;
```

**After (Phase 3)**:
```csharp
var str = new VarString8(5, data);
// Later... create new instance instead
str = new VarString8(10, newData);
```

### Step 4: Verify Factory Method Usage

Factory methods like `Create()` still work but now use constructors internally:

**Before and After (API unchanged)**:
```csharp
// This API is unchanged and continues to work
var str = VarString8.Create(buffer);
```

No migration needed for factory method calls.

### Step 5: Build and Test

```bash
# Build to find any compile errors
dotnet build

# Run tests to ensure correct behavior
dotnet test
```

Compile errors will point to any remaining object initializers that need updating.

## Common Migration Patterns

### Pattern 1: Simple Initialization

**Before**:
```csharp
var varStr = new VarString8 
{ 
    Length = length, 
    VarData = data 
};
```

**After**:
```csharp
var varStr = new VarString8(length, data);
```

### Pattern 2: Inline in Method Calls

**Before**:
```csharp
ProcessString(new VarString8 
{ 
    Length = 10, 
    VarData = buffer.Slice(0, 10) 
});
```

**After**:
```csharp
ProcessString(new VarString8(10, buffer.Slice(0, 10)));
```

### Pattern 3: Array/Collection Initialization

**Before**:
```csharp
var strings = new List<VarString8>
{
    new VarString8 { Length = 5, VarData = data1 },
    new VarString8 { Length = 3, VarData = data2 }
};
```

**After**:
```csharp
var strings = new List<VarString8>
{
    new VarString8(5, data1),
    new VarString8(3, data2)
};
```

### Pattern 4: Conditional Initialization

**Before**:
```csharp
VarString8 str;
if (condition)
    str = new VarString8 { Length = 10, VarData = data1 };
else
    str = new VarString8 { Length = 5, VarData = data2 };
```

**After**:
```csharp
VarString8 str;
if (condition)
    str = new VarString8(10, data1);
else
    str = new VarString8(5, data2);
```

### Pattern 5: Using Factory Methods (No Change)

**Before and After (unchanged)**:
```csharp
// Factory methods continue to work
var str = VarString8.Create(buffer);
var str2 = VarString8.Create(buffer.Slice(offset));
```

## What Types Are Affected?

### Ref Struct Types (Affected) ✅

Any composite type with variable-length fields:
- `VarString8` (length + UTF-8 data)
- `VarString16` (if defined in your schema)
- `VarData` (if defined)
- Any custom variable-length composite types

**How to identify**: Look for composites with a `length="0"` field in your SBE schema.

### Blittable Types (NOT Affected) ✅

Regular composites with fixed-size fields are **unchanged**:
- `MessageHeader` ✅ Still a regular struct
- `Price` (composite with mantissa/exponent) ✅ Unchanged
- `GroupSizeEncoding` ✅ Unchanged
- Any fixed-size composite type ✅ Unchanged

**Reason**: Blittable types need to work with `MemoryMarshal` for zero-copy deserialization.

### Other Types (NOT Affected) ✅

- **TypeDefinition** ✅ Already readonly in Phase 1
- **OptionalTypeDefinition** ✅ Not changed in Phase 3
- **Enums** ✅ Not affected
- **Sets** ✅ Not affected
- **Messages** ✅ Not affected

## Migration Checklist

### Pre-Migration

- [ ] Review Phase 3 changes (this document)
- [ ] Identify all ref struct usages in your codebase
- [ ] Create a backup or feature branch
- [ ] Ensure you have comprehensive tests

### Migration

- [ ] Update all object initializers to use constructors
- [ ] Remove any field mutation code
- [ ] Update any custom factory methods
- [ ] Fix compile errors

### Post-Migration

- [ ] Build project successfully
- [ ] Run all unit tests
- [ ] Run integration tests
- [ ] Verify performance (should be same or better)
- [ ] Update internal documentation if needed

## Automated Migration

### Find and Replace with Regex

For simple cases, you can use regex find/replace:

**Find** (regex):
```regex
new VarString8\s*\{\s*Length\s*=\s*(\w+)\s*,\s*VarData\s*=\s*([^}]+)\s*\}
```

**Replace**:
```
new VarString8($1, $2)
```

**Note**: Always review automated changes carefully. Complex expressions may need manual adjustment.

### Example Script

```csharp
// C# Interactive script for migration
using System.Text.RegularExpressions;

string MigrateFile(string content)
{
    // Simple pattern - adjust for your types
    var pattern = @"new VarString8\s*\{\s*Length\s*=\s*(\w+)\s*,\s*VarData\s*=\s*([^}]+)\s*\}";
    var replacement = "new VarString8($1, $2)";
    
    return Regex.Replace(content, pattern, replacement);
}
```

## Troubleshooting

### Compile Error: "Cannot use object initializer"

**Error**:
```
error CS8852: Init-only property or indexer cannot be assigned to -- except in an object initializer
```

**Solution**: Change to constructor syntax:
```csharp
// Before
new VarString8 { Length = 10, VarData = data }

// After  
new VarString8(10, data)
```

### Compile Error: "Cannot assign to readonly field"

**Error**:
```
error CS0191: A readonly field cannot be assigned to (except in a constructor)
```

**Solution**: Create a new instance instead of mutating:
```csharp
// Before
str.Length = newLength;

// After
str = new VarString8(newLength, str.VarData);
```

### Wrong Constructor Parameters

**Error**:
```
error CS1503: Argument 1: cannot convert from 'ReadOnlySpan<byte>' to 'byte'
```

**Solution**: Check parameter order matches field declaration order:
```csharp
// Correct order: (Length, VarData)
var str = new VarString8(length, data);  // ✅

// Wrong order
var str = new VarString8(data, length);  // ❌
```

## Impact Assessment

### Estimated Migration Effort

| Codebase Size | Ref Struct Usage | Estimated Effort |
|---------------|------------------|------------------|
| Small (< 10K LOC) | Low | 1-2 hours |
| Medium (10-50K LOC) | Medium | 2-4 hours |
| Large (50-100K LOC) | High | 4-8 hours |
| Very Large (100K+ LOC) | High | 1-2 days |

**Factors**:
- Number of ref struct types in your schema
- Frequency of manual instantiation (vs factory methods)
- Test coverage (better coverage = faster verification)

### Risk Level: Low ✅

- ✅ **Compile-time detection** - All issues caught at build time
- ✅ **Mechanical changes** - Simple find/replace patterns
- ✅ **Good tooling support** - IDE refactoring can help
- ✅ **No runtime surprises** - No behavioral changes

## Benefits After Migration

### Immediate Benefits

1. **Type Safety** - Prevents accidental buffer corruption
2. **Better Errors** - Compile-time instead of runtime errors
3. **Performance** - Potential compiler optimizations
4. **Consistency** - Aligns with C# readonly semantics

### Long-Term Benefits

1. **Maintainability** - Clearer immutability intent
2. **Reliability** - Fewer mutation bugs
3. **Performance** - Better defensive copy elimination
4. **Future-Proof** - Foundation for further enhancements

## Examples from Real Code

### Example 1: Message Parsing

**Before**:
```csharp
public void ParseMessage(ReadOnlySpan<byte> buffer)
{
    var header = new VarString8 
    { 
        Length = buffer[0], 
        VarData = buffer.Slice(1, buffer[0]) 
    };
    
    ProcessHeader(header);
}
```

**After**:
```csharp
public void ParseMessage(ReadOnlySpan<byte> buffer)
{
    var header = new VarString8(
        buffer[0], 
        buffer.Slice(1, buffer[0])
    );
    
    ProcessHeader(header);
}
```

### Example 2: Message Building

**Before**:
```csharp
public VarString8 CreateSymbol(string symbol)
{
    var bytes = Encoding.UTF8.GetBytes(symbol);
    return new VarString8 
    { 
        Length = (byte)bytes.Length, 
        VarData = bytes 
    };
}
```

**After**:
```csharp
public VarString8 CreateSymbol(string symbol)
{
    var bytes = Encoding.UTF8.GetBytes(symbol);
    return new VarString8((byte)bytes.Length, bytes);
}
```

### Example 3: Using Factory (No Change)

**Before and After (unchanged)**:
```csharp
public void ProcessData(ReadOnlySpan<byte> buffer)
{
    // Factory method continues to work
    var data = VarString8.Create(buffer);
    ProcessString(data);
}
```

## Related Documentation

- [PHASE3_IMPLEMENTATION.md](./PHASE3_IMPLEMENTATION.md) - Technical implementation details
- [PHASE3_SUMMARY.md](./PHASE3_SUMMARY.md) - Executive summary
- [PHASE2_SUMMARY.md](./PHASE2_SUMMARY.md) - Phase 2 recommendations
- [MIGRATION_GUIDE_PHASE1.md](./MIGRATION_GUIDE_PHASE1.md) - Phase 1 migration (TypeDefinition)

## Support

If you encounter issues not covered in this guide:

1. Check compile errors carefully - they usually point to the exact problem
2. Review the examples in this guide
3. Consult [PHASE3_IMPLEMENTATION.md](./PHASE3_IMPLEMENTATION.md) for technical details
4. Open an issue on the repository for assistance

## Version Information

- **Phase**: 3
- **Option**: Option 1 (Readonly Ref Structs)
- **Breaking**: Yes (compile-time only)
- **Status**: ✅ Implemented
