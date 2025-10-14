# Validation Patterns - Practical Examples

This document provides practical, real-world examples of using all three validation patterns together in a typical application.

## Example: Order Processing System

This example demonstrates how to use all three validation patterns effectively in a web API scenario.

### Schema Definition

```xml
<?xml version="1.0" encoding="UTF-8"?>
<sbe:messageSchema xmlns:sbe="http://fixprotocol.io/2016/sbe"
                   package="trading"
                   id="1"
                   version="0">
    <types>
        <type name="Price" primitiveType="int64" minValue="0" maxValue="999999999"/>
        <type name="Quantity" primitiveType="int64" minValue="1" maxValue="1000000"/>
    </types>

    <sbe:message name="Order" id="1">
        <field name="orderId" id="1" type="uint64"/>
        <field name="price" id="2" type="Price"/>
        <field name="quantity" id="3" type="Quantity"/>
    </sbe:message>
</sbe:messageSchema>
```

### API Controller - Using TryValidate()

```csharp
using Microsoft.AspNetCore.Mvc;
using Trading;

namespace OrderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _repository;
        
        public OrdersController(IOrderRepository repository)
        {
            _repository = repository;
        }
        
        /// <summary>
        /// Create a new order with validation
        /// </summary>
        [HttpPost]
        public IActionResult CreateOrder([FromBody] OrderRequest request)
        {
            // Map request to SBE message
            var order = new Order
            {
                OrderId = request.OrderId,
                Price = request.Price,
                Quantity = request.Quantity
            };
            
            // Use TryValidate for user-friendly error responses
            if (!order.TryValidate(out string? errorMessage))
            {
                return BadRequest(new 
                { 
                    error = "Validation failed", 
                    message = errorMessage 
                });
            }
            
            // Save the validated order
            _repository.Save(order);
            
            return Ok(new { orderId = order.OrderId });
        }
    }
}
```

### Service Layer - Using Validate()

```csharp
namespace OrderApi.Services
{
    public class OrderProcessingService
    {
        private readonly IOrderRepository _repository;
        private readonly INotificationService _notifications;
        
        public OrderProcessingService(
            IOrderRepository repository, 
            INotificationService notifications)
        {
            _repository = repository;
            _notifications = notifications;
        }
        
        /// <summary>
        /// Process an order from the queue - validation should never fail here
        /// </summary>
        public void ProcessQueuedOrder(Order order)
        {
            // Use Validate() - if this fails, it's a bug/data corruption
            // and we want the exception to be logged
            order.Validate();
            
            // Process the order
            var result = ExecuteOrder(order);
            
            // Notify completion
            _notifications.SendOrderConfirmation(order.OrderId, result);
        }
        
        private OrderResult ExecuteOrder(Order order)
        {
            // Order processing logic
            return new OrderResult { Success = true };
        }
    }
}
```

### Builder Pattern - Using CreateValidated()

```csharp
namespace OrderApi.Builders
{
    public class OrderBuilder
    {
        private ulong _orderId;
        private long _price;
        private long _quantity;
        
        public OrderBuilder WithOrderId(ulong orderId)
        {
            _orderId = orderId;
            return this;
        }
        
        public OrderBuilder WithPrice(long price)
        {
            _price = price;
            return this;
        }
        
        public OrderBuilder WithQuantity(long quantity)
        {
            _quantity = quantity;
            return this;
        }
        
        /// <summary>
        /// Build and validate the order in one step
        /// </summary>
        public Order Build()
        {
            return new Order
            {
                OrderId = _orderId,
                Price = _price,
                Quantity = _quantity
            }.CreateValidated(); // Ensures the built order is always valid
        }
    }
    
    // Usage
    public class OrderService
    {
        public Order CreateMarketOrder(ulong orderId, long price, long quantity)
        {
            return new OrderBuilder()
                .WithOrderId(orderId)
                .WithPrice(price)
                .WithQuantity(quantity)
                .Build(); // Throws if validation fails
        }
    }
}
```

### Batch Processing - Combining All Patterns

```csharp
namespace OrderApi.BatchProcessing
{
    public class OrderBatchProcessor
    {
        public BatchResult ProcessOrders(List<OrderRequest> requests)
        {
            var result = new BatchResult();
            
            foreach (var request in requests)
            {
                var order = new Order
                {
                    OrderId = request.OrderId,
                    Price = request.Price,
                    Quantity = request.Quantity
                };
                
                // Use TryValidate for graceful batch processing
                if (!order.TryValidate(out string? errorMessage))
                {
                    result.Failed.Add(new FailedOrder
                    {
                        OrderId = request.OrderId,
                        Reason = errorMessage
                    });
                    continue;
                }
                
                try
                {
                    // Process valid orders
                    ProcessOrder(order);
                    result.Succeeded.Add(order.OrderId);
                }
                catch (Exception ex)
                {
                    result.Failed.Add(new FailedOrder
                    {
                        OrderId = request.OrderId,
                        Reason = ex.Message
                    });
                }
            }
            
            return result;
        }
        
        private void ProcessOrder(Order order)
        {
            // Double-check with Validate() before processing
            // This will throw if somehow an invalid order got through
            order.Validate();
            
            // Processing logic...
        }
    }
}
```

### Testing - All Three Patterns

```csharp
using Xunit;
using Trading;

namespace OrderApi.Tests
{
    public class OrderValidationTests
    {
        [Fact]
        public void Validate_ThrowsOnInvalidPrice()
        {
            // Arrange
            var order = new Order
            {
                OrderId = 1,
                Price = -100,  // Invalid
                Quantity = 10
            };
            
            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => order.Validate());
            Assert.Contains("Price", exception.Message);
        }
        
        [Fact]
        public void TryValidate_ReturnsFalseWithErrorMessage()
        {
            // Arrange
            var order = new Order
            {
                OrderId = 1,
                Price = -100,  // Invalid
                Quantity = 10
            };
            
            // Act
            var isValid = order.TryValidate(out string? errorMessage);
            
            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Price must be", errorMessage);
            Assert.Contains("-100", errorMessage);
        }
        
        [Fact]
        public void TryValidate_ReturnsTrueForValidOrder()
        {
            // Arrange
            var order = new Order
            {
                OrderId = 1,
                Price = 10000,
                Quantity = 100
            };
            
            // Act
            var isValid = order.TryValidate(out string? errorMessage);
            
            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }
        
        [Fact]
        public void CreateValidated_ReturnsOrderWhenValid()
        {
            // Arrange
            var order = new Order
            {
                OrderId = 1,
                Price = 10000,
                Quantity = 100
            };
            
            // Act
            var validatedOrder = order.CreateValidated();
            
            // Assert
            Assert.NotNull(validatedOrder);
            Assert.Equal(1ul, validatedOrder.OrderId);
            Assert.Equal(10000, validatedOrder.Price);
        }
        
        [Fact]
        public void CreateValidated_ThrowsOnInvalidOrder()
        {
            // Arrange
            var order = new Order
            {
                OrderId = 1,
                Price = -100,  // Invalid
                Quantity = 100
            };
            
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => order.CreateValidated());
        }
        
        [Fact]
        public void Builder_CreatesValidatedOrder()
        {
            // Arrange
            var builder = new OrderBuilder()
                .WithOrderId(1)
                .WithPrice(10000)
                .WithQuantity(100);
            
            // Act
            var order = builder.Build();
            
            // Assert
            Assert.NotNull(order);
            Assert.Equal(1ul, order.OrderId);
        }
        
        [Fact]
        public void Builder_ThrowsOnInvalidData()
        {
            // Arrange
            var builder = new OrderBuilder()
                .WithOrderId(1)
                .WithPrice(-100)  // Invalid
                .WithQuantity(100);
            
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Build());
        }
    }
}
```

## Performance Comparison

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Trading;

namespace OrderApi.Benchmarks
{
    [MemoryDiagnoser]
    public class ValidationBenchmarks
    {
        private Order _validOrder;
        private Order _invalidOrder;
        
        [GlobalSetup]
        public void Setup()
        {
            _validOrder = new Order
            {
                OrderId = 1,
                Price = 10000,
                Quantity = 100
            };
            
            _invalidOrder = new Order
            {
                OrderId = 1,
                Price = -100,
                Quantity = 100
            };
        }
        
        [Benchmark]
        public void Validate_ValidOrder()
        {
            _validOrder.Validate();
        }
        
        [Benchmark]
        public bool TryValidate_ValidOrder()
        {
            return _validOrder.TryValidate(out _);
        }
        
        [Benchmark]
        public Order CreateValidated_ValidOrder()
        {
            return _validOrder.CreateValidated();
        }
        
        [Benchmark]
        public bool TryValidate_InvalidOrder()
        {
            return _invalidOrder.TryValidate(out _);
        }
    }
}

// Expected Results:
// TryValidate is fastest (no exception overhead)
// Validate and CreateValidated have similar performance
// TryValidate on invalid order is much faster than catching exceptions
```

## Best Practices Summary

### 1. API Controllers - Use TryValidate()
- Provides user-friendly error messages
- No exception overhead
- Better for handling user input

### 2. Internal Services - Use Validate()
- Fail-fast on unexpected invalid data
- Exceptions indicate bugs or data corruption
- Stack traces help with debugging

### 3. Builders/Factories - Use CreateValidated()
- Ensures objects are always valid
- Fluent, readable code
- Self-documenting validation

### 4. Batch Processing - Combine All Patterns
- TryValidate() for graceful error handling
- Validate() as a safety check
- CreateValidated() in builders

### 5. Testing
- Test all three patterns
- Verify error messages
- Check performance characteristics

## Conclusion

The three validation patterns work together to provide:
- **Flexibility**: Choose the right pattern for each scenario
- **Performance**: TryValidate() for hot paths
- **Safety**: Validate() for critical checks
- **Usability**: CreateValidated() for clean code

Use them together to build robust, performant, and maintainable applications.
