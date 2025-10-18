using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Integration tests for Phase 3: Group and VarData encoding support
    /// </summary>
    public class GroupAndVarDataEncodingTests
    {
        [Fact]
        public void GroupEncoding_TryEncodeBids_EncodesCorrectly()
        {
            // Arrange - Create buffer and bids data
            Span<byte> buffer = stackalloc byte[512];
            var bids = new Integration.Test.V0.OrderBookData.BidsData[]
            {
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1000, Quantity = 100 },
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1010, Quantity = 101 },
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1020, Quantity = 102 }
            };

            // Act - Encode using TryEncodeBids
            var writer = new Integration.Test.V0.Runtime.SpanWriter(buffer);
            var success = Integration.Test.V0.OrderBookData.TryEncodeBids(ref writer, bids);

            // Assert
            Assert.True(success);
            Assert.Equal(Integration.Test.V0.GroupSizeEncoding.MESSAGE_SIZE + 3 * Integration.Test.V0.OrderBookData.BidsData.MESSAGE_SIZE, writer.BytesWritten);

            // Verify the encoded data can be read back
            var reader = new Integration.Test.V0.Runtime.SpanReader(buffer);
            Assert.True(reader.TryRead<Integration.Test.V0.GroupSizeEncoding>(out var header));
            Assert.Equal((ushort)Integration.Test.V0.OrderBookData.BidsData.MESSAGE_SIZE, header.BlockLength);
            Assert.Equal(3u, header.NumInGroup);

            for (int i = 0; i < 3; i++)
            {
                Assert.True(reader.TryRead<Integration.Test.V0.OrderBookData.BidsData>(out var bid));
                Assert.Equal(bids[i].Price.Value, bid.Price.Value);
                Assert.Equal(bids[i].Quantity, bid.Quantity);
            }
        }

        [Fact]
        public void GroupEncoding_EmptyGroup_EncodesCorrectly()
        {
            // Arrange - Empty group
            Span<byte> buffer = stackalloc byte[512];
            var bids = Array.Empty<Integration.Test.V0.OrderBookData.BidsData>();

            // Act
            var writer = new Integration.Test.V0.Runtime.SpanWriter(buffer);
            var success = Integration.Test.V0.OrderBookData.TryEncodeBids(ref writer, bids);

            // Assert
            Assert.True(success);
            Assert.Equal(Integration.Test.V0.GroupSizeEncoding.MESSAGE_SIZE, writer.BytesWritten);

            // Verify header
            var reader = new Integration.Test.V0.Runtime.SpanReader(buffer);
            Assert.True(reader.TryRead<Integration.Test.V0.GroupSizeEncoding>(out var header));
            Assert.Equal(0u, header.NumInGroup);
        }

        [Fact]
        public void GroupEncoding_TwoGroups_EncodesCorrectly()
        {
            // Arrange - Create buffer and data for both bids and asks
            Span<byte> buffer = stackalloc byte[512];
            var bids = new Integration.Test.V0.OrderBookData.BidsData[]
            {
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1000, Quantity = 100 },
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1010, Quantity = 101 }
            };
            var asks = new Integration.Test.V0.OrderBookData.AsksData[]
            {
                new Integration.Test.V0.OrderBookData.AsksData { Price = 2000, Quantity = 200 }
            };

            // Act - Encode both groups
            var writer = new Integration.Test.V0.Runtime.SpanWriter(buffer);
            Assert.True(Integration.Test.V0.OrderBookData.TryEncodeBids(ref writer, bids));
            Assert.True(Integration.Test.V0.OrderBookData.TryEncodeAsks(ref writer, asks));

            // Assert - Verify both groups are encoded correctly
            var reader = new Integration.Test.V0.Runtime.SpanReader(buffer);
            
            // Verify bids
            Assert.True(reader.TryRead<Integration.Test.V0.GroupSizeEncoding>(out var bidsHeader));
            Assert.Equal(2u, bidsHeader.NumInGroup);
            for (int i = 0; i < 2; i++)
            {
                Assert.True(reader.TryRead<Integration.Test.V0.OrderBookData.BidsData>(out var bid));
                Assert.Equal(bids[i].Price.Value, bid.Price.Value);
            }

            // Verify asks
            Assert.True(reader.TryRead<Integration.Test.V0.GroupSizeEncoding>(out var asksHeader));
            Assert.Equal(1u, asksHeader.NumInGroup);
            Assert.True(reader.TryRead<Integration.Test.V0.OrderBookData.AsksData>(out var ask));
            Assert.Equal(2000, ask.Price.Value);
        }

        [Fact]
        public void GroupEncoding_InsufficientBuffer_ReturnsFalse()
        {
            // Arrange - Buffer too small
            Span<byte> buffer = stackalloc byte[10]; // Too small for group header
            var bids = new Integration.Test.V0.OrderBookData.BidsData[]
            {
                new Integration.Test.V0.OrderBookData.BidsData { Price = 1000, Quantity = 100 }
            };

            // Act
            var writer = new Integration.Test.V0.Runtime.SpanWriter(buffer);
            var success = Integration.Test.V0.OrderBookData.TryEncodeBids(ref writer, bids);

            // Assert
            Assert.False(success);
        }

        [Fact]
        public void VarDataEncoding_TryEncodeSymbol_EncodesCorrectly()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[512];
            string symbol = "AAPL";
            var symbolBytes = Encoding.UTF8.GetBytes(symbol);

            // Act - Encode varData
            var writer = new Integration.Test.V0.Runtime.SpanWriter(buffer);
            var success = Integration.Test.V0.NewOrderData.TryEncodeSymbol(ref writer, symbolBytes);

            // Assert
            Assert.True(success);
            Assert.Equal(1 + symbolBytes.Length, writer.BytesWritten); // 1 byte for length + data

            // Verify the encoded data can be read back
            Assert.Equal((byte)symbolBytes.Length, buffer[0]); // Length prefix
            var decodedBytes = buffer.Slice(1, symbolBytes.Length);
            var decodedSymbol = Encoding.UTF8.GetString(decodedBytes);
            Assert.Equal(symbol, decodedSymbol);
        }

        [Fact]
        public void VarDataEncoding_EmptyData_EncodesCorrectly()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[512];
            var emptyData = Array.Empty<byte>();

            // Act
            var writer = new Integration.Test.V0.Runtime.SpanWriter(buffer);
            var success = Integration.Test.V0.NewOrderData.TryEncodeSymbol(ref writer, emptyData);

            // Assert
            Assert.True(success);
            Assert.Equal(1, writer.BytesWritten); // Just the length byte (0)
            Assert.Equal(0, buffer[0]);
        }

        [Fact]
        public void VarDataEncoding_TooLong_ReturnsFalse()
        {
            // Arrange - Data longer than 255 bytes (max for uint8 length)
            Span<byte> buffer = stackalloc byte[512];
            var tooLongData = new byte[256]; // 256 > 255

            // Act
            var writer = new Integration.Test.V0.Runtime.SpanWriter(buffer);
            var success = Integration.Test.V0.NewOrderData.TryEncodeSymbol(ref writer, tooLongData);

            // Assert
            Assert.False(success); // Should fail because length > 255
        }

        [Fact]
        public void VarDataEncoding_InsufficientBuffer_ReturnsFalse()
        {
            // Arrange - Buffer too small
            Span<byte> buffer = stackalloc byte[2]; // Only room for length + 1 byte
            var data = new byte[5]; // Need 6 bytes total

            // Act
            var writer = new Integration.Test.V0.Runtime.SpanWriter(buffer);
            var success = Integration.Test.V0.NewOrderData.TryEncodeSymbol(ref writer, data);

            // Assert
            Assert.False(success);
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

            // Act - Encode message with groups using BeginEncoding
            Assert.True(orderBook.BeginEncoding(buffer, out var writer));
            Assert.True(Integration.Test.V0.OrderBookData.TryEncodeBids(ref writer, bids));
            Assert.True(Integration.Test.V0.OrderBookData.TryEncodeAsks(ref writer, asks));

            int totalBytesWritten = writer.BytesWritten;

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

            // Act - Encode message with varData using BeginEncoding
            Assert.True(order.BeginEncoding(buffer, out var writer));
            Assert.True(Integration.Test.V0.NewOrderData.TryEncodeSymbol(ref writer, symbolBytes));

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

        [Fact]
        public void BeginEncoding_InsufficientBuffer_ReturnsFalse()
        {
            // Arrange - Buffer too small for message header
            Span<byte> buffer = stackalloc byte[2];
            var orderBook = new Integration.Test.V0.OrderBookData { InstrumentId = 42 };

            // Act
            var success = orderBook.BeginEncoding(buffer, out var writer);

            // Assert
            Assert.False(success);
        }
    }
}
