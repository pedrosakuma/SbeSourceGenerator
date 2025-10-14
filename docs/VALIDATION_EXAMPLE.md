# Validation Constraints - Quick Example

This example demonstrates the validation constraints feature in action.

## Schema Definition

```xml
<?xml version="1.0" encoding="UTF-8"?>
<sbe:messageSchema xmlns:sbe="http://fixprotocol.io/2016/sbe"
                   package="trading_example"
                   id="1"
                   version="0">
    <types>
        <type name="Price" primitiveType="int64" minValue="0" maxValue="999999999"/>
        <type name="Quantity" primitiveType="int64" minValue="1"/>
    </types>

    <sbe:message name="Order" id="1">
        <field name="orderId" id="1" type="uint64"/>
        <field name="price" id="2" type="int64" minValue="0" maxValue="999999999"/>
        <field name="quantity" id="3" type="int64" minValue="1"/>
    </sbe:message>
</sbe:messageSchema>
```

## Generated Validation Code

### Price Type Validation

```csharp
namespace Trading.Example;

public static class PriceValidation
{
    public static void Validate(this Price value)
    {
        if (value.Value < 0 || value.Value > 999999999)
            throw new ArgumentOutOfRangeException(nameof(value), value.Value, 
                "Price must be between 0 and 999999999");
    }
}
```

### Order Message Validation

```csharp
namespace Trading.Example;

public static class OrderValidation
{
    public static void Validate(this Order message)
    {
        if (message.Price < 0 || message.Price > 999999999)
            throw new ArgumentOutOfRangeException(nameof(message.Price), 
                message.Price, "Price must be between 0 and 999999999");
        
        if (message.Quantity < 1)
            throw new ArgumentOutOfRangeException(nameof(message.Quantity), 
                message.Quantity, "Quantity must be greater than or equal to 1");
    }
}
```

## Usage Examples

### Throwing Validation (Traditional) ✅

```csharp
// Valid type
var price = new Price { Value = 100000 };
price.Validate(); // ✅ Passes

// Valid message
var order = new Order 
{
    OrderId = 12345,
    Price = 100000,
    Quantity = 100
};
order.Validate(); // ✅ Passes
```

### Non-Throwing Validation (TryValidate Pattern) ✅

```csharp
// Check validity without exceptions
var price = new Price { Value = -100 };
if (!price.TryValidate(out string? errorMessage))
{
    Console.WriteLine($"Invalid price: {errorMessage}");
    // Output: Invalid price: Price must be between 0 and 999999999. Actual value was -100.
    return;
}

// Validate order with user-friendly error messages
var order = new Order 
{
    OrderId = 12345,
    Price = -1000,
    Quantity = 100
};

if (order.TryValidate(out errorMessage))
{
    Console.WriteLine("Order is valid!");
}
else
{
    Console.WriteLine($"Order validation failed: {errorMessage}");
    // Output: Order validation failed: Price must be between 0 and 999999999. Actual value was -1000.
}
```

### Factory Method Pattern (CreateValidated) ✅

```csharp
// Create and validate in one fluent step
var validPrice = new Price { Value = 100000 }.CreateValidated();

// Use with object initializer syntax
var order = new Order 
{
    OrderId = 12345,
    Price = 100000,
    Quantity = 100
}.CreateValidated(); // Throws if invalid

// This pattern ensures the object is always valid
try
{
    var invalidOrder = new Order 
    { 
        OrderId = 1, 
        Price = -100,  // Invalid
        Quantity = 10 
    }.CreateValidated();
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Cannot create invalid order: {ex.Message}");
}
```

### Invalid Values with Throwing Validation ❌

```csharp
// Invalid: negative price
var invalidPrice = new Price { Value = -100 };
invalidPrice.Validate(); // ❌ Throws ArgumentOutOfRangeException

// Invalid: price too high
var tooHighPrice = new Price { Value = 1000000000 };
tooHighPrice.Validate(); // ❌ Throws ArgumentOutOfRangeException

// Invalid: zero quantity
var invalidOrder = new Order 
{
    OrderId = 12345,
    Price = 100000,
    Quantity = 0  // Invalid
};
invalidOrder.Validate(); // ❌ Throws ArgumentOutOfRangeException
```

## Error Handling

```csharp
try
{
    var order = new Order 
    {
        OrderId = 12345,
        Price = -1000,  // Invalid
        Quantity = 100
    };
    
    order.Validate();
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
    // Output: Validation failed: Price must be between 0 and 999999999
    //         Actual value was -1000.
}
```

## Integration with Application Logic

### Using Throwing Validation

```csharp
public class OrderProcessor
{
    public void ProcessOrder(Order order)
    {
        // Validate before processing - throws on error
        order.Validate();
        
        // Process the validated order
        Console.WriteLine($"Processing order {order.OrderId}");
        Console.WriteLine($"Price: {order.Price}, Quantity: {order.Quantity}");
    }
}

// Usage
var processor = new OrderProcessor();

// This will work
var validOrder = new Order 
{ 
    OrderId = 1, 
    Price = 50000, 
    Quantity = 10 
};
processor.ProcessOrder(validOrder); // ✅ Success

// This will throw
var invalidOrder = new Order 
{ 
    OrderId = 2, 
    Price = -100,  // Invalid
    Quantity = 10 
};
processor.ProcessOrder(invalidOrder); // ❌ Throws
```

### Using TryValidate Pattern

```csharp
public class OrderValidator
{
    public bool TryProcessOrder(Order order, out string? error)
    {
        // Non-throwing validation
        if (!order.TryValidate(out error))
        {
            return false;
        }
        
        // Process the validated order
        Console.WriteLine($"Processing order {order.OrderId}");
        Console.WriteLine($"Price: {order.Price}, Quantity: {order.Quantity}");
        return true;
    }
}

// Usage with user-friendly error handling
var validator = new OrderValidator();
var order = new Order 
{ 
    OrderId = 1, 
    Price = -100,  // Invalid
    Quantity = 10 
};

if (!validator.TryProcessOrder(order, out string? error))
{
    Console.WriteLine($"Cannot process order: {error}");
    // Output: Cannot process order: Price must be between 0 and 999999999. Actual value was -100.
}
```

### Using CreateValidated Pattern

```csharp
public class OrderBuilder
{
    public Order BuildValidatedOrder(long orderId, long price, long quantity)
    {
        // Factory pattern - ensures valid object creation
        return new Order
        {
            OrderId = orderId,
            Price = price,
            Quantity = quantity
        }.CreateValidated(); // Throws immediately if invalid
    }
}

// Usage
var builder = new OrderBuilder();

try
{
    var order = builder.BuildValidatedOrder(1, 50000, 10);
    Console.WriteLine("Order created successfully!");
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Failed to create order: {ex.Message}");
}
```

## Performance Note

Validation is **opt-in** - there is no performance overhead unless you explicitly call `.Validate()`. This allows you to:

- Skip validation in performance-critical paths
- Add validation only where needed (e.g., external API boundaries)
- Use conditional compilation to exclude validation in release builds

```csharp
#if DEBUG
    order.Validate(); // Only validate in debug builds
#endif
```
