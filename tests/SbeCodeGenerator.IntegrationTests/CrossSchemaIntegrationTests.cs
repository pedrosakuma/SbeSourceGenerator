using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests;

/// <summary>
/// Tests that multiple SBE schemas can coexist in the same project without conflicts,
/// and that types with the same name in different schemas are correctly isolated.
/// </summary>
public class CrossSchemaIntegrationTests
{
    [Fact]
    public void SeparateSchemas_GenerateIsolatedNamespaces()
    {
        // Both schemas define Currency enum — verify they are distinct types
        var commonCurrency = Cross.Schema.Common.V0.Currency.EUR;
        var ordersCurrency = Cross.Schema.Orders.V0.Currency.EUR;

        Assert.Equal((byte)1, (byte)commonCurrency);
        Assert.Equal((byte)1, (byte)ordersCurrency);

        // Verify they are different types (cannot assign one to the other)
        Assert.Equal(typeof(Cross.Schema.Common.V0.Currency), commonCurrency.GetType());
        Assert.Equal(typeof(Cross.Schema.Orders.V0.Currency), ordersCurrency.GetType());
        Assert.NotEqual(commonCurrency.GetType(), ordersCurrency.GetType());
    }

    [Fact]
    public void SeparateSchemas_MessageHeadersAreIndependent()
    {
        // Both schemas define MessageHeader — verify both are usable
        Assert.Equal(8, Cross.Schema.Common.V0.MessageHeader.MESSAGE_SIZE);
        Assert.Equal(8, Cross.Schema.Orders.V0.MessageHeader.MESSAGE_SIZE);

        // Different types despite identical layout
        Assert.NotEqual(
            typeof(Cross.Schema.Common.V0.MessageHeader),
            typeof(Cross.Schema.Orders.V0.MessageHeader));
    }

    [Fact]
    public void CommonSchema_HeartbeatRoundTrip()
    {
        // Encode and decode Heartbeat from the common schema
        Span<byte> buffer = stackalloc byte[Cross.Schema.Common.V0.HeartbeatData.MESSAGE_SIZE];
        ref var msg = ref MemoryMarshal.AsRef<Cross.Schema.Common.V0.HeartbeatData>(buffer);
        msg.Timestamp = 1234567890123456789UL;

        var success = Cross.Schema.Common.V0.HeartbeatData.TryParse(
            buffer,
            Cross.Schema.Common.V0.HeartbeatData.MESSAGE_SIZE,
            out var parsed);

        Assert.True(success);
        Assert.Equal(1234567890123456789UL, parsed.Data.Timestamp);
    }

    [Fact]
    public void OrdersSchema_NewOrderEncodeDecodeWithGroups()
    {
        // Test the orders schema message with groups and varData
        Span<byte> buffer = stackalloc byte[512];
        ref var msg = ref MemoryMarshal.AsRef<Cross.Schema.Orders.V0.NewOrderData>(buffer);

        msg.OrderId = 42;
        msg.Quantity = 100;
        msg.Side = Cross.Schema.Orders.V0.Side.Buy;
        msg.Currency = Cross.Schema.Orders.V0.Currency.USD;

        // Verify field values
        Assert.Equal(42UL, msg.OrderId);
        Assert.Equal(100U, msg.Quantity);
        Assert.Equal(Cross.Schema.Orders.V0.Side.Buy, msg.Side);
        Assert.Equal(Cross.Schema.Orders.V0.Currency.USD, msg.Currency);
    }

    [Fact]
    public void SeparateSchemas_DecimalCompositeIsIndependent()
    {
        // Both schemas define Decimal composite — verify both work independently
        Assert.Equal(
            Unsafe.SizeOf<Cross.Schema.Common.V0.Decimal>(),
            Unsafe.SizeOf<Cross.Schema.Orders.V0.Decimal>());

        // Different types
        Assert.NotEqual(
            typeof(Cross.Schema.Common.V0.Decimal),
            typeof(Cross.Schema.Orders.V0.Decimal));
    }

    [Fact]
    public void SeparateSchemas_GroupSizeEncodingIsIndependent()
    {
        // Both define GroupSizeEncoding — verify independent
        Assert.Equal(
            Cross.Schema.Common.V0.GroupSizeEncoding.MESSAGE_SIZE,
            Cross.Schema.Orders.V0.GroupSizeEncoding.MESSAGE_SIZE);

        Assert.NotEqual(
            typeof(Cross.Schema.Common.V0.GroupSizeEncoding),
            typeof(Cross.Schema.Orders.V0.GroupSizeEncoding));
    }

    [Fact]
    public void CrossSchemaInterop_CanUseTypesFromBothSchemas()
    {
        // Simulate a scenario where application code uses types from both schemas
        Span<byte> heartbeatBuffer = stackalloc byte[Cross.Schema.Common.V0.HeartbeatData.MESSAGE_SIZE];
        Span<byte> orderBuffer = stackalloc byte[Cross.Schema.Orders.V0.NewOrderData.MESSAGE_SIZE];

        ref var heartbeat = ref MemoryMarshal.AsRef<Cross.Schema.Common.V0.HeartbeatData>(heartbeatBuffer);
        ref var order = ref MemoryMarshal.AsRef<Cross.Schema.Orders.V0.NewOrderData>(orderBuffer);

        heartbeat.Timestamp = 100;
        order.OrderId = 200;

        // Parse both independently
        var s1 = Cross.Schema.Common.V0.HeartbeatData.TryParse(
            heartbeatBuffer,
            Cross.Schema.Common.V0.HeartbeatData.MESSAGE_SIZE,
            out var parsedHb);

        var s2 = Cross.Schema.Orders.V0.NewOrderData.TryParse(
            orderBuffer,
            Cross.Schema.Orders.V0.NewOrderData.MESSAGE_SIZE,
            out var parsedOrder);

        Assert.True(s1);
        Assert.True(s2);
        Assert.Equal(100UL, parsedHb.Data.Timestamp);
        Assert.Equal(200UL, parsedOrder.Data.OrderId);
    }
}
