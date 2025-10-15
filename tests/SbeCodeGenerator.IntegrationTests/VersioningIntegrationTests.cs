using System;
using System.Runtime.InteropServices;
using Versioning.Test;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Integration tests for schema versioning with sinceVersion attribute.
    /// Tests demonstrate forward and backward compatibility scenarios.
    /// </summary>
    public class VersioningIntegrationTests
    {
        /// <summary>
        /// Schema V1: orderId + price = 16 bytes
        /// Schema V2: orderId + price + quantity = 24 bytes (quantity added in version 1)
        /// Schema V3: orderId + price + quantity + side = 25 bytes (side added in version 2)
        /// </summary>
        private const int V1_BLOCK_LENGTH = 16;  // orderId (8) + price (8)
        private const int V2_BLOCK_LENGTH = 24;  // V1 + quantity (8)
        private const int V3_BLOCK_LENGTH = 25;  // V2 + side (1)

        [Fact]
        public void SinceVersion_DocumentationIsGenerated()
        {
            // Verify that the generated code includes "Since version" in XML documentation
            // This is checked by looking at the generated file content
            var projectDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
            var generatedFile = File.ReadAllText(
                Directory.GetFiles(
                    Path.Combine(projectDir, "Generated"),
                    "EvolvingOrder.cs",
                    SearchOption.AllDirectories
                ).First()
            );

            // Fields without sinceVersion should not have "Since version" comment
            Assert.Contains("Order ID - present since version 0", generatedFile);
            Assert.Contains("Order price - present since version 0", generatedFile);

            // Fields with sinceVersion should have "Since version" comment
            Assert.Contains("Since version 1", generatedFile);
            Assert.Contains("Since version 2", generatedFile);
        }

        [Fact]
        public void SchemaEvolution_V3DecoderReadsV1Message()
        {
            // Arrange: Create a V1 message (only orderId and price)
            // Buffer must be at least MESSAGE_SIZE for TryRead to work
            Span<byte> v1Buffer = stackalloc byte[EvolvingOrderData.MESSAGE_SIZE];
            v1Buffer.Clear();  // Zero out the buffer
            ref EvolvingOrderData v1Message = ref MemoryMarshal.AsRef<EvolvingOrderData>(v1Buffer);
            
            v1Message.OrderId = 12345;
            v1Message.Price = 9950;
            // quantity and side are not written (V1 doesn't have them) - they remain zero

            // Act: Use V3 decoder to read V1 message with V1 block length
            var success = EvolvingOrderData.TryParse(v1Buffer, V1_BLOCK_LENGTH, out var decoded, out var remaining);

            // Assert
            Assert.True(success);
            Assert.Equal(12345, decoded.OrderId.Value);
            Assert.Equal(9950, decoded.Price.Value);
            // Fields added in later versions should have default values
            Assert.Equal(0, decoded.Quantity);  // Default value for int64
            Assert.Equal(0, decoded.Side);      // Default value for uint8
            // Remaining starts at V1_BLOCK_LENGTH
            Assert.Equal(EvolvingOrderData.MESSAGE_SIZE - V1_BLOCK_LENGTH, remaining.Length);
        }

        [Fact]
        public void SchemaEvolution_V3DecoderReadsV2Message()
        {
            // Arrange: Create a V2 message (orderId, price, and quantity)
            // Buffer must be at least MESSAGE_SIZE for TryRead to work
            Span<byte> v2Buffer = stackalloc byte[EvolvingOrderData.MESSAGE_SIZE];
            v2Buffer.Clear();  // Zero out the buffer
            ref EvolvingOrderData v2Message = ref MemoryMarshal.AsRef<EvolvingOrderData>(v2Buffer);
            
            v2Message.OrderId = 67890;
            v2Message.Price = 10050;
            v2Message.Quantity = 100;
            // side is not written (V2 doesn't have it) - it remains zero

            // Act: Use V3 decoder to read V2 message with V2 block length
            var success = EvolvingOrderData.TryParse(v2Buffer, V2_BLOCK_LENGTH, out var decoded, out var remaining);

            // Assert
            Assert.True(success);
            Assert.Equal(67890, decoded.OrderId.Value);
            Assert.Equal(10050, decoded.Price.Value);
            Assert.Equal(100, decoded.Quantity);
            // Field added in version 2 should have default value
            Assert.Equal(0, decoded.Side);
            // Remaining starts at V2_BLOCK_LENGTH
            Assert.Equal(EvolvingOrderData.MESSAGE_SIZE - V2_BLOCK_LENGTH, remaining.Length);
        }

        [Fact]
        public void SchemaEvolution_V3DecoderReadsV3Message()
        {
            // Arrange: Create a V3 message (all fields)
            Span<byte> v3Buffer = stackalloc byte[V3_BLOCK_LENGTH];
            ref EvolvingOrderData v3Message = ref MemoryMarshal.AsRef<EvolvingOrderData>(v3Buffer);
            
            v3Message.OrderId = 99999;
            v3Message.Price = 10100;
            v3Message.Quantity = 200;
            v3Message.Side = 1;  // Buy

            // Act: Use V3 decoder to read V3 message
            var success = EvolvingOrderData.TryParse(v3Buffer, V3_BLOCK_LENGTH, out var decoded, out var remaining);

            // Assert
            Assert.True(success);
            Assert.Equal(99999, decoded.OrderId.Value);
            Assert.Equal(10100, decoded.Price.Value);
            Assert.Equal(200, decoded.Quantity);
            Assert.Equal(1, decoded.Side);
            Assert.Equal(0, remaining.Length);
        }

        [Fact]
        public void SchemaEvolution_V1DecoderReadsV3Message()
        {
            // Arrange: Create a V3 message (all fields)
            Span<byte> v3Buffer = stackalloc byte[V3_BLOCK_LENGTH];
            ref EvolvingOrderData v3Message = ref MemoryMarshal.AsRef<EvolvingOrderData>(v3Buffer);
            
            v3Message.OrderId = 11111;
            v3Message.Price = 9900;
            v3Message.Quantity = 150;
            v3Message.Side = 2;  // Sell

            // Act: Simulate V1 decoder which only knows about blockLength=16
            // It will read the first 16 bytes and skip the rest (9 bytes)
            var success = EvolvingOrderData.TryParse(v3Buffer, V1_BLOCK_LENGTH, out var decoded, out var remaining);

            // Assert
            Assert.True(success);
            // V1 decoder can read orderId and price
            Assert.Equal(11111, decoded.OrderId.Value);
            Assert.Equal(9900, decoded.Price.Value);
            // V1 decoder doesn't read quantity and side, but they exist in the struct
            // Since we're using a blittable struct, the values are actually there
            Assert.Equal(150, decoded.Quantity);  // Present in buffer, read as part of struct
            Assert.Equal(2, decoded.Side);        // Present in buffer, read as part of struct
            
            // Remaining data starts at position 16 (V1 block length)
            Assert.Equal(V3_BLOCK_LENGTH - V1_BLOCK_LENGTH, remaining.Length);
        }

        [Fact]
        public void SchemaEvolution_MessageSizeConstantReflectsLatestVersion()
        {
            // The MESSAGE_SIZE constant should reflect the latest schema version (V3)
            Assert.Equal(V3_BLOCK_LENGTH, EvolvingOrderData.MESSAGE_SIZE);
        }

        [Fact]
        public void SchemaEvolution_WithVariableData()
        {
            // Arrange: Create a V2 message followed by variable-length data
            int bufferSize = V2_BLOCK_LENGTH + 10;  // V2 message + 10 bytes variable data
            Span<byte> buffer = stackalloc byte[bufferSize];
            
            ref EvolvingOrderData v2Message = ref MemoryMarshal.AsRef<EvolvingOrderData>(buffer);
            v2Message.OrderId = 55555;
            v2Message.Price = 9800;
            v2Message.Quantity = 75;
            
            // Write some variable data after the message
            buffer.Slice(V2_BLOCK_LENGTH, 10).Fill(0xFF);

            // Act: Parse with V2 block length
            var success = EvolvingOrderData.TryParse(buffer, V2_BLOCK_LENGTH, out var decoded, out var remaining);

            // Assert
            Assert.True(success);
            Assert.Equal(55555, decoded.OrderId.Value);
            Assert.Equal(9800, decoded.Price.Value);
            Assert.Equal(75, decoded.Quantity);
            
            // Variable data should start at V2_BLOCK_LENGTH and be 10 bytes
            Assert.Equal(10, remaining.Length);
            Assert.All(remaining.ToArray(), b => Assert.Equal(0xFF, b));
        }

        [Fact]
        public void SchemaEvolution_WithSpanReaderAndBlockLength()
        {
            // Arrange: Create a V2 message
            Span<byte> v2Buffer = stackalloc byte[EvolvingOrderData.MESSAGE_SIZE + 5];  // MESSAGE_SIZE + extra bytes for testing
            v2Buffer.Clear();
            ref EvolvingOrderData v2Message = ref MemoryMarshal.AsRef<EvolvingOrderData>(v2Buffer);
            
            v2Message.OrderId = 33333;
            v2Message.Price = 10200;
            v2Message.Quantity = 250;

            // Act: Use TryParseWithReader with V2 block length
            var reader = new Versioning.Test.Runtime.SpanReader(v2Buffer);
            var success = EvolvingOrderData.TryParseWithReader(ref reader, V2_BLOCK_LENGTH, out var decoded);

            // Assert
            Assert.True(success);
            Assert.Equal(33333, decoded.OrderId.Value);
            Assert.Equal(10200, decoded.Price.Value);
            Assert.Equal(250, decoded.Quantity);
            
            // Reader should have consumed MESSAGE_SIZE bytes (not V2_BLOCK_LENGTH)
            // because TryRead<EvolvingOrderData> always reads the full struct
            // Then it skips 0 additional bytes since V2_BLOCK_LENGTH (24) < MESSAGE_SIZE (25)
            Assert.Equal(5, reader.RemainingBytes);
        }
    }
}
