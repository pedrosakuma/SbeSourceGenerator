# Migration Guide: Phase 2 Recommendations

> **⚠️ HISTORICAL DOCUMENT**  
> This document is now historical. The recommendations described here have been implemented in Phase 3.  
> **For current migration guidance, see [MIGRATION_GUIDE_PHASE3.md](./MIGRATION_GUIDE_PHASE3.md)**.

## Overview

This document provides migration guidance for potential future type system enhancements identified in Phase 2. **Note:** These features were recommendations that have now been implemented in Phase 3.

Phase 2 was a documentation and planning phase. It reviewed Phase 1 results and identified potential enhancements for future phases. Phase 3 subsequently implemented Option 1 (readonly ref structs with constructors).

## Current Status

- ✅ **Phase 1**: Completed - TypeDefinition enhancements implemented
- ✅ **Phase 2**: Completed - Documentation and planning
- ✅ **Phase 3**: Completed - Readonly ref structs implemented (was "Option 1" in Phase 2)

## Phase 1 Migration (Already Complete)

For migration from pre-Phase 1 to Phase 1, see [MIGRATION_GUIDE_PHASE1.md](./MIGRATION_GUIDE_PHASE1.md).

## Phase 3 Migration (Current)

For migration to Phase 3 (readonly ref structs), see [MIGRATION_GUIDE_PHASE3.md](./MIGRATION_GUIDE_PHASE3.md).

## Potential Future Migrations

### Option 1: Ref Struct Enhancements

If ref struct enhancements are implemented in a future phase:

#### What Would Change

**Current Code (Today):**
```csharp
public ref struct VarString8
{
    public byte Length;
    public ReadOnlySpan<byte> VarData;
    
    public static VarString8 Create(ReadOnlySpan<byte> buffer) => 
        new VarString8 { Length = buffer[0], VarData = buffer.Slice(1) };
}

// Usage
var varStr = new VarString8 { Length = 10, VarData = data };
```

**Future Code (If Implemented):**
```csharp
public readonly ref struct VarString8
{
    public readonly byte Length;
    public readonly ReadOnlySpan<byte> VarData;
    
    public VarString8(byte length, ReadOnlySpan<byte> varData)
    {
        Length = length;
        VarData = varData;
    }
    
    public static VarString8 Create(ReadOnlySpan<byte> buffer) => 
        new VarString8(buffer[0], buffer.Slice(1));
}

// Usage - BREAKING CHANGE
var varStr = new VarString8(10, data);  // Must use constructor
```

#### Migration Steps

1. **Find all ref struct initializations**
   ```bash
   # Find object initializers for ref structs
   grep -r "new VarString.*{" --include="*.cs"
   ```

2. **Replace with constructor calls**
   ```csharp
   // Before
   var str = new VarString8 { Length = len, VarData = data };
   
   // After
   var str = new VarString8(len, data);
   ```

3. **Update any field assignments**
   ```csharp
   // Before - won't compile with readonly
   varStr.Length = newLength;  // ERROR: readonly field
   
   // After - create new instance
   varStr = new VarString8(newLength, varStr.VarData);
   ```

#### Estimated Impact
- **Effort**: Low (simple find/replace)
- **Files affected**: Any code using ref structs
- **Breaking**: Yes (object initializers)
- **Workaround**: None (must migrate)

---

### Option 2: OptionalTypeDefinition Enhancements

If OptionalTypeDefinition enhancements are implemented in a future phase:

#### What Would Change

**Current Code (Today):**
```csharp
public partial struct OptionalOrderId
{
    private long value;
    public long? Value => value == NullValue ? null : value;
}

// Usage
var id = new OptionalOrderId { value = 123456 };  // Direct field access
if (id.Value.HasValue) { /* ... */ }
```

**Future Code (If Implemented):**
```csharp
public readonly partial struct OptionalOrderId
{
    public readonly long Value;  // Now public field, not property
    public const long NullValue = long.MinValue;
    
    public OptionalOrderId(long value)
    {
        Value = value;
    }
    
    // Implicit from primitive
    public static implicit operator OptionalOrderId(long value) => 
        new OptionalOrderId(value);
    
    // Implicit from nullable
    public static implicit operator OptionalOrderId(long? value) => 
        value.HasValue ? new OptionalOrderId(value.Value) : new OptionalOrderId(NullValue);
    
    // Explicit to nullable
    public static explicit operator long?(OptionalOrderId value) => 
        value.Value == NullValue ? null : (long?)value.Value;
    
    public bool IsNull() => Value == NullValue;
}

// Usage - BREAKING CHANGE
OptionalOrderId id = 123456;           // Implicit conversion
OptionalOrderId id2 = (long?)null;     // From nullable
long? value = (long?)id;               // To nullable
```

#### Migration Steps

1. **Update field access patterns**
   ```csharp
   // Before
   var id = new OptionalOrderId { value = 123 };  // Private field
   long? val = id.Value;  // Property returns nullable
   
   // After
   OptionalOrderId id = 123;  // Implicit conversion
   long? val = (long?)id;     // Explicit to nullable
   ```

2. **Update null checking**
   ```csharp
   // Before
   if (id.Value.HasValue) { long v = id.Value.Value; }
   
   // After
   if (!id.IsNull()) { long v = id.Value; }
   // Or
   long? val = (long?)id;
   if (val.HasValue) { /* ... */ }
   ```

3. **Update initialization patterns**
   ```csharp
   // Before
   message.OptionalId = new OptionalOrderId { value = 123 };
   
   // After
   message.OptionalId = 123;  // Implicit conversion
   // Or from nullable
   long? nullableValue = GetValue();
   message.OptionalId = nullableValue;  // Handles null automatically
   ```

#### Estimated Impact
- **Effort**: Medium (requires pattern changes)
- **Files affected**: Any code using optional types
- **Breaking**: Yes (field visibility and pattern changes)
- **Workaround**: None (significant refactoring needed)

---

### Option 3: Semantic Type Conversions

If semantic type conversions are implemented for specific types:

#### Example: LocalMktDate

**Current Code (Today):**
```csharp
public readonly partial struct LocalMktDate
{
    public readonly int Value;  // YYYYMMDD format
    
    public LocalMktDate(int value)
    {
        Value = value;
    }
    
    public static implicit operator LocalMktDate(int value) => new LocalMktDate(value);
    public static explicit operator int(LocalMktDate value) => value.Value;
}

// Usage - manual conversion
var date = new DateOnly(2024, 10, 14);
var sbeDate = new LocalMktDate(int.Parse(date.ToString("yyyyMMdd")));
```

**Future Code (If Implemented):**
```csharp
public readonly partial struct LocalMktDate
{
    public readonly int Value;
    
    public LocalMktDate(int value)
    {
        Value = value;
    }
    
    // Existing conversions
    public static implicit operator LocalMktDate(int value) => new LocalMktDate(value);
    public static explicit operator int(LocalMktDate value) => value.Value;
    
    // NEW: DateOnly conversions (.NET 6+)
    public static explicit operator DateOnly(LocalMktDate value) => 
        DateOnly.ParseExact(value.Value.ToString("D8"), "yyyyMMdd", CultureInfo.InvariantCulture);
    
    public static explicit operator LocalMktDate(DateOnly value) => 
        new LocalMktDate(int.Parse(value.ToString("yyyyMMdd")));
}

// Usage - direct conversion
var date = new DateOnly(2024, 10, 14);
var sbeDate = (LocalMktDate)date;  // Explicit conversion
var backToDate = (DateOnly)sbeDate;  // Explicit conversion
```

#### Migration Steps

1. **Optional migration** (conversions are additive)
   ```csharp
   // Old way still works
   var sbeDate = new LocalMktDate(20241014);
   
   // New way (optional)
   var sbeDate = (LocalMktDate)new DateOnly(2024, 10, 14);
   ```

2. **Update date handling code** (optional)
   ```csharp
   // Before
   DateOnly date = DateOnly.ParseExact(localMktDate.Value.ToString("D8"), "yyyyMMdd");
   
   // After
   DateOnly date = (DateOnly)localMktDate;
   ```

#### Estimated Impact
- **Effort**: None to Low (additive feature)
- **Files affected**: Optional (can choose to adopt)
- **Breaking**: No (backward compatible)
- **Workaround**: N/A (can ignore new conversions)

---

## Decision Points

### Should You Wait for These Features?

**If you need:**
- ✅ **TypeDefinition conversions** → Already available (Phase 1)
- ❓ **Ref struct constructors** → Phase 3 (under evaluation)
- ❓ **Optional type conversions** → Phase 3+ (under evaluation)
- ❓ **Semantic conversions** → Phase 3+ (under evaluation)

### How to Influence the Roadmap

1. **Provide feedback** on which features would be most valuable
2. **Share use cases** that would benefit from specific enhancements
3. **Participate in design discussions** if features are approved
4. **Test pre-release versions** when features are implemented

## Risk Assessment

| Feature | Breaking Change | Migration Effort | Benefit | Recommendation |
|---------|----------------|------------------|---------|----------------|
| Ref struct enhancements | Yes | Low | Medium | ✅ Consider |
| OptionalTypeDefinition | Yes | Medium | Medium | ⚠️ Needs design review |
| Semantic conversions | No | None | High | ✅ Evaluate per-type |

## Testing Strategy for Future Migrations

When these features are implemented:

1. **Comprehensive testing**
   - Unit tests for all conversion scenarios
   - Integration tests with real schemas
   - Performance benchmarks

2. **Migration validation**
   - Before/after comparison tests
   - Backward compatibility verification
   - Edge case testing

3. **Documentation**
   - Updated API docs
   - Migration examples
   - Breaking change announcements

## Resources

- [PHASE1_IMPLEMENTATION.md](./PHASE1_IMPLEMENTATION.md) - Implemented Phase 1 features
- [PHASE2_IMPLEMENTATION.md](./PHASE2_IMPLEMENTATION.md) - Phase 2 analysis and recommendations
- [PHASE2_SUMMARY.md](./PHASE2_SUMMARY.md) - Phase 2 executive summary
- [MIGRATION_GUIDE_PHASE1.md](./MIGRATION_GUIDE_PHASE1.md) - Phase 1 migration guide
- [FEASIBILITY_STUDY_AUTO_CONSTRUCTORS_READONLY_CONVERSIONS.md](./FEASIBILITY_STUDY_AUTO_CONSTRUCTORS_READONLY_CONVERSIONS.md) - Original feasibility study

## Getting Help

If these features are implemented and you need migration assistance:

1. **Check the documentation** - Migration guides will be updated
2. **Review examples** - Sample code will be provided
3. **Ask questions** - Use GitHub Discussions or Issues
4. **Request support** - Reach out to maintainers if needed

## Summary

This guide documents potential future migrations for type system enhancements identified in Phase 2:

- **Ref struct enhancements**: Low-effort migration, requires constructor usage
- **OptionalTypeDefinition**: Medium-effort migration, requires pattern changes
- **Semantic conversions**: No migration needed (additive features)

**Current Action**: These features are under evaluation. This guide will be updated if and when they are implemented.

---

**Version:** 1.0  
**Date:** 2025-10-14  
**Status:** Recommendations for Future Implementation
