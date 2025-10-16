# Multiple Schema Version Support Guide

## Overview

The SBE Code Generator now provides comprehensive support for multiple schema versions through schema-level metadata tracking. This enables applications to work with different schemas simultaneously, perform version negotiation, and validate schema compatibility at runtime.

## Key Features

### 1. Schema Metadata Generation

For each SBE XML schema file, the generator automatically creates a `SchemaMetadata` class containing:

- **SCHEMA_ID**: Unique identifier for the schema (from `id` attribute)
- **SCHEMA_VERSION**: Current version number (from `version` attribute)
- **SEMANTIC_VERSION**: Human-readable version (from `semanticVersion` attribute)
- **PACKAGE**: Package/namespace name (from `package` attribute)
- **DESCRIPTION**: Schema description (from `description` attribute)
- **BYTE_ORDER**: Endianness setting (from `byteOrder` attribute)

### 2. Runtime Version Checking

The `SchemaMetadata` class provides helper methods for:
- **IsCompatible()**: Checks if the current schema can read messages from another version
- **GetVersionInfo()**: Returns formatted version information string

## Schema XML Example

```xml
<?xml version="1.0" encoding="UTF-8"?>
<sbe:messageSchema xmlns:sbe="http://fixprotocol.io/2016/sbe"
                   package="trading_system"
                   id="100"
                   version="2"
                   semanticVersion="2.1.0"
                   description="Trading system messages"
                   byteOrder="littleEndian">
    <types>
        <composite name="MessageHeader">
            <type name="blockLength" primitiveType="uint16"/>
            <type name="templateId" primitiveType="uint16"/>
            <type name="schemaId" primitiveType="uint16"/>
            <type name="version" primitiveType="uint16"/>
        </composite>
        
        <type name="OrderId" primitiveType="int64"/>
        <type name="Price" primitiveType="int64"/>
    </types>

    <sbe:message name="Order" id="1" description="Order message">
        <field name="orderId" id="1" type="OrderId"/>
        <field name="price" id="2" type="Price"/>
        <field name="quantity" id="3" type="int64" sinceVersion="1"/>
        <field name="side" id="4" type="uint8" sinceVersion="2"/>
    </sbe:message>
</sbe:messageSchema>
```

## Generated Code

The generator creates:

```csharp
namespace Trading.System
{
    /// <summary>
    /// Schema metadata for Trading.System
    /// Trading system messages
    /// </summary>
    public static class SchemaMetadata
    {
        public const ushort SCHEMA_ID = 100;
        public const ushort SCHEMA_VERSION = 2;
        public const string SEMANTIC_VERSION = "2.1.0";
        public const string PACKAGE = "trading_system";
        public const string DESCRIPTION = "Trading system messages";
        public const string BYTE_ORDER = "littleEndian";
        
        public static bool IsCompatible(ushort schemaId, ushort version)
        {
            if (schemaId != SCHEMA_ID)
                return false;
            return version <= SCHEMA_VERSION;
        }
        
        public static string GetVersionInfo()
        {
            return $"Schema ID: {SCHEMA_ID}, Version: {SCHEMA_VERSION} ({SEMANTIC_VERSION})";
        }
    }
}
```

## Usage Examples

### Example 1: Multi-Schema Message Router

When your application needs to handle messages from multiple schemas:

```csharp
using System;
using Trading.System;
using Market.Data;

public class MessageRouter
{
    public void RouteMessage(ReadOnlySpan<byte> buffer)
    {
        // Read the message header to get schema ID and version
        var header = MemoryMarshal.Read<MessageHeader>(buffer);
        
        // Route to the appropriate handler based on schema ID
        if (Trading.System.SchemaMetadata.IsCompatible(header.SchemaId, header.Version))
        {
            HandleTradingMessage(buffer);
        }
        else if (Market.Data.SchemaMetadata.IsCompatible(header.SchemaId, header.Version))
        {
            HandleMarketDataMessage(buffer);
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported schema: ID={header.SchemaId}, Version={header.Version}");
        }
    }
    
    private void HandleTradingMessage(ReadOnlySpan<byte> buffer)
    {
        // Process using Trading.System schema
        Console.WriteLine($"Processing with {Trading.System.SchemaMetadata.GetVersionInfo()}");
        // ... decode and process message
    }
    
    private void HandleMarketDataMessage(ReadOnlySpan<byte> buffer)
    {
        // Process using Market.Data schema
        Console.WriteLine($"Processing with {Market.Data.SchemaMetadata.GetVersionInfo()}");
        // ... decode and process message
    }
}
```

### Example 2: Version Negotiation

Implement version negotiation between client and server:

```csharp
public class SchemaVersionNegotiator
{
    public bool NegotiateVersion(
        ushort clientSchemaId, 
        ushort clientVersion,
        out ushort negotiatedVersion)
    {
        // Check if we support the client's schema
        if (clientSchemaId != Trading.System.SchemaMetadata.SCHEMA_ID)
        {
            Console.WriteLine($"Schema ID mismatch: client={clientSchemaId}, server={Trading.System.SchemaMetadata.SCHEMA_ID}");
            negotiatedVersion = 0;
            return false;
        }
        
        // Use the minimum version supported by both parties
        negotiatedVersion = Math.Min(clientVersion, Trading.System.SchemaMetadata.SCHEMA_VERSION);
        
        Console.WriteLine($"Version negotiated: {negotiatedVersion} (client={clientVersion}, server={Trading.System.SchemaMetadata.SCHEMA_VERSION})");
        return true;
    }
}

// Client usage
var negotiator = new SchemaVersionNegotiator();
if (negotiator.NegotiateVersion(
    Trading.System.SchemaMetadata.SCHEMA_ID,
    1, // Client supports version 1
    out ushort negotiatedVersion))
{
    Console.WriteLine($"Using protocol version {negotiatedVersion}");
    // Proceed with communication using negotiated version
}
```

### Example 3: Schema Compatibility Validation

Validate message compatibility before decoding:

```csharp
public class MessageValidator
{
    public bool ValidateMessage(ReadOnlySpan<byte> buffer, out string errorMessage)
    {
        if (buffer.Length < 8) // Minimum header size
        {
            errorMessage = "Buffer too small for message header";
            return false;
        }
        
        // Read message header
        var header = MemoryMarshal.Read<MessageHeader>(buffer);
        
        // Validate schema compatibility
        if (!Trading.System.SchemaMetadata.IsCompatible(header.SchemaId, header.Version))
        {
            errorMessage = $"Incompatible schema: received ID={header.SchemaId}, Version={header.Version}; " +
                          $"expected ID={Trading.System.SchemaMetadata.SCHEMA_ID}, Version<={Trading.System.SchemaMetadata.SCHEMA_VERSION}";
            return false;
        }
        
        errorMessage = null;
        return true;
    }
}

// Usage
var validator = new MessageValidator();
if (validator.ValidateMessage(receivedBuffer, out string error))
{
    // Safe to decode
    Trading.System.OrderData.TryParse(receivedBuffer, out var order, out _);
}
else
{
    Console.WriteLine($"Validation failed: {error}");
}
```

### Example 4: Multi-Version Support

Support multiple schema versions in the same application:

```csharp
public class MultiVersionOrderProcessor
{
    public void ProcessOrder(ReadOnlySpan<byte> buffer)
    {
        var header = MemoryMarshal.Read<MessageHeader>(buffer);
        
        // Check schema compatibility
        if (!Trading.System.SchemaMetadata.IsCompatible(header.SchemaId, header.Version))
        {
            throw new InvalidOperationException("Incompatible schema version");
        }
        
        // Process based on the version
        switch (header.Version)
        {
            case 0:
                ProcessV0Order(buffer);
                break;
            case 1:
                ProcessV1Order(buffer);
                break;
            case 2:
                ProcessV2Order(buffer);
                break;
            default:
                throw new NotSupportedException($"Version {header.Version} not supported");
        }
    }
    
    private void ProcessV0Order(ReadOnlySpan<byte> buffer)
    {
        // Use V0 types (base version)
        Trading.System.OrderData.TryParse(buffer, out var order, out _);
        Console.WriteLine($"V0 Order: ID={order.OrderId.Value}, Price={order.Price.Value}");
    }
    
    private void ProcessV1Order(ReadOnlySpan<byte> buffer)
    {
        // Use V1 types (includes quantity field)
        Trading.System.V1.OrderData.TryParse(buffer, out var order, out _);
        Console.WriteLine($"V1 Order: ID={order.OrderId.Value}, Price={order.Price.Value}, Qty={order.Quantity}");
    }
    
    private void ProcessV2Order(ReadOnlySpan<byte> buffer)
    {
        // Use V2 types (includes quantity and side fields)
        Trading.System.V2.OrderData.TryParse(buffer, out var order, out _);
        Console.WriteLine($"V2 Order: ID={order.OrderId.Value}, Price={order.Price.Value}, Qty={order.Quantity}, Side={order.Side}");
    }
}
```

### Example 5: Schema Registry

Implement a schema registry for managing multiple schemas:

```csharp
public class SchemaRegistry
{
    private readonly Dictionary<ushort, SchemaInfo> _schemas = new();
    
    public class SchemaInfo
    {
        public ushort SchemaId { get; set; }
        public ushort Version { get; set; }
        public string SemanticVersion { get; set; }
        public string Package { get; set; }
        public string Description { get; set; }
        public Func<ushort, ushort, bool> IsCompatible { get; set; }
    }
    
    public SchemaRegistry()
    {
        // Register all known schemas
        RegisterSchema(
            Trading.System.SchemaMetadata.SCHEMA_ID,
            Trading.System.SchemaMetadata.SCHEMA_VERSION,
            Trading.System.SchemaMetadata.SEMANTIC_VERSION,
            Trading.System.SchemaMetadata.PACKAGE,
            Trading.System.SchemaMetadata.DESCRIPTION,
            Trading.System.SchemaMetadata.IsCompatible
        );
        
        // Register other schemas...
    }
    
    public void RegisterSchema(
        ushort schemaId, 
        ushort version, 
        string semanticVersion,
        string package,
        string description,
        Func<ushort, ushort, bool> isCompatible)
    {
        _schemas[schemaId] = new SchemaInfo
        {
            SchemaId = schemaId,
            Version = version,
            SemanticVersion = semanticVersion,
            Package = package,
            Description = description,
            IsCompatible = isCompatible
        };
    }
    
    public bool TryGetSchema(ushort schemaId, out SchemaInfo schema)
    {
        return _schemas.TryGetValue(schemaId, out schema);
    }
    
    public bool ValidateCompatibility(ushort schemaId, ushort version)
    {
        if (!TryGetSchema(schemaId, out var schema))
            return false;
            
        return schema.IsCompatible(schemaId, version);
    }
    
    public void ListSchemas()
    {
        foreach (var schema in _schemas.Values)
        {
            Console.WriteLine($"Schema {schema.SchemaId} v{schema.Version} ({schema.SemanticVersion}): {schema.Description}");
        }
    }
}

// Usage
var registry = new SchemaRegistry();
registry.ListSchemas();

if (registry.ValidateCompatibility(100, 1))
{
    Console.WriteLine("Message is compatible with registered schemas");
}
```

## Best Practices

### 1. Always Check Schema Compatibility

Before decoding messages, always validate schema compatibility:

```csharp
if (!SchemaMetadata.IsCompatible(header.SchemaId, header.Version))
{
    // Handle incompatibility
    throw new InvalidOperationException("Schema version mismatch");
}
```

### 2. Use Schema ID for Routing

Use schema IDs to route messages to appropriate handlers:

```csharp
switch (header.SchemaId)
{
    case TradingSchemaMetadata.SCHEMA_ID:
        ProcessTradingMessage(buffer);
        break;
    case MarketDataSchemaMetadata.SCHEMA_ID:
        ProcessMarketDataMessage(buffer);
        break;
}
```

### 3. Version Negotiation at Connection

Perform version negotiation during connection setup:

```csharp
public void OnClientConnect(Client client)
{
    var clientVersion = client.GetSchemaVersion();
    var serverVersion = SchemaMetadata.SCHEMA_VERSION;
    var negotiatedVersion = Math.Min(clientVersion, serverVersion);
    
    client.SetNegotiatedVersion(negotiatedVersion);
}
```

### 4. Log Schema Information

Log schema information for debugging:

```csharp
_logger.LogInformation(
    "Processing message with {SchemaInfo}",
    SchemaMetadata.GetVersionInfo()
);
```

## Integration with Existing Code

### Backward Compatibility

The schema metadata feature is additive and doesn't break existing code. All existing message types and functionality remain unchanged.

### Migration Path

1. Update to the latest version of SbeSourceGenerator
2. Rebuild your project - SchemaMetadata classes are generated automatically
3. Optionally use SchemaMetadata for version checking and multi-schema support
4. Existing code continues to work without modifications

## Testing

Test schema compatibility with different versions:

```csharp
[Fact]
public void SchemaCompatibility_WorksCorrectly()
{
    // Current schema is version 2
    Assert.True(SchemaMetadata.IsCompatible(100, 0)); // Old version
    Assert.True(SchemaMetadata.IsCompatible(100, 1)); // Old version
    Assert.True(SchemaMetadata.IsCompatible(100, 2)); // Current version
    Assert.False(SchemaMetadata.IsCompatible(100, 3)); // Future version
    Assert.False(SchemaMetadata.IsCompatible(999, 2)); // Different schema
}
```

## See Also

- [Schema Versioning Guide](./SCHEMA_VERSIONING.md) - Field-level versioning with sinceVersion
- [Integration Tests](../tests/SbeCodeGenerator.IntegrationTests/SchemaMetadataIntegrationTests.cs) - Examples and test cases
- [SBE Specification](https://github.com/FIXTradingCommunity/fix-simple-binary-encoding) - SBE standard documentation
