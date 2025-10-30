using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Integration tests for the fluent encoder API
    /// Tests the new CreateEncoder() fluent builder pattern for improved usability
    /// </summary>
    public class FluentEncoderIntegrationTests
    {
        [Fact]
        public void FluentEncoder_WithGroups_EncodesCorrectly()
        {
            // Arrange - Create message and groups
            Span<byte> buffer = stackalloc byte[1024];
            
            var orderBook = new Integration.Test.V0.OrderBookData
            {
                InstrumentId = 42
            };

            var bids = new Integration.Test.V0.OrderBookData.BidsData[]
            {
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1000, Quantity = 100 },
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1010, Quantity = 101 }
            };

            var asks = new Integration.Test.V0.OrderBookData.AsksData[]
            {
                new Integration.Test.V0.OrderBookData.AsksData { Price = 2000, Quantity = 200 }
            };

            // Act - Use fluent encoder API
            var encoder = orderBook.CreateEncoder(buffer)
                .WithBids(bids)
                .WithAsks(asks);

            int bytesWritten = encoder.BytesWritten;

            // Assert - Verify encoding worked correctly
            Assert.True(bytesWritten > 0);

            // Decode and verify
            Assert.True(Integration.Test.V0.OrderBookData.TryParse(buffer, out var decoded, out var variableData));
            Assert.Equal(42, decoded.InstrumentId);

            // Verify groups
            var decodedBids = new List<Integration.Test.V0.OrderBookData.BidsData>();
            var decodedAsks = new List<Integration.Test.V0.OrderBookData.AsksData>();

            decoded.ConsumeVariableLengthSegments(
                variableData,
                bid => decodedBids.Add(bid),
                ask => decodedAsks.Add(ask)
            );

            Assert.Equal(2, decodedBids.Count);
            Assert.Equal(1, decodedAsks.Count);
            Assert.Equal(1000, decodedBids[0].Price.Value);
            Assert.Equal(2000, decodedAsks[0].Price.Value);
        }

        [Fact]
        public void FluentEncoder_WithVarData_EncodesCorrectly()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[512];
            
            var order = new Integration.Test.V0.NewOrderData
            {
                OrderId = 123,
                Price = 9950,
                Quantity = 100,
                Side = Integration.Test.V0.OrderSide.Buy,
                OrderType = Integration.Test.V0.OrderType.Limit
            };

            string symbol = "AAPL";
            var symbolBytes = Encoding.UTF8.GetBytes(symbol);

            // Act - Use fluent encoder API
            var encoder = order.CreateEncoder(buffer)
                .WithSymbol(symbolBytes);

            int bytesWritten = encoder.BytesWritten;

            // Assert
            Assert.True(bytesWritten > 0);

            // Decode and verify
            Assert.True(Integration.Test.V0.NewOrderData.TryParse(buffer, out var decoded, out var variableData));
            Assert.Equal(123, decoded.OrderId.Value);

            // Verify varData
            string decodedSymbol = "";
            decoded.ConsumeVariableLengthSegments(
                variableData,
                symbolData => {
                    decodedSymbol = Encoding.UTF8.GetString(symbolData.VarData.Slice(0, symbolData.Length));
                }
            );

            Assert.Equal(symbol, decodedSymbol);
        }

        [Fact]
        public void FluentEncoder_TryWithVariant_HandleFailuresGracefully()
        {
            // Arrange - Very small buffer to force failure
            Span<byte> buffer = stackalloc byte[10];
            
            var orderBook = new Integration.Test.V0.OrderBookData
            {
                InstrumentId = 42
            };

            var bids = new Integration.Test.V0.OrderBookData.BidsData[]
            {
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1000, Quantity = 100 }
            };

            // Act - Use TryWith variant
            var encoder = orderBook.CreateEncoder(buffer);
            bool success = encoder.TryWithBids(bids);

            // Assert - Should fail due to buffer being too small
            Assert.False(success);
        }

        [Fact]
        public void FluentEncoder_ChainedCalls_WorksCorrectly()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[1024];
            
            var orderBook = new Integration.Test.V0.OrderBookData
            {
                InstrumentId = 99
            };

            var bids = new Integration.Test.V0.OrderBookData.BidsData[]
            {
                new Integration.Test.V0.OrderBookData.BidsData { Price = 100, Quantity = 10 },
                new Integration.Test.V0.OrderBookData.BidsData { Price = 101, Quantity = 11 },
                new Integration.Test.V0.OrderBookData.BidsData { Price = 102, Quantity = 12 }
            };

            var asks = new Integration.Test.V0.OrderBookData.AsksData[]
            {
                new Integration.Test.V0.OrderBookData.AsksData { Price = 200, Quantity = 20 },
                new Integration.Test.V0.OrderBookData.AsksData { Price = 201, Quantity = 21 }
            };

            // Act - Chain multiple groups in a fluent manner
            var bytesWritten = orderBook.CreateEncoder(buffer)
                .WithBids(bids)
                .WithAsks(asks)
                .BytesWritten;

            // Assert
            Assert.True(bytesWritten > Integration.Test.V0.OrderBookData.MESSAGE_SIZE);

            // Verify by decoding
            Assert.True(Integration.Test.V0.OrderBookData.TryParse(buffer, out var decoded, out var variableData));
            Assert.Equal(99, decoded.InstrumentId);

            var bidCount = 0;
            var askCount = 0;
            decoded.ConsumeVariableLengthSegments(
                variableData,
                bid => bidCount++,
                ask => askCount++
            );

            Assert.Equal(3, bidCount);
            Assert.Equal(2, askCount);
        }

        [Fact]
        public void FluentEncoder_CompareWithOldAPI_ProducesSameResult()
        {
            // This test demonstrates that the new fluent API produces identical output to the old API
            
            // Arrange
            Span<byte> bufferOld = stackalloc byte[1024];
            Span<byte> bufferNew = stackalloc byte[1024];
            
            var orderBook = new Integration.Test.V0.OrderBookData
            {
                InstrumentId = 42
            };

            var bids = new Integration.Test.V0.OrderBookData.BidsData[]
            {
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1000, Quantity = 100 }
            };

            var asks = new Integration.Test.V0.OrderBookData.AsksData[]
            {
                new Integration.Test.V0.OrderBookData.AsksData { Price = 2000, Quantity = 200 }
            };

            // Act - Old API
            Assert.True(orderBook.BeginEncoding(bufferOld, out var writerOld));
            Assert.True(Integration.Test.V0.OrderBookData.TryEncodeBids(ref writerOld, bids));
            Assert.True(Integration.Test.V0.OrderBookData.TryEncodeAsks(ref writerOld, asks));
            int bytesWrittenOld = writerOld.BytesWritten;

            // Act - New Fluent API
            var encoderNew = orderBook.CreateEncoder(bufferNew)
                .WithBids(bids)
                .WithAsks(asks);
            int bytesWrittenNew = encoderNew.BytesWritten;

            // Assert - Both produce identical results
            Assert.Equal(bytesWrittenOld, bytesWrittenNew);
            Assert.True(bufferOld.Slice(0, bytesWrittenOld).SequenceEqual(bufferNew.Slice(0, bytesWrittenNew)));
        }
    }
}
