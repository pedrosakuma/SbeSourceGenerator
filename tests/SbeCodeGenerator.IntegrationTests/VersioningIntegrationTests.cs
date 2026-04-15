using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using V0 = Versioning.Test.V2;
using V1 = Versioning.Test.V2.V1;
using V2 = Versioning.Test.V2.V2;

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

            // Discover the hash-suffixed directory dynamically
            var schemaDir = Directory.GetDirectories(generatorRoot, "versioning_test_schema_*").FirstOrDefault();
            Assert.NotNull(schemaDir);

            var v0File = Path.Combine(schemaDir!, "Versioning.Test.V2", "Messages", "EvolvingOrder.cs");
            var v1File = Path.Combine(schemaDir!, "Versioning.Test.V2.V1", "Messages", "EvolvingOrderV1.cs");
            var v2File = Path.Combine(schemaDir!, "Versioning.Test.V2.V2", "Messages", "EvolvingOrderV2.cs");

            Assert.True(File.Exists(v0File), $"Expected version 0 message at {v0File}");
            Assert.True(File.Exists(v1File), $"Expected version 1 message at {v1File}");
            Assert.True(File.Exists(v2File), $"Expected version 2 message at {v2File}");
        }

        [Fact]
        public void V0Type_IncludesAllSchemaVersionFields()
        {
            // Base type (V0 alias) should have all fields up to schema version 2
            // so BLOCK_LENGTH matches the wire blockLength (#143)
            Assert.Equal(25, V0.EvolvingOrderData.MESSAGE_SIZE);
            
            // Verify we can create and use base type with all fields
            Span<byte> buffer = stackalloc byte[V0.EvolvingOrderData.MESSAGE_SIZE];
            ref V0.EvolvingOrderData message = ref MemoryMarshal.AsRef<V0.EvolvingOrderData>(buffer);
            
            message.OrderId = 100;
            message.Price = 9950;
            message.Quantity = 50;
            message.Side = 1;
            
            Assert.Equal(100, message.OrderId.Value);
            Assert.Equal(9950, message.Price.Value);
            Assert.Equal(50, message.Quantity);
            Assert.Equal(1, message.Side);
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
            Span<byte> baseBuffer = stackalloc byte[V0.EvolvingOrderData.MESSAGE_SIZE];
            Span<byte> v1Buffer = stackalloc byte[V1.EvolvingOrderData.MESSAGE_SIZE];
            Span<byte> v2Buffer = stackalloc byte[V2.EvolvingOrderData.MESSAGE_SIZE];
            
            // Base type has all schema-version fields
            ref V0.EvolvingOrderData baseMsg = ref MemoryMarshal.AsRef<V0.EvolvingOrderData>(baseBuffer);
            ref V1.EvolvingOrderData v1Msg = ref MemoryMarshal.AsRef<V1.EvolvingOrderData>(v1Buffer);
            ref V2.EvolvingOrderData v2Msg = ref MemoryMarshal.AsRef<V2.EvolvingOrderData>(v2Buffer);
            
            baseMsg.OrderId = 1;
            baseMsg.Price = 1000;
            baseMsg.Quantity = 25;
            baseMsg.Side = 1;
            
            v1Msg.OrderId = 2;
            v1Msg.Price = 2000;
            v1Msg.Quantity = 50;
            
            v2Msg.OrderId = 3;
            v2Msg.Price = 3000;
            v2Msg.Quantity = 75;
            v2Msg.Side = 2; // Sell
            
            // Each version has its appropriate fields
            Assert.Equal(1, baseMsg.OrderId.Value);
            Assert.Equal(25, baseMsg.Quantity);
            Assert.Equal(50, v1Msg.Quantity);
            Assert.Equal(2, v2Msg.Side);
        }

        [Fact]
        public void TryParse_WorksForEachVersion()
        {
            // Test that TryParse works for base type (has all schema-version fields)
            Span<byte> v0Buffer = stackalloc byte[V0.EvolvingOrderData.MESSAGE_SIZE];
            ref V0.EvolvingOrderData v0Setup = ref MemoryMarshal.AsRef<V0.EvolvingOrderData>(v0Buffer);
            v0Setup.OrderId = 123;
            v0Setup.Price = 9900;
            v0Setup.Quantity = 50;
            v0Setup.Side = 2;
            
            var v0Success = V0.EvolvingOrderData.TryParse(v0Buffer, out var v0Parsed);
            Assert.True(v0Success);
            Assert.Equal(123, v0Parsed.Data.OrderId.Value);
            Assert.Equal(9900, v0Parsed.Data.Price.Value);
            Assert.Equal(50, v0Parsed.Data.Quantity);
            Assert.Equal(2, v0Parsed.Data.Side);
            
            // Test that TryParse works for V1
            Span<byte> v1Buffer = stackalloc byte[V1.EvolvingOrderData.MESSAGE_SIZE];
            ref V1.EvolvingOrderData v1Setup = ref MemoryMarshal.AsRef<V1.EvolvingOrderData>(v1Buffer);
            v1Setup.OrderId = 456;
            v1Setup.Price = 10000;
            v1Setup.Quantity = 200;
            
            var v1Success = V1.EvolvingOrderData.TryParse(v1Buffer, out var v1Parsed);
            Assert.True(v1Success);
            Assert.Equal(456, v1Parsed.Data.OrderId.Value);
            Assert.Equal(200, v1Parsed.Data.Quantity);
            
            // Test that TryParse works for V2
            Span<byte> v2Buffer = stackalloc byte[V2.EvolvingOrderData.MESSAGE_SIZE];
            ref V2.EvolvingOrderData v2Setup = ref MemoryMarshal.AsRef<V2.EvolvingOrderData>(v2Buffer);
            v2Setup.OrderId = 789;
            v2Setup.Price = 10100;
            v2Setup.Quantity = 300;
            v2Setup.Side = 1;
            
            var v2Success = V2.EvolvingOrderData.TryParse(v2Buffer, out var v2Parsed);
            Assert.True(v2Success);
            Assert.Equal(789, v2Parsed.Data.OrderId.Value);
            Assert.Equal(300, v2Parsed.Data.Quantity);
            Assert.Equal(1, v2Parsed.Data.Side);
        }

        [Fact]
        public void BaseType_BlockLengthMatchesSchemaVersion()
        {
            // Regression test for #143: BLOCK_LENGTH must equal the schema-version
            // size so ReadGroups starts parsing at the correct wire offset.
            // V2 schema: orderId(8) + price(8) + quantity(8) + side(1) = 25
            Assert.Equal(25, V0.EvolvingOrderData.BLOCK_LENGTH);
            Assert.Equal(V2.EvolvingOrderData.BLOCK_LENGTH, V0.EvolvingOrderData.BLOCK_LENGTH);
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

            var schemaDir = Directory.GetDirectories(generatorRoot, "versioning_test_schema_*").FirstOrDefault();
            Assert.NotNull(schemaDir);

            var v1File = Path.Combine(schemaDir!, "Versioning.Test.V2.V1", "Messages", "EvolvingOrderV1.cs");
            Assert.True(File.Exists(v1File), $"Expected version 1 message at {v1File}");

            var v1Content = File.ReadAllText(v1File);
            Assert.Contains("Since version 1", v1Content);
            
            // V2 file should have "Since version 2" for side
            var v2File = Path.Combine(schemaDir!, "Versioning.Test.V2.V2", "Messages", "EvolvingOrderV2.cs");
            Assert.True(File.Exists(v2File), $"Expected version 2 message at {v2File}");

            var v2Content = File.ReadAllText(v2File);
            Assert.Contains("Since version 2", v2Content);
        }
    }
}
