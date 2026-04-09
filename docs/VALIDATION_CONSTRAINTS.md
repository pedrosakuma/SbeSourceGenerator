# Validation Constraints Implementation

## Overview

The SBE Source Generator now supports validation constraints from SBE schemas. This feature automatically generates validation methods for types and messages that have `minValue` and `maxValue` constraints defined in the schema.

## Supported Constraints

### Min/Max Value Constraints

The generator supports the following validation constraints:

- **minValue**: Specifies the minimum allowed value for a numeric field or type
- **maxValue**: Specifies the maximum allowed value for a numeric field or type

These constraints can be applied to:
1. Type definitions in the `<types>` section
2. Field definitions in messages

## Schema Examples

### Type with Constraints

```xml
<types>
    <type name="Price" primitiveType="int64" minValue="0" maxValue="999999999"/>
    <type name="Quantity" primitiveType="int64" minValue="1"/>
</types>
```

### Message Fields with Constraints

```xml
<sbe:message name="Order" id="1">
    <field name="orderId" id="1" type="uint64"/>
    <field name="price" id="2" type="int64" minValue="0" maxValue="999999999"/>
    <field name="quantity" id="3" type="int64" minValue="1"/>
</sbe:message>
```

## Generated Code

For each type or message with validation constraints, the generator creates:

1. **Type Validation Extension Methods** - Static extension methods for types with constraints
2. **Message Validation Extension Methods** - Static extension methods for messages with field constraints

Each validation class includes three methods:
- **Validate()** - Throws exception on invalid values (traditional approach)
- **TryValidate()** - Non-throwing validation that returns bool with error message
- **CreateValidated()** - Factory method that validates and returns the value

### Example: Type Validation

For the `Price` type defined above, the generator creates:

```csharp
namespace YourNamespace;

/// <summary>
/// Validation extension methods for Price.
/// </summary>
public static class PriceValidation
{
    /// <summary>
    /// Validates that the value is within the schema-defined constraints.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is outside the valid range.</exception>
    public static void Validate(this Price value)
    {
        if (value.Value < 0 || value.Value > 999999999)
            throw new ArgumentOutOfRangeException(nameof(value), value.Value, "Price must be between 0 and 999999999");
    }

    /// <summary>
    /// Attempts to validate that the value is within the schema-defined constraints.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="errorMessage">The error message if validation fails, or null if validation succeeds.</param>
    /// <returns>True if validation succeeds, false otherwise.</returns>
    public static bool TryValidate(this Price value, out string? errorMessage)
    {
        if (value.Value < 0 || value.Value > 999999999)
        {
            errorMessage = $"Price must be between 0 and 999999999. Actual value was {value.Value}.";
            return false;
        }
        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Creates a validated instance of Price.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is outside the valid range.</exception>
    public static Price CreateValidated(this Price value)
    {
        value.Validate();
        return value;
    }
}
```

### Example: Message Validation

For the `Order` message defined above, the generator creates:

```csharp
namespace YourNamespace;

/// <summary>
/// Validation extension methods for Order.
/// </summary>
public static class OrderValidation
{
    /// <summary>
    /// Validates all fields with constraints in Order.
    /// </summary>
    /// <param name="message">The message to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a field value is outside its valid range.</exception>
    public static void Validate(this Order message)
    {
        if (message.Price < 0 || message.Price > 999999999)
            throw new ArgumentOutOfRangeException(nameof(message.Price), message.Price, "Price must be between 0 and 999999999");
        if (message.Quantity < 1)
            throw new ArgumentOutOfRangeException(nameof(message.Quantity), message.Quantity, "Quantity must be greater than or equal to 1");
    }

    /// <summary>
    /// Attempts to validate all fields with constraints in Order.
    /// </summary>
    /// <param name="message">The message to validate.</param>
    /// <param name="errorMessage">The error message if validation fails, or null if validation succeeds.</param>
    /// <returns>True if validation succeeds, false otherwise.</returns>
    public static bool TryValidate(this Order message, out string? errorMessage)
    {
        if (message.Price < 0 || message.Price > 999999999)
        {
            errorMessage = $"Price must be between 0 and 999999999. Actual value was {message.Price}.";
            return false;
        }
        if (message.Quantity < 1)
        {
            errorMessage = $"Quantity must be greater than or equal to 1. Actual value was {message.Quantity}.";
            return false;
        }
        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Creates a validated instance of Order.
    /// </summary>
    /// <param name="message">The message to validate.</param>
    /// <returns>The validated message.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a field value is outside its valid range.</exception>
    public static Order CreateValidated(this Order message)
    {
        message.Validate();
        return message;
    }
}
```

## Usage

### Validating Types

#### Throwing Validation (Traditional)

```csharp
Price price = new Price { Value = 1000 };
price.Validate(); // Passes

Price invalidPrice = new Price { Value = -100 };
invalidPrice.Validate(); // Throws ArgumentOutOfRangeException
```

#### Non-Throwing Validation (TryValidate Pattern)

```csharp
Price price = new Price { Value = -100 };
if (!price.TryValidate(out string? errorMessage))
{
    Console.WriteLine($"Validation failed: {errorMessage}");
    // Output: Validation failed: Price must be between 0 and 999999999. Actual value was -100.
}

Price validPrice = new Price { Value = 1000 };
if (validPrice.TryValidate(out errorMessage))
{
    Console.WriteLine("Validation passed!");
}
```

#### Factory Method Pattern

```csharp
// Create and validate in one step
var price = new Price { Value = 1000 }.CreateValidated();

// This will throw if validation fails
var invalidPrice = new Price { Value = -100 }.CreateValidated(); // Throws ArgumentOutOfRangeException
```

### Validating Messages

#### Throwing Validation (Traditional)

```csharp
Order order = new Order 
{ 
    OrderId = 123,
    Price = 10000,
    Quantity = 100
};
order.Validate(); // Passes

Order invalidOrder = new Order 
{ 
    OrderId = 123,
    Price = -100,  // Invalid: below minValue
    Quantity = 0   // Invalid: below minValue
};
invalidOrder.Validate(); // Throws ArgumentOutOfRangeException
```

#### Non-Throwing Validation (TryValidate Pattern)

```csharp
Order order = new Order 
{ 
    OrderId = 123,
    Price = -100,
    Quantity = 100
};

if (!order.TryValidate(out string? errorMessage))
{
    Console.WriteLine($"Order validation failed: {errorMessage}");
    // Output: Order validation failed: Price must be between 0 and 999999999. Actual value was -100.
}
```

#### Factory Method Pattern

```csharp
// Create and validate in one step
var order = new Order 
{ 
    OrderId = 123,
    Price = 10000,
    Quantity = 100
}.CreateValidated();

// Fluent-style usage
var validatedOrder = new Order()
    .CreateValidated(); // Ensures validation on creation
```

### Choosing the Right Validation Pattern

**Use `Validate()` (throwing) when:**
- You want to fail fast on invalid data
- Validation failures are exceptional cases
- You're in a context where exceptions are appropriate (e.g., parsing external input)

**Use `TryValidate()` (non-throwing) when:**
- You want to handle validation errors gracefully
- You need detailed error messages for user feedback
- Performance is critical and you want to avoid exception overhead
- You're validating user input in interactive scenarios

**Use `CreateValidated()` (factory) when:**
- You want to ensure objects are always valid after construction
- You prefer a fluent, declarative style
- You want to validate immediately after object initialization

## Validation Behavior

- **Min-only constraints**: Validates that the value is greater than or equal to the minimum
- **Max-only constraints**: Validates that the value is less than or equal to the maximum
- **Min and Max constraints**: Validates that the value is within the inclusive range
- **No constraints**: No validation code is generated

## Error Messages

Validation failures throw `ArgumentOutOfRangeException` with descriptive messages:

- Min-only: `"{FieldName} must be greater than or equal to {minValue}"`
- Max-only: `"{FieldName} must be less than or equal to {maxValue}"`
- Min and Max: `"{FieldName} must be between {minValue} and {maxValue}"`

## Performance Considerations

- Validation is **opt-in**: You must explicitly call `.Validate()` on types or messages
- No runtime overhead if validation is not used
- Validation methods are extension methods, so they can be easily excluded from release builds using conditional compilation if needed

## Implementation Details

### Schema Parsing

The generator parses the following attributes from XML schema:
- `minValue`: Extracted from `<type>` and `<field>` elements
- `maxValue`: Extracted from `<type>` and `<field>` elements

### Code Generation

The `ValidationGenerator` class:
1. Scans all type and message definitions
2. Identifies fields/types with min/max constraints
3. Generates static extension methods in the `Validation` namespace folder
4. Creates appropriate validation logic based on constraint combinations

## Testing

The implementation includes comprehensive tests:

### Unit Tests (`ValidationGeneratorTests.cs`)
- Validates generation of validation code for types with min/max constraints
- Verifies no code is generated for types without constraints
- Tests different constraint combinations (min-only, max-only, both)
- Validates message validation generation for multiple fields

### Integration Tests (`GeneratorIntegrationTests.cs`)
- End-to-end validation of generated code
- Verifies that validation methods throw appropriate exceptions
- Tests valid and invalid values
- Confirms error messages are correct

## Future Enhancements

Potential future additions to validation support:

1. **Valid Value Ranges**: Support for validValue elements in enums
2. **Character Set Validation**: Validate string encodings and character sets
3. **Length Constraints**: Validate string and array lengths
4. **Custom Validation**: Allow custom validation logic via attributes
5. **Validation Modes**: Optional strict/lenient validation modes

## Migration Guide

For existing codebases:

1. **No Breaking Changes**: Validation is opt-in, existing code continues to work
2. **Gradual Adoption**: Add `.Validate()` calls where needed
3. **Schema Updates**: Add min/max attributes to schemas as needed
4. **Error Handling**: Add try-catch blocks where validation is performed

## See Also

- [SBE Implementation Roadmap](./SBE_IMPLEMENTATION_ROADMAP.md) - Phase 2.1: Validation Constraints
- [SBE Feature Completeness](./SBE_FEATURE_COMPLETENESS.md) - Feature #14: Validation Constraints
