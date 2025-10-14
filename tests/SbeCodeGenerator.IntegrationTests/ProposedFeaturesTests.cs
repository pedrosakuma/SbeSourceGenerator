using Xunit;
using System.Runtime.InteropServices;

namespace SbeCodeGenerator.IntegrationTests.Proposed;

// Helper struct for testing readonly container
file readonly struct ReadonlyContainer
{
    private readonly ProposedFeaturesTests.ProposedOrderId _orderId;
    
    public ReadonlyContainer(ProposedFeaturesTests.ProposedOrderId orderId)
    {
        _orderId = orderId;
    }
    
    // This should not create defensive copy because ProposedOrderId is readonly
    public long GetValue() => (long)_orderId;
}

/// <summary>
/// Tests demonstrating proposed features from feasibility study:
/// - Automatic constructors
/// - Readonly structs  
/// - Implicit/explicit conversions
/// 
/// These tests show how the features would work if implemented.
/// Currently they test manually-created example types.
/// </summary>
public class ProposedFeaturesTests
{
    // Example of proposed TypeDefinition with all features
    public readonly struct ProposedOrderId
    {
        public readonly long Value;
        
        public ProposedOrderId(long value)
        {
            Value = value;
        }
        
        public static implicit operator ProposedOrderId(long value) => new ProposedOrderId(value);
        public static explicit operator long(ProposedOrderId id) => id.Value;
    }
    
    public readonly struct ProposedPrice
    {
        public readonly long Value;
        
        public ProposedPrice(long value)
        {
            Value = value;
        }
        
        public static implicit operator ProposedPrice(long value) => new ProposedPrice(value);
        public static explicit operator long(ProposedPrice price) => price.Value;
    }

    [Fact]
    public void ImplicitConversion_FromNativeToWrapper_Works()
    {
        // Arrange
        long value = 123456;
        
        // Act - implicit conversion
        ProposedOrderId orderId = value;
        
        // Assert
        Assert.Equal(value, orderId.Value);
    }

    [Fact]
    public void ExplicitConversion_FromWrapperToNative_Works()
    {
        // Arrange
        var orderId = new ProposedOrderId(123456);
        
        // Act - explicit conversion
        long value = (long)orderId;
        
        // Assert
        Assert.Equal(123456, value);
    }

    [Fact]
    public void Constructor_InitializesValueCorrectly()
    {
        // Act
        var orderId = new ProposedOrderId(123456);
        
        // Assert
        Assert.Equal(123456, orderId.Value);
    }

    [Fact]
    public void ReadonlyStruct_CanBeUsedInReadonlyContext()
    {
        // Arrange
        var container = new ReadonlyContainer(new ProposedOrderId(123456));
        
        // Act & Assert
        Assert.Equal(123456, container.GetValue());
    }

    [Fact]
    public void ImplicitConversion_WorksInObjectInitializer()
    {
        // This demonstrates how conversions would work in message construction
        var data = new
        {
            OrderId = (ProposedOrderId)123456,  // With implicit conversion
            Price = (ProposedPrice)100000       // With implicit conversion
        };
        
        // Assert
        Assert.Equal(123456, data.OrderId.Value);
        Assert.Equal(100000, data.Price.Value);
    }

    [Fact]
    public void ImplicitConversion_SimplifiesUsage()
    {
        // Before: would use constructor or direct value assignment
        var orderIdOld = new ProposedOrderId(123456);
        
        // After: concise with implicit conversion
        ProposedOrderId orderIdNew = 123456;
        
        // Assert - both work the same
        Assert.Equal(orderIdOld.Value, orderIdNew.Value);
    }

    [Fact]
    public void ExplicitConversion_PreventsAccidentalLossOfTypeSafety()
    {
        // Arrange
        ProposedOrderId orderId = 123456;
        
        // This would NOT compile - good! Preserves type safety
        // long value = orderId; // Error: cannot implicitly convert
        
        // Must be explicit - intentional loss of type safety
        long value = (long)orderId;
        
        Assert.Equal(123456, value);
    }

    // Helper struct for testing composite constructors
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct ProposedMessageHeader
    {
        public readonly ushort BlockLength;
        public readonly ushort TemplateId;
        public readonly ushort SchemaId;
        public readonly ushort Version;
        
        public ProposedMessageHeader(ushort blockLength, ushort templateId, ushort schemaId, ushort version)
        {
            BlockLength = blockLength;
            TemplateId = templateId;
            SchemaId = schemaId;
            Version = version;
        }
    }

    [Fact]
    public void ProposedComposite_WithConstructor_SimplifiesCreation()
    {
        // Example of proposed composite with constructor
        // (Currently this is manual, but shows how it would work if generated)
        
        // Before: would use verbose object initializer (not possible with readonly)
        // After: concise constructor
        var headerNew = new ProposedMessageHeader(100, 10, 2, 0);
        
        // Assert
        Assert.Equal((ushort)100, headerNew.BlockLength);
        Assert.Equal((ushort)10, headerNew.TemplateId);
        Assert.Equal((ushort)2, headerNew.SchemaId);
        Assert.Equal((ushort)0, headerNew.Version);
    }

    [Fact]
    public void DefaultConstructor_CreatesDefaultValue()
    {
        // Act - default constructor
        var orderId = new ProposedOrderId();
        
        // Assert
        Assert.Equal(0, orderId.Value);
    }

    [Fact]
    public void ReadonlyStruct_IsActuallyReadonly()
    {
        // Arrange
        var orderId = new ProposedOrderId(123456);
        
        // This would NOT compile - good!
        // orderId.Value = 789; // Error: cannot modify readonly field
        
        // Assert - value unchanged
        Assert.Equal(123456, orderId.Value);
    }

    [Fact]
    public void Conversions_WorkWithArithmetic()
    {
        // Arrange
        ProposedPrice unitPrice = 100;
        long quantity = 5;
        
        // Act - arithmetic requires explicit conversion to native
        long totalValue = (long)unitPrice * quantity;
        ProposedPrice totalPrice = totalValue;
        
        // Assert
        Assert.Equal(500, totalPrice.Value);
    }

    [Fact]
    public void Conversions_EnableInteropWithExistingAPIs()
    {
        // Simulating an existing API that takes long
        static void ProcessOrder(long orderId, long price)
        {
            Assert.Equal(123456, orderId);
            Assert.Equal(100000, price);
        }
        
        // Arrange
        ProposedOrderId orderId = 123456;
        ProposedPrice price = 100000;
        
        // Act - explicit conversions for API compatibility
        ProcessOrder((long)orderId, (long)price);
    }

    // Helper struct for testing ref structs
    private readonly ref struct ProposedVarString
    {
        public readonly byte Length;
        public readonly ReadOnlySpan<byte> Data;
        
        public ProposedVarString(byte length, ReadOnlySpan<byte> data)
        {
            Length = length;
            Data = data;
        }
    }

    [Fact]
    public void ProposedRefStruct_WithReadonlyAndConstructor()
    {
        // Example of proposed ref struct with readonly and constructor
        
        // Arrange
        Span<byte> buffer = stackalloc byte[] { 5, (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };
        
        // Act
        var str = new ProposedVarString(5, buffer.Slice(1));
        
        // Assert
        Assert.Equal((byte)5, str.Length);
        Assert.Equal(5, str.Data.Length);
    }

    // ===== Phase 1 Feature Validation Tests =====
    // These tests validate that the actual generated types from the schema
    // now have the same features as the manually-created proposed types above

    [Fact]
    public void GeneratedTypeDefinition_HasReadonlyStruct()
    {
        // Verify that generated TypeDefinition types are readonly structs
        var orderIdType = typeof(Integration.Test.OrderId);
        Assert.True(orderIdType.IsValueType);
        
        // Check if struct is readonly (using reflection to check attributes/modifiers)
        var structAttribute = orderIdType.GetCustomAttributes(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), false);
        // Note: IsReadOnlyAttribute may not be present in all frameworks, so we check the field instead
        var valueField = orderIdType.GetField("Value");
        Assert.NotNull(valueField);
        Assert.True(valueField.IsInitOnly, "Value field should be readonly");
    }

    [Fact]
    public void GeneratedTypeDefinition_HasConstructor()
    {
        // Verify that generated TypeDefinition types have a constructor
        // Act - use constructor to create instance
        var orderId = new Integration.Test.OrderId(123456);
        
        // Assert
        Assert.Equal(123456, orderId.Value);
    }

    [Fact]
    public void GeneratedTypeDefinition_SupportsImplicitConversion()
    {
        // Verify implicit conversion from primitive to wrapper works
        // Act - implicit conversion
        Integration.Test.OrderId orderId = 123456;
        
        // Assert
        Assert.Equal(123456, orderId.Value);
    }

    [Fact]
    public void GeneratedTypeDefinition_SupportsExplicitConversion()
    {
        // Verify explicit conversion from wrapper to primitive works
        // Arrange
        var orderId = new Integration.Test.OrderId(123456);
        
        // Act - explicit conversion
        long value = (long)orderId;
        
        // Assert
        Assert.Equal(123456, value);
    }

    [Fact]
    public void GeneratedTypeDefinition_Phase1Features_WorkTogether()
    {
        // Comprehensive test showing all Phase 1 features working together
        
        // 1. Implicit conversion for concise initialization
        Integration.Test.Price price = 100000;
        Assert.Equal(100000, price.Value);
        
        // 2. Constructor for explicit initialization
        var quantity = new Integration.Test.OrderId(500);
        Assert.Equal(500, quantity.Value);
        
        // 3. Explicit conversion for interop
        long priceValue = (long)price;
        Assert.Equal(100000, priceValue);
        
        // 4. Use in calculations
        long total = (long)price * (long)quantity;
        Assert.Equal(50000000, total);
        
        // 5. Readonly prevents mutation (compile-time check, shown in test)
        // price.Value = 200000; // Would not compile - field is readonly
    }
}
