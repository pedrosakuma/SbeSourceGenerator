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

### Valid Values ✅

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

### Invalid Values ❌

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

```csharp
public class OrderProcessor
{
    public void ProcessOrder(Order order)
    {
        // Validate before processing
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
