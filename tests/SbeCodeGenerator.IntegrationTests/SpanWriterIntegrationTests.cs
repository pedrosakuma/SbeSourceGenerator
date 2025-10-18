using System;
using System.Runtime.InteropServices;
using Integration.Test.V0;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Integration tests for SpanWriter functionality demonstrating round-trip encoding/decoding.
    /// Tests verify that messages can be encoded and then decoded back to the original values.
    /// </summary>
    public class SpanWriterIntegrationTests
    {
        [Fact]
        public void TryEncode_SimpleMessage_RoundTripSucceeds()
        {
            // Arrange - Create a simple NewOrder message
            var original = new NewOrderData
            {
                OrderId = 123456,
                Price = 9950,
                Quantity = 100,
                Side = OrderSide.Buy,
                OrderType = OrderType.Limit
            };

            var buffer = new byte[NewOrderData.MESSAGE_SIZE];

            // Act - Encode
            bool encodeResult = original.TryEncode(buffer, out int bytesWritten);

            // Assert encoding succeeded
            Assert.True(encodeResult);
            Assert.Equal(NewOrderData.MESSAGE_SIZE, bytesWritten);

            // Act - Decode
            bool decodeResult = NewOrderData.TryParse(buffer, out var decoded, out _);

            // Assert decoding succeeded and values match
            Assert.True(decodeResult);
            Assert.Equal(original.OrderId, decoded.OrderId);
            Assert.Equal(original.Price, decoded.Price);
            Assert.Equal(original.Quantity, decoded.Quantity);
            Assert.Equal(original.Side, decoded.Side);
            Assert.Equal(original.OrderType, decoded.OrderType);
        }

        [Fact]
        public void TryEncode_WithTooSmallBuffer_ReturnsFalse()
        {
            // Arrange
            var message = new NewOrderData
            {
                OrderId = 123,
                Price = 100,
                Quantity = 10,
                Side = OrderSide.Sell,
                OrderType = OrderType.Market
            };

            var tooSmallBuffer = new byte[NewOrderData.MESSAGE_SIZE - 1];

            // Act
            bool result = message.TryEncode(tooSmallBuffer, out int bytesWritten);

            // Assert
            Assert.False(result);
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void Encode_ThrowingVersion_WithValidBuffer_Succeeds()
        {
            // Arrange
            var message = new NewOrderData
            {
                OrderId = 789,
                Price = 1000,
                Quantity = 50,
                Side = OrderSide.Buy,
                OrderType = OrderType.Limit
            };

            var buffer = new byte[NewOrderData.MESSAGE_SIZE];

            // Act
            int bytesWritten = message.Encode(buffer);

            // Assert
            Assert.Equal(NewOrderData.MESSAGE_SIZE, bytesWritten);

            // Verify round-trip
            NewOrderData.TryParse(buffer, out var decoded, out _);
            Assert.Equal(message.OrderId, decoded.OrderId);
        }

        [Fact]
        public void Encode_ThrowingVersion_WithTooSmallBuffer_Throws()
        {
            // Arrange
            var message = new NewOrderData { OrderId = 123 };
            var tooSmallBuffer = new byte[5];

            // Act & Assert
            bool exceptionThrown = false;
            try
            {
                message.Encode(tooSmallBuffer);
            }
            catch (InvalidOperationException ex)
            {
                exceptionThrown = true;
                Assert.Contains("Failed to encode", ex.Message);
                Assert.Contains("NewOrderData", ex.Message);
            }

            Assert.True(exceptionThrown);
        }

        [Fact]
        public void TryEncodeWithWriter_UsingExistingWriter_Works()
        {
            // Arrange
            var message = new NewOrderData
            {
                OrderId = 999,
                Price = 2000,
                Quantity = 75,
                Side = OrderSide.Sell,
                OrderType = OrderType.Limit
            };

            var buffer = new byte[100]; // Larger buffer for demonstration
            var writer = new Integration.Test.V0.Runtime.SpanWriter(buffer);

            // Act
            bool result = message.TryEncodeWithWriter(ref writer);

            // Assert
            Assert.True(result);
            Assert.Equal(NewOrderData.MESSAGE_SIZE, writer.BytesWritten);

            // Verify round-trip
            NewOrderData.TryParse(buffer, out var decoded, out _);
            Assert.Equal(message.OrderId, decoded.OrderId);
            Assert.Equal(message.Price, decoded.Price);
            Assert.Equal(message.Quantity, decoded.Quantity);
        }

        [Fact]
        public void RoundTrip_MultipleMessages_AllDataPreserved()
        {
            // Test that encoding/decoding preserves all field values across different message instances
            var testCases = new[]
            {
                new NewOrderData { OrderId = 1, Price = 100, Quantity = 10, Side = OrderSide.Buy, OrderType = OrderType.Limit },
                new NewOrderData { OrderId = long.MaxValue, Price = long.MaxValue, Quantity = long.MaxValue, Side = OrderSide.Sell, OrderType = OrderType.Market },
                new NewOrderData { OrderId = 0, Price = 0, Quantity = 0, Side = (OrderSide)0, OrderType = (OrderType)0 }
            };

            foreach (var original in testCases)
            {
                // Arrange
                var buffer = new byte[NewOrderData.MESSAGE_SIZE];

                // Act - Encode
                bool encodeResult = original.TryEncode(buffer, out int bytesWritten);

                // Assert encoding
                Assert.True(encodeResult);

                // Act - Decode
                bool decodeResult = NewOrderData.TryParse(buffer, out var decoded, out _);

                // Assert decoding and value preservation
                Assert.True(decodeResult);
                Assert.Equal(original.OrderId, decoded.OrderId);
                Assert.Equal(original.Price, decoded.Price);
                Assert.Equal(original.Quantity, decoded.Quantity);
                Assert.Equal(original.Side, decoded.Side);
                Assert.Equal(original.OrderType, decoded.OrderType);
            }
        }

        [Fact]
        public void MultipleMessagesInSequence_CanBeEncodedAndDecoded()
        {
            // Arrange - Encode multiple messages into one buffer
            var buffer = new byte[NewOrderData.MESSAGE_SIZE * 3];
            var writer = new Integration.Test.V0.Runtime.SpanWriter(buffer);

            var message1 = new NewOrderData { OrderId = 1, Price = 100, Quantity = 10, Side = OrderSide.Buy, OrderType = OrderType.Limit };
            var message2 = new NewOrderData { OrderId = 2, Price = 200, Quantity = 20, Side = OrderSide.Sell, OrderType = OrderType.Market };
            var message3 = new NewOrderData { OrderId = 3, Price = 300, Quantity = 30, Side = OrderSide.Buy, OrderType = OrderType.Limit };

            // Act - Encode all three messages
            Assert.True(message1.TryEncodeWithWriter(ref writer));
            Assert.True(message2.TryEncodeWithWriter(ref writer));
            Assert.True(message3.TryEncodeWithWriter(ref writer));

            Assert.Equal(NewOrderData.MESSAGE_SIZE * 3, writer.BytesWritten);

            // Act - Decode all three messages
            var reader = new Integration.Test.V0.Runtime.SpanReader(buffer);

            Assert.True(reader.TryRead<NewOrderData>(out var decoded1));
            Assert.True(reader.TryRead<NewOrderData>(out var decoded2));
            Assert.True(reader.TryRead<NewOrderData>(out var decoded3));

            // Assert
            Assert.Equal(message1.OrderId, decoded1.OrderId);
            Assert.Equal(message2.OrderId, decoded2.OrderId);
            Assert.Equal(message3.OrderId, decoded3.OrderId);
            Assert.Equal(message1.Price, decoded1.Price);
            Assert.Equal(message2.Price, decoded2.Price);
            Assert.Equal(message3.Price, decoded3.Price);
        }

        [Fact]
        public void OrderBookMessage_RoundTrip_Succeeds()
        {
            // Arrange - Test with a different message type
            var original = new OrderBookData
            {
                InstrumentId = 123
            };

            var buffer = new byte[OrderBookData.MESSAGE_SIZE];

            // Act - Encode
            bool encodeResult = original.TryEncode(buffer, out int bytesWritten);

            // Assert encoding
            Assert.True(encodeResult);
            Assert.Equal(OrderBookData.MESSAGE_SIZE, bytesWritten);

            // Act - Decode
            bool decodeResult = OrderBookData.TryParse(buffer, out var decoded, out _);

            // Assert decoding
            Assert.True(decodeResult);
            Assert.Equal(original.InstrumentId, decoded.InstrumentId);
        }

        [Fact]
        public void WriterAndReader_SymmetricOperations_ProduceConsistentResults()
        {
            // This test verifies that SpanWriter and SpanReader are truly symmetric
            var buffer = new byte[100];
            var writer = new Integration.Test.V0.Runtime.SpanWriter(buffer);

            // Write various types in sequence
            writer.Write((int)42);
            writer.Write((long)123456789L);
            writer.Write((byte)255);
            writer.Write((ushort)65535);

            int totalWritten = writer.BytesWritten;

            // Read them back in the same sequence
            var reader = new Integration.Test.V0.Runtime.SpanReader(buffer);

            Assert.True(reader.TryRead<int>(out var intVal));
            Assert.True(reader.TryRead<long>(out var longVal));
            Assert.True(reader.TryRead<byte>(out var byteVal));
            Assert.True(reader.TryRead<ushort>(out var ushortVal));

            // Assert
            Assert.Equal(42, intVal);
            Assert.Equal(123456789L, longVal);
            Assert.Equal(255, byteVal);
            Assert.Equal(65535, ushortVal);
            Assert.Equal(totalWritten, buffer.Length - reader.RemainingBytes);
        }
    }
}
