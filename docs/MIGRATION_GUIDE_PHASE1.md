# Migration Guide: Phase 1 - Readonly TypeDefinition with Constructors and Conversions

## Overview

Phase 1 introduces improved type safety and usability for SBE TypeDefinition types. This is a **breaking change** that requires updates to existing code, but the migration is straightforward and results in cleaner, more maintainable code.

## What Changed

### Before Phase 1

```csharp
// Generated code (before)
public partial struct OrderId
{
    public long Value;
}

// Usage (before)
var orderId = new OrderId { Value = 123456 };
message.OrderId = new OrderId { Value = 789012 };
```

### After Phase 1

```csharp
// Generated code (after)
public readonly partial struct OrderId
{
    public readonly long Value;
    
    public OrderId(long value)
    {
        Value = value;
    }
    
    public static implicit operator OrderId(long value) => new OrderId(value);
    public static explicit operator long(OrderId value) => value.Value;
}

// Usage (after) - Choose your preferred style
var orderId = new OrderId(123456);  // Constructor
OrderId orderId2 = 789012;          // Implicit conversion
message.OrderId = 789012;           // Implicit conversion
```

## Breaking Changes

### ❌ Object Initializers No Longer Work

**Old code that will break:**
```csharp
var orderId = new OrderId { Value = 123 };
var price = new Price { Value = 9950 };
```

**Error message:**
```
CS0191: A readonly field cannot be assigned to (except in a constructor 
or init-only setter of the type in which the field is defined)
```

## Migration Steps

You have **three migration options**. Choose the one that best fits your coding style:

### Option 1: Use Constructors (Recommended for explicit code)

```csharp
// Before
var orderId = new OrderId { Value = 123456 };

// After
var orderId = new OrderId(123456);
```

**Pros:**
- Explicit and clear
- Familiar pattern for most C# developers
- Easy to find/replace across codebase

**When to use:**
- When clarity is important
- In public APIs where explicit is better
- When migrating existing code with find/replace

### Option 2: Use Implicit Conversions (Recommended for concise code)

```csharp
// Before
var orderId = new OrderId { Value = 123456 };

// After
OrderId orderId = 123456;
```

**Pros:**
- Most concise
- Reduces boilerplate
- Feels natural after short adaptation

**When to use:**
- In test code
- In internal implementation code
- When brevity improves readability

### Option 3: Mixed Approach (Recommended for teams)

```csharp
// Use constructor for variable declarations
var orderId = new OrderId(123456);
var price = new Price(9950);

// Use implicit conversion for assignments and parameters
message.OrderId = 123456;
message.Price = 9950;
ProcessOrder(789012, 10050);  // Instead of ProcessOrder(new OrderId(789012), new Price(10050))
```

**Pros:**
- Best of both worlds
- Explicit where it matters (declarations)
- Concise where it helps (assignments)

**When to use:**
- In production code
- When balancing clarity and conciseness
- For team consistency

## Common Migration Patterns

### Pattern 1: Message Field Assignment

```csharp
// Before
ref var message = ref MemoryMarshal.AsRef<NewOrderData>(buffer);
message.OrderId = new OrderId { Value = 123 };
message.Price = new Price { Value = 9950 };

// After (implicit conversion)
ref var message = ref MemoryMarshal.AsRef<NewOrderData>(buffer);
message.OrderId = 123;
message.Price = 9950;

// Or (constructor)
message.OrderId = new OrderId(123);
message.Price = new Price(9950);
```

### Pattern 2: Variable Declaration

```csharp
// Before
var orderId = new OrderId { Value = 123 };

// After (constructor - recommended)
var orderId = new OrderId(123);

// Or (implicit conversion - requires type annotation)
OrderId orderId = 123;
```

### Pattern 3: Method Parameters

```csharp
void ProcessOrder(OrderId orderId, Price price) { ... }

// Before
ProcessOrder(new OrderId { Value = 123 }, new Price { Value = 9950 });

// After (implicit conversion - cleanest)
ProcessOrder(123, 9950);

// Or (constructor)
ProcessOrder(new OrderId(123), new Price(9950));
```

### Pattern 4: Collections

```csharp
// Before
var orderIds = new List<OrderId>
{
    new OrderId { Value = 1 },
    new OrderId { Value = 2 },
    new OrderId { Value = 3 }
};

// After (implicit conversion - cleanest)
var orderIds = new List<OrderId> { 1, 2, 3 };

// Or (constructor)
var orderIds = new List<OrderId>
{
    new OrderId(1),
    new OrderId(2),
    new OrderId(3)
};
```

### Pattern 5: Extracting Primitive Value

```csharp
// Before
long rawValue = orderId.Value;

// After (explicit conversion)
long rawValue = (long)orderId;

// Or (still works - access Value property)
long rawValue = orderId.Value;
```

## Automated Migration

### Using Find/Replace in Visual Studio / VS Code

1. **Find:** `new OrderId { Value = ([0-9]+) }`
2. **Replace:** `new OrderId($1)`

Repeat for each type (Price, Quantity, etc.).

### Using Regex for Multiple Types

**Find (regex):**
```
new ([A-Za-z]+) { Value = ([^}]+) }
```

**Replace:**
```
new $1($2)
```

This works for all TypeDefinition types at once.

## What Types Are Affected?

Phase 1 **only affects TypeDefinition types** - simple wrapper types around primitives defined in your SBE schema:

✅ **Affected** (need migration):
- Custom type wrappers like `OrderId`, `Price`, `Quantity`
- Any `<type>` element in your schema that wraps a primitive

❌ **NOT affected** (no changes needed):
- Enums (`<enum>`)
- Composites (`<composite>`)
- Messages
- Optional types
- Set types

## Benefits After Migration

### 1. Type Safety

```csharp
// Before: Easy to mix up values
long orderId = 123;
long price = 456;
message.OrderId = new OrderId { Value = price };  // Oops! Wrong value

// After: Type safety with implicit conversion
OrderId orderId = 123;
Price price = 456;
message.OrderId = price;  // Compile error! Type mismatch
```

### 2. Immutability

```csharp
// Before: Accidental mutation possible
var orderId = new OrderId { Value = 123 };
orderId.Value = 456;  // Whoops, changed it!

// After: Compile-time prevention
var orderId = new OrderId(123);
orderId.Value = 456;  // Compile error! Field is readonly
```

### 3. Better Performance

```csharp
// Readonly structs eliminate defensive copies
void ProcessOrder(in OrderId orderId)  // 'in' parameter
{
    // Before: Compiler creates defensive copy when accessing Value
    // After: No defensive copy needed - struct is readonly
    var value = orderId.Value;
}
```

### 4. Cleaner Code

```csharp
// Before (verbose)
ProcessOrder(
    new OrderId { Value = 123456 },
    new Price { Value = 9950 },
    new Quantity { Value = 100 }
);

// After (concise with implicit conversion)
ProcessOrder(123456, 9950, 100);
```

## Troubleshooting

### Issue: "Cannot use object initializer"

**Symptom:**
```csharp
var orderId = new OrderId { Value = 123 };  // Error CS0191
```

**Solution:**
Use constructor or implicit conversion:
```csharp
var orderId = new OrderId(123);  // Constructor
OrderId orderId = 123;           // Implicit conversion
```

### Issue: "Cannot implicitly convert"

**Symptom:**
```csharp
long value = orderId;  // Error CS0266
```

**Solution:**
Use explicit conversion:
```csharp
long value = (long)orderId;  // Explicit cast
// Or access the property
long value = orderId.Value;
```

### Issue: "Cannot assign to readonly field"

**Symptom:**
```csharp
var orderId = new OrderId(123);
orderId.Value = 456;  // Error CS0191
```

**Solution:**
Create a new instance instead:
```csharp
orderId = new OrderId(456);  // Replace entire struct
// Or use implicit conversion
orderId = 456;
```

## Testing Your Migration

After migrating, verify:

1. **Code compiles** without CS0191 errors
2. **Tests pass** - especially serialization/deserialization tests
3. **No behavioral changes** - same values in messages before/after

Example test:
```csharp
[Fact]
public void Migration_ProducesSameResults()
{
    // Old way (commented out, won't compile)
    // var orderOld = new OrderId { Value = 123 };
    
    // New way
    var orderNew = new OrderId(123);
    OrderId orderImplicit = 123;
    
    // All should have same value
    Assert.Equal(123, orderNew.Value);
    Assert.Equal(123, orderImplicit.Value);
    Assert.Equal((long)orderNew, (long)orderImplicit);
}
```

## Need Help?

- Review [Phase 1 Implementation Documentation](./PHASE1_IMPLEMENTATION.md)
- Check [Feasibility Study](./FEASIBILITY_STUDY_AUTO_CONSTRUCTORS_READONLY_CONVERSIONS.md) for design rationale
- See [Integration Tests](../tests/SbeCodeGenerator.IntegrationTests/GeneratorIntegrationTests.cs) for examples

## Summary

**Migration Checklist:**

- [ ] Identify all TypeDefinition types in your schema (OrderId, Price, etc.)
- [ ] Replace object initializers with constructors or implicit conversions
- [ ] Update method calls to use implicit conversions where beneficial
- [ ] Replace `value.Value` with `(primitiveType)value` where preferred
- [ ] Run tests to verify no behavioral changes
- [ ] Update team coding guidelines with preferred patterns

**Remember:** This migration makes your code safer, more maintainable, and often more concise. The effort is worthwhile!
