# Validation Patterns - Design Discussion

This document discusses the three validation patterns now available in the SBE Source Generator, their use cases, and recommendations for when to use each.

## Overview

The SBE Source Generator now supports three complementary validation patterns:

1. **Validate()** - Traditional throwing validation
2. **TryValidate()** - Non-throwing validation with error messages
3. **CreateValidated()** - Factory method pattern with validation

All three patterns are generated for types and messages with `minValue` and `maxValue` constraints.

## Pattern Comparison

### 1. Validate() - Throwing Validation

**Signature:**
```csharp
public static void Validate(this Order message)
```

**Behavior:**
- Throws `ArgumentOutOfRangeException` on validation failure
- No return value
- Follows traditional .NET validation pattern

**Use Cases:**
- Fail-fast scenarios where invalid data is exceptional
- Input validation at system boundaries
- When exceptions are the appropriate error handling mechanism
- When you need a simple, straightforward validation approach

**Example:**
```csharp
var order = new Order { Price = -100 };
order.Validate(); // Throws ArgumentOutOfRangeException
```

**Pros:**
- Simple and familiar API
- Follows .NET framework conventions
- Clear error messages with exception details
- Stack trace available for debugging

**Cons:**
- Exception overhead (performance)
- Not suitable for high-frequency validation
- Requires try-catch for graceful error handling

### 2. TryValidate() - Non-Throwing Validation

**Signature:**
```csharp
public static bool TryValidate(this Order message, out string? errorMessage)
```

**Behavior:**
- Returns `true` if valid, `false` if invalid
- Provides detailed error message via `out` parameter
- No exceptions thrown
- Follows .NET's "Try" pattern (like `int.TryParse`)

**Use Cases:**
- User input validation in interactive applications
- High-frequency validation scenarios
- When you need detailed error messages for user feedback
- Performance-critical paths where exceptions are costly
- API input validation with graceful error responses

**Example:**
```csharp
var order = new Order { Price = -100 };
if (!order.TryValidate(out string? error))
{
    return BadRequest(error); // Return user-friendly error
}
```

**Pros:**
- No exception overhead
- User-friendly error messages
- Follows familiar .NET "Try" pattern
- Better performance for frequent validation
- Easier to use in fluent/functional code

**Cons:**
- Slightly more verbose (requires `out` parameter)
- Caller must check return value
- No stack trace for debugging

### 3. CreateValidated() - Factory Pattern

**Signature:**
```csharp
public static Order CreateValidated(this Order message)
```

**Behavior:**
- Calls `Validate()` internally
- Returns the validated object
- Throws exception if validation fails
- Enables fluent/chained usage

**Use Cases:**
- Fluent object initialization
- Ensuring objects are always valid after creation
- Builder pattern implementations
- When you want validation as part of object construction
- Declarative, readable code style

**Example:**
```csharp
var order = new Order 
{ 
    Price = 100000,
    Quantity = 10
}.CreateValidated(); // Throws if invalid, otherwise returns order
```

**Pros:**
- Fluent, declarative syntax
- Ensures objects are validated immediately
- Self-documenting code
- Works well with object initializers
- Can be chained with other operations

**Cons:**
- Still throws exceptions (same overhead as Validate())
- Less flexible than separate validation
- May be redundant if Validate() is already called

## Recommendations

### When to Use Each Pattern

#### Use **Validate()** when:
- ✅ Working with external input that should be validated at boundaries
- ✅ Validation failures are truly exceptional cases
- ✅ You want simple, straightforward validation
- ✅ You're already using try-catch for error handling
- ✅ Debugging information (stack traces) is important

#### Use **TryValidate()** when:
- ✅ Validating user input in web APIs or UI applications
- ✅ You need detailed, user-friendly error messages
- ✅ Performance matters (high-frequency validation)
- ✅ You want to avoid exception overhead
- ✅ Building validation chains or composing validators
- ✅ Implementing conditional logic based on validation results

#### Use **CreateValidated()** when:
- ✅ Building objects using fluent patterns
- ✅ You want to guarantee objects are validated at creation
- ✅ Working with builder or factory patterns
- ✅ Prefer declarative, self-documenting code
- ✅ Chaining operations on newly created objects

### Performance Considerations

| Pattern | Overhead | Best For |
|---------|----------|----------|
| `Validate()` | Medium (exception throwing) | Infrequent validation, exceptional cases |
| `TryValidate()` | Low (no exceptions) | High-frequency validation, user input |
| `CreateValidated()` | Medium (uses Validate()) | Object creation, builder patterns |

### Code Style Recommendations

**Defensive Programming (Validate at Boundaries):**
```csharp
public void ProcessOrder(Order order)
{
    order.Validate(); // Fail fast on invalid input
    // ... processing logic
}
```

**User-Friendly Error Handling (Web APIs):**
```csharp
public IActionResult CreateOrder([FromBody] Order order)
{
    if (!order.TryValidate(out string? error))
    {
        return BadRequest(new { error });
    }
    // ... create order
    return Ok();
}
```

**Fluent Object Construction (Builders):**
```csharp
public class OrderBuilder
{
    public Order Build()
    {
        return new Order
        {
            Price = _price,
            Quantity = _quantity
        }.CreateValidated();
    }
}
```

## Combining Patterns

All three patterns can be used together in the same codebase:

```csharp
public class OrderService
{
    // Use TryValidate for user input
    public Result<Order> CreateOrder(OrderRequest request)
    {
        var order = new Order
        {
            Price = request.Price,
            Quantity = request.Quantity
        };
        
        if (!order.TryValidate(out string? error))
        {
            return Result.Failure<Order>(error);
        }
        
        return Result.Success(order);
    }
    
    // Use Validate for internal processing
    public void ProcessInternalOrder(Order order)
    {
        order.Validate(); // Should never fail, so throw if it does
        _repository.Save(order);
    }
    
    // Use CreateValidated in builders
    public Order BuildOrder(long price, long quantity)
    {
        return new Order { Price = price, Quantity = quantity }
            .CreateValidated();
    }
}
```

## Migration Strategy

For existing code using `Validate()`:

1. **No Breaking Changes**: All existing code continues to work
2. **Gradual Adoption**: Introduce `TryValidate()` for new features
3. **Refactor Incrementally**: Replace exception-heavy validation with `TryValidate()` where appropriate
4. **Performance Optimization**: Use `TryValidate()` in hot paths

## Future Enhancements

Potential future additions to validation patterns:

1. **Async Validation**: `Task<bool> TryValidateAsync()` for async validation rules
2. **Validation Collections**: Collect multiple validation errors
3. **Custom Validators**: Extension point for custom validation logic
4. **Validation Context**: Pass context information to validators
5. **Conditional Validation**: Rules that apply based on conditions

## Conclusion

The three validation patterns provide flexibility for different scenarios:

- **Validate()** for traditional, exception-based validation
- **TryValidate()** for performance-sensitive, user-facing scenarios
- **CreateValidated()** for fluent, declarative object creation

Choose the pattern that best fits your use case, and feel free to mix them in the same codebase. All patterns maintain backward compatibility and add no overhead unless explicitly used.
