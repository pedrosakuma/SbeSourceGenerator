using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using V0 = Versioning.Test.V0;
using V1 = Versioning.Test.V1;
using V2 = Versioning.Test.V2;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Integration tests for schema versioning with sinceVersion attribute.
    /// Tests demonstrate that separate types are generated for each version.
    /// </summary>
    public class VersioningIntegrationTests
    {
        [Fact]
        public void SinceVersion_GeneratesSeparateTypesForEachVersion()
        {
            // Verify that 3 separate types were generated (V0, V1, V2)
            var projectDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
            var generatorRoot = Path.Combine(
                projectDir,
                "Generated",
                "SbeSourceGenerator",
                "SbeSourceGenerator.SBESourceGenerator");

            var v0File = Path.Combine(generatorRoot, "Versioning.Test.V0", "Messages", "EvolvingOrder.cs");
            var v1File = Path.Combine(generatorRoot, "Versioning.Test.V1", "Messages", "EvolvingOrderV1.cs");
            var v2File = Path.Combine(generatorRoot, "Versioning.Test.V2", "Messages", "EvolvingOrderV2.cs");

            Assert.True(File.Exists(v0File), $"Expected version 0 message at {v0File}");
            Assert.True(File.Exists(v1File), $"Expected version 1 message at {v1File}");
            Assert.True(File.Exists(v2File), $"Expected version 2 message at {v2File}");
        }

        [Fact]
        public void V0Type_HasOnlyBaseFields()
        {
            // V0 should have only orderId and price (16 bytes)
            Assert.Equal(16, V0.EvolvingOrderData.MESSAGE_SIZE);
            
            // Verify we can create and use V0 type
            Span<byte> buffer = stackalloc byte[V0.EvolvingOrderData.MESSAGE_SIZE];
            ref V0.EvolvingOrderData message = ref MemoryMarshal.AsRef<V0.EvolvingOrderData>(buffer);
            
            message.OrderId = 100;
            message.Price = 9950;
            
            Assert.Equal(100, message.OrderId.Value);
            Assert.Equal(9950, message.Price.Value);
        }

        [Fact]
        public void V1Type_HasQuantityField()
        {
            // V1 should have orderId, price, and quantity (24 bytes)
            Assert.Equal(24, V1.EvolvingOrderData.MESSAGE_SIZE);
            
            // Verify we can create and use V1 type
            Span<byte> buffer = stackalloc byte[V1.EvolvingOrderData.MESSAGE_SIZE];
            ref V1.EvolvingOrderData message = ref MemoryMarshal.AsRef<V1.EvolvingOrderData>(buffer);
            
            message.OrderId = 200;
            message.Price = 10050;
            message.Quantity = 100;
            
            Assert.Equal(200, message.OrderId.Value);
            Assert.Equal(10050, message.Price.Value);
            Assert.Equal(100, message.Quantity);
        }

        [Fact]
        public void V2Type_HasAllFields()
        {
            // V2 should have orderId, price, quantity, and side (25 bytes)
            Assert.Equal(25, V2.EvolvingOrderData.MESSAGE_SIZE);
            
            // Verify we can create and use V2 type
            Span<byte> buffer = stackalloc byte[V2.EvolvingOrderData.MESSAGE_SIZE];
            ref V2.EvolvingOrderData message = ref MemoryMarshal.AsRef<V2.EvolvingOrderData>(buffer);
            
            message.OrderId = 300;
            message.Price = 10100;
            message.Quantity = 150;
            message.Side = 1; // Buy
            
            Assert.Equal(300, message.OrderId.Value);
            Assert.Equal(10100, message.Price.Value);
            Assert.Equal(150, message.Quantity);
            Assert.Equal(1, message.Side);
        }

        [Fact]
        public void VersionedTypes_CanBeUsedTogether()
        {
            // Demonstrate using multiple versions in the same code
            Span<byte> v0Buffer = stackalloc byte[V0.EvolvingOrderData.MESSAGE_SIZE];
            Span<byte> v1Buffer = stackalloc byte[V1.EvolvingOrderData.MESSAGE_SIZE];
            Span<byte> v2Buffer = stackalloc byte[V2.EvolvingOrderData.MESSAGE_SIZE];
            
            ref V0.EvolvingOrderData v0Msg = ref MemoryMarshal.AsRef<V0.EvolvingOrderData>(v0Buffer);
            ref V1.EvolvingOrderData v1Msg = ref MemoryMarshal.AsRef<V1.EvolvingOrderData>(v1Buffer);
            ref V2.EvolvingOrderData v2Msg = ref MemoryMarshal.AsRef<V2.EvolvingOrderData>(v2Buffer);
            
            v0Msg.OrderId = 1;
            v0Msg.Price = 1000;
            
            v1Msg.OrderId = 2;
            v1Msg.Price = 2000;
            v1Msg.Quantity = 50;
            
            v2Msg.OrderId = 3;
            v2Msg.Price = 3000;
            v2Msg.Quantity = 75;
            v2Msg.Side = 2; // Sell
            
            // Each version has its appropriate fields
            Assert.Equal(1, v0Msg.OrderId.Value);
            Assert.Equal(50, v1Msg.Quantity);
            Assert.Equal(2, v2Msg.Side);
        }

        [Fact]
        public void TryParse_WorksForEachVersion()
        {
            // Test that TryParse works for V0
            Span<byte> v0Buffer = stackalloc byte[V0.EvolvingOrderData.MESSAGE_SIZE];
            ref V0.EvolvingOrderData v0Setup = ref MemoryMarshal.AsRef<V0.EvolvingOrderData>(v0Buffer);
            v0Setup.OrderId = 123;
            v0Setup.Price = 9900;
            
            var v0Success = V0.EvolvingOrderData.TryParse(v0Buffer, out var v0Parsed, out _);
            Assert.True(v0Success);
            Assert.Equal(123, v0Parsed.OrderId.Value);
            Assert.Equal(9900, v0Parsed.Price.Value);
            
            // Test that TryParse works for V1
            Span<byte> v1Buffer = stackalloc byte[V1.EvolvingOrderData.MESSAGE_SIZE];
            ref V1.EvolvingOrderData v1Setup = ref MemoryMarshal.AsRef<V1.EvolvingOrderData>(v1Buffer);
            v1Setup.OrderId = 456;
            v1Setup.Price = 10000;
            v1Setup.Quantity = 200;
            
            var v1Success = V1.EvolvingOrderData.TryParse(v1Buffer, out var v1Parsed, out _);
            Assert.True(v1Success);
            Assert.Equal(456, v1Parsed.OrderId.Value);
            Assert.Equal(200, v1Parsed.Quantity);
            
            // Test that TryParse works for V2
            Span<byte> v2Buffer = stackalloc byte[V2.EvolvingOrderData.MESSAGE_SIZE];
            ref V2.EvolvingOrderData v2Setup = ref MemoryMarshal.AsRef<V2.EvolvingOrderData>(v2Buffer);
            v2Setup.OrderId = 789;
            v2Setup.Price = 10100;
            v2Setup.Quantity = 300;
            v2Setup.Side = 1;
            
            var v2Success = V2.EvolvingOrderData.TryParse(v2Buffer, out var v2Parsed, out _);
            Assert.True(v2Success);
            Assert.Equal(789, v2Parsed.OrderId.Value);
            Assert.Equal(300, v2Parsed.Quantity);
            Assert.Equal(1, v2Parsed.Side);
        }

        [Fact]
        public void SinceVersion_DocumentationIndicatesVersion()
        {
            // Verify that version documentation is in generated files
            var projectDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
            var generatorRoot = Path.Combine(
                projectDir,
                "Generated",
                "SbeSourceGenerator",
                "SbeSourceGenerator.SBESourceGenerator");

            var v1File = Path.Combine(generatorRoot, "Versioning.Test.V1", "Messages", "EvolvingOrderV1.cs");
            Assert.True(File.Exists(v1File), $"Expected version 1 message at {v1File}");

            var v1Content = File.ReadAllText(v1File);
            Assert.Contains("Since version 1", v1Content);
            
            // V2 file should have "Since version 2" for side
            var v2File = Path.Combine(generatorRoot, "Versioning.Test.V2", "Messages", "EvolvingOrderV2.cs");
            Assert.True(File.Exists(v2File), $"Expected version 2 message at {v2File}");

            var v2Content = File.ReadAllText(v2File);
            Assert.Contains("Since version 2", v2Content);
        }
    }
}
