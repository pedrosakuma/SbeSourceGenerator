# Example Schemas

This directory contains example SBE schemas that demonstrate various features of the SBE Code Generator, including multiple schema support and schema versioning.

## Example Schemas

### 1. example-trading-schema.xml
- **Schema ID**: 100
- **Version**: 1
- **Package**: example_trading
- **Description**: Demonstrates a trading system schema with order messages

**Features Demonstrated**:
- Schema metadata (id, version, semanticVersion)
- Field-level versioning with `sinceVersion` attribute
- Enums for order side
- Custom types (OrderId, Price)

**Generated Classes**:
- `Example.Trading.SchemaMetadata` - Schema metadata and version checking
- `Example.Trading.Order` - Order message (version 0)
- `Example.Trading.V1.Order` - Order message with quantity field (version 1)
- `Example.Trading.Side` - Enum for Buy/Sell

### 2. example-marketdata-schema.xml
- **Schema ID**: 200
- **Version**: 0
- **Package**: example_marketdata
- **Description**: Demonstrates a market data schema with quote messages

**Features Demonstrated**:
- Different schema ID for multi-schema environments
- Fixed-length char arrays for symbols
- Simple message structure without versioning

**Generated Classes**:
- `Example.Marketdata.SchemaMetadata` - Schema metadata and version checking
- `Example.Marketdata.Quote` - Market quote message

## Using These Schemas

### In Your Project

1. Add the schema files to your project:
```xml
<ItemGroup>
  <AdditionalFiles Include="example-trading-schema.xml" />
  <AdditionalFiles Include="example-marketdata-schema.xml" />
</ItemGroup>
```

2. Build your project - the generator will create all classes automatically.

3. Use the generated code:
```csharp
// Check schema compatibility
if (Example.Trading.SchemaMetadata.IsCompatible(schemaId, version))
{
    // Process trading message
}
else if (Example.Marketdata.SchemaMetadata.IsCompatible(schemaId, version))
{
    // Process market data message
}
```

### Multi-Schema Message Router Example

```csharp
public class ExampleMessageRouter
{
    public void RouteMessage(ReadOnlySpan<byte> buffer)
    {
        // Read header to determine which schema to use
        var header = MemoryMarshal.Read<MessageHeader>(buffer);
        
        if (Example.Trading.SchemaMetadata.IsCompatible(header.SchemaId, header.Version))
        {
            Console.WriteLine($"Routing to trading handler: {Example.Trading.SchemaMetadata.GetVersionInfo()}");
            HandleTradingMessage(buffer);
        }
        else if (Example.Marketdata.SchemaMetadata.IsCompatible(header.SchemaId, header.Version))
        {
            Console.WriteLine($"Routing to market data handler: {Example.Marketdata.SchemaMetadata.GetVersionInfo()}");
            HandleMarketDataMessage(buffer);
        }
        else
        {
            throw new InvalidOperationException($"Unknown schema: ID={header.SchemaId}, Version={header.Version}");
        }
    }
    
    private void HandleTradingMessage(ReadOnlySpan<byte> buffer)
    {
        if (Example.Trading.OrderData.TryParse(buffer, out var order, out _))
        {
            Console.WriteLine($"Trading Order: ID={order.OrderId.Value}, Price={order.Price.Value}");
        }
    }
    
    private void HandleMarketDataMessage(ReadOnlySpan<byte> buffer)
    {
        if (Example.Marketdata.QuoteData.TryParse(buffer, out var quote, out _))
        {
            Console.WriteLine($"Market Quote: Bid={quote.BidPrice.Value}, Ask={quote.AskPrice.Value}");
        }
    }
}
```

## See Also

- [Multiple Schema Support Guide](../MULTIPLE_SCHEMA_SUPPORT.md) - Comprehensive guide with usage examples
- [Schema Versioning Guide](../SCHEMA_VERSIONING.md) - Field-level versioning details
- [Integration Tests](../../tests/SbeCodeGenerator.IntegrationTests/) - Test examples and use cases
