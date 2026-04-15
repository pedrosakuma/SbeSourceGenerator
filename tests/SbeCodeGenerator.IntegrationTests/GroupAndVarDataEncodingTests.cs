using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Integration tests for Group and VarData encoding using the comprehensive TryEncode API
    /// These tests verify the span-based (allocation-based) encoding flow
    /// For callback-based (allocation-free) tests, see FluentEncoderIntegrationTests.cs
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
            Assert.True(Integration.Test.V0.OrderBookData.TryParse(buffer, out var decoded));
            Assert.Equal(42, decoded.Data.InstrumentId);
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
            Assert.True(Integration.Test.V0.OrderBookData.TryParse(buffer, out var decoded));
            Assert.Equal(99, decoded.Data.InstrumentId);
            
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
            Assert.True(Integration.Test.V0.NewOrderData.TryParse(buffer, out var decoded));
            Assert.Equal(123, decoded.Data.OrderId.Value);
            
            string decodedSymbol = "";
            decoded.ReadGroups(
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
            Assert.True(Integration.Test.V0.OrderBookData.TryParse(buffer, out var decodedMessage));
            Assert.Equal(42, decodedMessage.Data.InstrumentId);

            // Decode the groups using ReadGroups
            var decodedBids = new List<Integration.Test.V0.OrderBookData.BidsData>();
            var decodedAsks = new List<Integration.Test.V0.OrderBookData.AsksData>();

            decodedMessage.ReadGroups(
                (in Integration.Test.V0.OrderBookData.BidsData bid) => decodedBids.Add(bid),
                (in Integration.Test.V0.OrderBookData.AsksData ask) => decodedAsks.Add(ask)
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
            Assert.True(Integration.Test.V0.NewOrderData.TryParse(buffer, out var decodedOrder));
            Assert.Equal(123, decodedOrder.Data.OrderId.Value);
            Assert.Equal(9950, decodedOrder.Data.Price.Value);
            Assert.Equal(100, decodedOrder.Data.Quantity);

            // Decode the varData using ReadGroups
            string? decodedSymbol = null;
            byte symbolLength = 0;
            decodedOrder.ReadGroups(
                symbolData => {
                    symbolLength = symbolData.Length;
                    // VarData contains the actual data, but we need to use Length to know how much to read
                    decodedSymbol = Encoding.UTF8.GetString(symbolData.VarData.Slice(0, symbolLength));
                }
            );

            // Assert - Verify symbol matches
            Assert.NotNull(decodedSymbol);
            Assert.Equal(symbol, decodedSymbol);
            Assert.Equal((byte)symbolBytes.Length, symbolLength);
        }

        [Fact]
        public void VarDataCreate_EmptyBuffer_ReturnsEmptyInstance()
        {
            // Arrange - Simulate the scenario from issue #142:
            // When groups consume all remaining bytes, varData.Create receives an empty buffer
            ReadOnlySpan<byte> emptyBuffer = ReadOnlySpan<byte>.Empty;

            // Act - This previously threw ArgumentOutOfRangeException
            var result = Integration.Test.V0.VarString8.Create(emptyBuffer);

            // Assert
            Assert.Equal(0, result.Length);
            Assert.Equal(0, result.VarData.Length);
            // TotalLength is computed (lengthSize + Length = 1 + 0 = 1), but TrySkip handles this gracefully
            Assert.Equal(1, result.TotalLength);
        }

        [Fact]
        public void VarDataCreate_TruncatedBuffer_ClampsDataLength()
        {
            // Arrange - Buffer has length prefix (says 10 bytes) but only 3 data bytes available
            Span<byte> truncated = stackalloc byte[4]; // 1 byte length + 3 bytes data
            truncated[0] = 10; // length prefix claims 10 bytes
            truncated[1] = 0x41; // 'A'
            truncated[2] = 0x42; // 'B'
            truncated[3] = 0x43; // 'C'

            // Act - Should not throw, should clamp to available data
            var result = Integration.Test.V0.VarString8.Create(truncated);

            // Assert - Length field reflects wire value, but VarData is clamped
            Assert.Equal(10, result.Length);
            Assert.Equal(3, result.VarData.Length);
            Assert.Equal((byte)'A', result.VarData[0]);
            Assert.Equal((byte)'B', result.VarData[1]);
            Assert.Equal((byte)'C', result.VarData[2]);
        }
    }
}
