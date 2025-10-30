using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Integration tests for the improved encoder API
    /// Tests the new TryEncode() method that enforces correct schema order
    /// </summary>
    public class ImprovedEncoderIntegrationTests
    {
        [Fact]
        public void TryEncode_WithGroups_EncodesInCorrectOrder()
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

            // Act - Use new TryEncode API with all parameters in schema order
            bool success = Integration.Test.V0.OrderBookData.TryEncode(
                orderBook, 
                buffer, 
                bids,  // First group in schema
                asks,  // Second group in schema
                out int bytesWritten
            );

            // Assert - Verify encoding worked correctly
            Assert.True(success);
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
            Assert.Single(decodedAsks);
            Assert.Equal(1000, decodedBids[0].Price.Value);
            Assert.Equal(2000, decodedAsks[0].Price.Value);
        }

        [Fact]
        public void TryEncode_WithVarData_EncodesCorrectly()
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

            // Act - Use new TryEncode API with varData
            bool success = Integration.Test.V0.NewOrderData.TryEncode(
                order,
                buffer,
                symbolBytes,  // VarData in schema order
                out int bytesWritten
            );

            // Assert
            Assert.True(success);
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
        public void TryEncode_InsufficientBuffer_ReturnsFalse()
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

            var asks = Array.Empty<Integration.Test.V0.OrderBookData.AsksData>();

            // Act - Try to encode with insufficient buffer
            bool success = Integration.Test.V0.OrderBookData.TryEncode(
                orderBook,
                buffer,
                bids,
                asks,
                out int bytesWritten
            );

            // Assert - Should fail due to buffer being too small
            Assert.False(success);
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void TryEncode_ParameterOrder_EnforcesSchemaOrder()
        {
            // This test demonstrates that the API enforces correct order at compile time
            // The method signature requires: TryEncode(message, buffer, bids, asks, out bytesWritten)
            // You cannot pass asks before bids - the compiler will prevent it
            
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

            // Act - Parameters must be in schema-defined order (bids, then asks)
            bool success = Integration.Test.V0.OrderBookData.TryEncode(
                orderBook,
                buffer,
                bids,   // MUST be first (as defined in schema)
                asks,   // MUST be second (as defined in schema)
                out int bytesWritten
            );

            // Assert
            Assert.True(success);
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
        public void TryEncode_CompareWithOldAPI_ProducesSameResult()
        {
            // This test demonstrates that the new API produces identical output to the old API
            
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

            // Act - Old API (manual BeginEncoding + individual TryEncode calls)
            Assert.True(orderBook.BeginEncoding(bufferOld, out var writerOld));
            Assert.True(Integration.Test.V0.OrderBookData.TryEncodeBids(ref writerOld, bids));
            Assert.True(Integration.Test.V0.OrderBookData.TryEncodeAsks(ref writerOld, asks));
            int bytesWrittenOld = writerOld.BytesWritten;

            // Act - New API (single TryEncode call with all parameters)
            bool successNew = Integration.Test.V0.OrderBookData.TryEncode(
                orderBook,
                bufferNew,
                bids,
                asks,
                out int bytesWrittenNew
            );

            // Assert - Both produce identical results
            Assert.True(successNew);
            Assert.Equal(bytesWrittenOld, bytesWrittenNew);
            Assert.True(bufferOld.Slice(0, bytesWrittenOld).SequenceEqual(bufferNew.Slice(0, bytesWrittenNew)));
        }
    }
}
