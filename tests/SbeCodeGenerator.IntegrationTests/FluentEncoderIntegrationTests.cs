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
            Assert.True(Integration.Test.V0.OrderBookData.TryParse(buffer, out var decoded));
            Assert.Equal(42, decoded.Data.InstrumentId);

            // Verify groups
            var decodedBids = new List<Integration.Test.V0.OrderBookData.BidsData>();
            var decodedAsks = new List<Integration.Test.V0.OrderBookData.AsksData>();

            decoded.ReadGroups(
                (in Integration.Test.V0.OrderBookData.BidsData bid) => decodedBids.Add(bid),
                (in Integration.Test.V0.OrderBookData.AsksData ask) => decodedAsks.Add(ask)
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
            Assert.True(Integration.Test.V0.NewOrderData.TryParse(buffer, out var decoded));
            Assert.Equal(123, decoded.Data.OrderId.Value);

            // Verify varData
            string decodedSymbol = "";
            decoded.ReadGroups(
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
            Assert.True(Integration.Test.V0.OrderBookData.TryParse(buffer, out var decoded));
            Assert.Equal(99, decoded.Data.InstrumentId);

            var bidCount = 0;
            var askCount = 0;
            decoded.ReadGroups(
                (in Integration.Test.V0.OrderBookData.BidsData bid) => bidCount++,
                (in Integration.Test.V0.OrderBookData.AsksData ask) => askCount++
            );

            Assert.Equal(3, bidCount);
            Assert.Equal(2, askCount);
        }

        [Fact]
        public void TryEncode_CallbackBased_ZeroAllocation()
        {
            // This test demonstrates the zero-allocation callback-based API
            
            // Arrange
            Span<byte> buffer = stackalloc byte[1024];
            
            var orderBook = new Integration.Test.V0.OrderBookData
            {
                InstrumentId = 123
            };

            // Simulate data source without allocating arrays
            var bidPrices = new long[] { 100, 101, 102 };
            var bidQuantities = new long[] { 10, 11, 12 };
            var askPrices = new long[] { 200, 201 };
            var askQuantities = new long[] { 20, 21 };

            // Act - Use callback-based API for zero-allocation encoding
            bool success = Integration.Test.V0.OrderBookData.TryEncode(
                orderBook,
                buffer,
                bidPrices.Length,
                (int index, ref Integration.Test.V0.OrderBookData.BidsData item) =>
                {
                    item.Price = bidPrices[index];
                    item.Quantity = bidQuantities[index];
                },
                askPrices.Length,
                (int index, ref Integration.Test.V0.OrderBookData.AsksData item) =>
                {
                    item.Price = askPrices[index];
                    item.Quantity = askQuantities[index];
                },
                out int bytesWritten
            );

            // Assert
            Assert.True(success);
            Assert.True(bytesWritten > 0);

            // Decode and verify
            Assert.True(Integration.Test.V0.OrderBookData.TryParse(buffer, out var decoded));
            Assert.Equal(123, decoded.Data.InstrumentId);

            var decodedBids = new List<Integration.Test.V0.OrderBookData.BidsData>();
            var decodedAsks = new List<Integration.Test.V0.OrderBookData.AsksData>();

            decoded.ReadGroups(
                (in Integration.Test.V0.OrderBookData.BidsData bid) => decodedBids.Add(bid),
                (in Integration.Test.V0.OrderBookData.AsksData ask) => decodedAsks.Add(ask)
            );

            Assert.Equal(3, decodedBids.Count);
            Assert.Equal(2, decodedAsks.Count);
            Assert.Equal(100, decodedBids[0].Price.Value);
            Assert.Equal(101, decodedBids[1].Price.Value);
            Assert.Equal(102, decodedBids[2].Price.Value);
            Assert.Equal(200, decodedAsks[0].Price.Value);
            Assert.Equal(201, decodedAsks[1].Price.Value);
        }

        [Fact]
        public void TryEncode_CallbackVsSpan_ProduceSameResult()
        {
            // This test verifies callback-based and span-based APIs produce identical output
            
            // Arrange
            Span<byte> bufferCallback = stackalloc byte[1024];
            Span<byte> bufferSpan = stackalloc byte[1024];
            
            var orderBook = new Integration.Test.V0.OrderBookData
            {
                InstrumentId = 99
            };

            var bidsArray = new Integration.Test.V0.OrderBookData.BidsData[]
            {
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1000, Quantity = 100 },
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1001, Quantity = 101 }
            };

            var asksArray = new Integration.Test.V0.OrderBookData.AsksData[]
            {
                new Integration.Test.V0.OrderBookData.AsksData { Price = 2000, Quantity = 200 }
            };

            // Act - Span-based API
            bool successSpan = Integration.Test.V0.OrderBookData.TryEncode(
                orderBook,
                bufferSpan,
                bidsArray,
                asksArray,
                out int bytesWrittenSpan
            );

            // Act - Callback-based API
            bool successCallback = Integration.Test.V0.OrderBookData.TryEncode(
                orderBook,
                bufferCallback,
                bidsArray.Length,
                (int index, ref Integration.Test.V0.OrderBookData.BidsData item) => item = bidsArray[index],
                asksArray.Length,
                (int index, ref Integration.Test.V0.OrderBookData.AsksData item) => item = asksArray[index],
                out int bytesWrittenCallback
            );

            // Assert - Both APIs produce identical results
            Assert.True(successSpan);
            Assert.True(successCallback);
            Assert.Equal(bytesWrittenSpan, bytesWrittenCallback);
            Assert.True(bufferSpan.Slice(0, bytesWrittenSpan).SequenceEqual(bufferCallback.Slice(0, bytesWrittenCallback)));
        }
    }
}
