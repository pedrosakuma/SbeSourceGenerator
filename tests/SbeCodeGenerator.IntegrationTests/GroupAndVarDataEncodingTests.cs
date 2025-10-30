using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Integration tests for Group and VarData encoding using the comprehensive TryEncode API
    /// These tests verify that the allocation-free and allocation-based encoding flows work correctly
    /// </summary>
    public class GroupAndVarDataEncodingTests
    {
        [Fact]
        public void GroupEncoding_EmptyGroups_EncodesCorrectly()
        {
            // Arrange - Empty groups
            Span<byte> buffer = stackalloc byte[512];
            var orderBook = new Integration.Test.V0.OrderBookData { InstrumentId = 42 };
            var bids = Array.Empty<Integration.Test.V0.OrderBookData.BidsData>();
            var asks = Array.Empty<Integration.Test.V0.OrderBookData.AsksData>();

            // Act - Use comprehensive TryEncode with empty groups
            bool success = Integration.Test.V0.OrderBookData.TryEncode(
                orderBook,
                buffer,
                bids,
                asks,
                out int bytesWritten
            );

            // Assert
            Assert.True(success);
            Assert.True(bytesWritten > 0);
            
            // Decode and verify
            Assert.True(Integration.Test.V0.OrderBookData.TryParse(buffer, out var decoded, out var variableData));
            Assert.Equal(42, decoded.InstrumentId);
        }

        [Fact]
        public void GroupEncoding_TwoGroups_EncodesCorrectly()
        {
            // Arrange - Create buffer and data for both bids and asks
            Span<byte> buffer = stackalloc byte[512];
            var orderBook = new Integration.Test.V0.OrderBookData { InstrumentId = 99 };
            var bids = new Integration.Test.V0.OrderBookData.BidsData[]
            {
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1000, Quantity = 100 },
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1010, Quantity = 101 }
            };
            var asks = new Integration.Test.V0.OrderBookData.AsksData[]
            {
                new Integration.Test.V0.OrderBookData.AsksData { Price = 2000, Quantity = 200 }
            };

            // Act - Use comprehensive TryEncode
            bool success = Integration.Test.V0.OrderBookData.TryEncode(
                orderBook,
                buffer,
                bids,
                asks,
                out int bytesWritten
            );

            // Assert
            Assert.True(success);
            Assert.True(bytesWritten > 0);
            
            // Decode and verify both groups
            Assert.True(Integration.Test.V0.OrderBookData.TryParse(buffer, out var decoded, out var variableData));
            Assert.Equal(99, decoded.InstrumentId);
            
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
        public void GroupEncoding_InsufficientBuffer_ReturnsFalse()
        {
            // Arrange - Buffer too small
            Span<byte> buffer = stackalloc byte[10]; // Too small
            var orderBook = new Integration.Test.V0.OrderBookData { InstrumentId = 42 };
            var bids = new Integration.Test.V0.OrderBookData.BidsData[]
            {
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1000, Quantity = 100 }
            };
            var asks = Array.Empty<Integration.Test.V0.OrderBookData.AsksData>();

            // Act
            bool success = Integration.Test.V0.OrderBookData.TryEncode(
                orderBook,
                buffer,
                bids,
                asks,
                out int bytesWritten
            );

            // Assert
            Assert.False(success);
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void VarDataEncoding_WithSymbol_EncodesCorrectly()
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

            // Act - Use comprehensive TryEncode
            bool success = Integration.Test.V0.NewOrderData.TryEncode(
                order,
                buffer,
                symbolBytes,
                out int bytesWritten
            );

            // Assert
            Assert.True(success);
            Assert.True(bytesWritten > 0);
            
            // Decode and verify
            Assert.True(Integration.Test.V0.NewOrderData.TryParse(buffer, out var decoded, out var variableData));
            Assert.Equal(123, decoded.OrderId.Value);
            
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
        public void VarDataEncoding_EmptyData_EncodesCorrectly()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[512];
            var order = new Integration.Test.V0.NewOrderData
            {
                OrderId = 456,
                Price = 1000,
                Quantity = 10,
                Side = Integration.Test.V0.OrderSide.Sell,
                OrderType = Integration.Test.V0.OrderType.Market
            };
            var emptyData = Array.Empty<byte>();

            // Act
            bool success = Integration.Test.V0.NewOrderData.TryEncode(
                order,
                buffer,
                emptyData,
                out int bytesWritten
            );

            // Assert
            Assert.True(success);
            Assert.True(bytesWritten > 0);
        }

        [Fact]
        public void VarDataEncoding_TooLong_ReturnsFalse()
        {
            // Arrange - Data longer than 255 bytes (max for uint8 length)
            Span<byte> buffer = stackalloc byte[512];
            var order = new Integration.Test.V0.NewOrderData
            {
                OrderId = 789,
                Price = 2000,
                Quantity = 20,
                Side = Integration.Test.V0.OrderSide.Buy,
                OrderType = Integration.Test.V0.OrderType.Limit
            };
            var tooLongData = new byte[256]; // 256 > 255

            // Act
            bool success = Integration.Test.V0.NewOrderData.TryEncode(
                order,
                buffer,
                tooLongData,
                out int bytesWritten
            );

            // Assert
            Assert.False(success); // Should fail because length > 255
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void VarDataEncoding_InsufficientBuffer_ReturnsFalse()
        {
            // Arrange - Buffer too small
            Span<byte> buffer = stackalloc byte[2]; // Too small
            var order = new Integration.Test.V0.NewOrderData
            {
                OrderId = 111,
                Price = 3000,
                Quantity = 30,
                Side = Integration.Test.V0.OrderSide.Sell,
                OrderType = Integration.Test.V0.OrderType.Limit
            };
            var data = new byte[5];

            // Act
            bool success = Integration.Test.V0.NewOrderData.TryEncode(
                order,
                buffer,
                data,
                out int bytesWritten
            );

            // Assert
            Assert.False(success);
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void CompleteMessageWithGroups_RoundTrip_PreservesData()
        {
            // Arrange - Create a complete OrderBook message with groups
            Span<byte> buffer = stackalloc byte[1024];
            
            var orderBook = new Integration.Test.V0.OrderBookData
            {
                InstrumentId = 42
            };

            var bids = new Integration.Test.V0.OrderBookData.BidsData[]
            {
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1000, Quantity = 100 },
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1010, Quantity = 101 },
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1020, Quantity = 102 }
            };

            var asks = new Integration.Test.V0.OrderBookData.AsksData[]
            {
                new Integration.Test.V0.OrderBookData.AsksData { Price = 2000, Quantity = 200 },
                new Integration.Test.V0.OrderBookData.AsksData { Price = 2010, Quantity = 201 }
            };

            // Act - Encode message with groups using comprehensive TryEncode
            bool success = Integration.Test.V0.OrderBookData.TryEncode(
                orderBook,
                buffer,
                bids,
                asks,
                out int totalBytesWritten
            );

            // Assert
            Assert.True(success);
            Assert.True(totalBytesWritten > 0);

            // Decode the fixed part
            Assert.True(Integration.Test.V0.OrderBookData.TryParse(buffer, out var decodedMessage, out var variableData));
            Assert.Equal(42, decodedMessage.InstrumentId);

            // Decode the groups using ConsumeVariableLengthSegments
            var decodedBids = new List<Integration.Test.V0.OrderBookData.BidsData>();
            var decodedAsks = new List<Integration.Test.V0.OrderBookData.AsksData>();

            decodedMessage.ConsumeVariableLengthSegments(
                variableData,
                bid => decodedBids.Add(bid),
                ask => decodedAsks.Add(ask)
            );

            // Assert - Verify all data matches
            Assert.Equal(3, decodedBids.Count);
            Assert.Equal(2, decodedAsks.Count);

            for (int i = 0; i < bids.Length; i++)
            {
                Assert.Equal(bids[i].Price.Value, decodedBids[i].Price.Value);
                Assert.Equal(bids[i].Quantity, decodedBids[i].Quantity);
            }

            for (int i = 0; i < asks.Length; i++)
            {
                Assert.Equal(asks[i].Price.Value, decodedAsks[i].Price.Value);
                Assert.Equal(asks[i].Quantity, decodedAsks[i].Quantity);
            }
        }

        [Fact]
        public void CompleteMessageWithVarData_RoundTrip_PreservesData()
        {
            // Arrange - Create a complete NewOrder message with varData
            Span<byte> buffer = stackalloc byte[1024];
            
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

            // Act - Encode message with varData using comprehensive TryEncode
            bool success = Integration.Test.V0.NewOrderData.TryEncode(
                order,
                buffer,
                symbolBytes,
                out int bytesWritten
            );

            // Assert
            Assert.True(success);
            Assert.True(bytesWritten > 0);

            // Decode the fixed part
            Assert.True(Integration.Test.V0.NewOrderData.TryParse(buffer, out var decodedOrder, out var variableData));
            Assert.Equal(123, decodedOrder.OrderId.Value);
            Assert.Equal(9950, decodedOrder.Price.Value);
            Assert.Equal(100, decodedOrder.Quantity);

            // Decode the varData using ConsumeVariableLengthSegments
            string decodedSymbol = "";
            byte symbolLength = 0;
            decodedOrder.ConsumeVariableLengthSegments(
                variableData,
                symbolData => {
                    symbolLength = symbolData.Length;
                    // VarData contains the actual data, but we need to use Length to know how much to read
                    decodedSymbol = Encoding.UTF8.GetString(symbolData.VarData.Slice(0, symbolLength));
                }
            );

            // Assert - Verify symbol matches
            Assert.Equal(symbol, decodedSymbol);
            Assert.Equal((byte)symbolBytes.Length, symbolLength);
        }
    }
}
