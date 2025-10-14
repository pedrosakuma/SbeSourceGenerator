using System;
using System.Runtime.InteropServices;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
    // Delegate for custom parsing - matches the runtime version
    public delegate bool SpanParser<T>(ReadOnlySpan<byte> buffer, out T value, out int bytesConsumed);

    /// <summary>
    /// Integration tests demonstrating SpanReader usage for parsing SBE messages.
    /// These tests show the difference between manual offset management and SpanReader approach.
    /// </summary>
    public class SpanReaderIntegrationTests
    {
        // Local ref struct for testing (simulating what would be in SbeSourceGenerator.Runtime)
        private ref struct TestSpanReader
        {
            private ReadOnlySpan<byte> _buffer;

            public TestSpanReader(ReadOnlySpan<byte> buffer)
            {
                _buffer = buffer;
            }

            public readonly int RemainingBytes => _buffer.Length;

            public bool TryRead<T>(out T value) where T : struct
            {
                int size = Marshal.SizeOf<T>();
                if (_buffer.Length < size)
                {
                    value = default;
                    return false;
                }

                value = MemoryMarshal.Read<T>(_buffer);
                _buffer = _buffer.Slice(size);
                return true;
            }

            public bool TrySkip(int count)
            {
                if (_buffer.Length < count)
                    return false;

                _buffer = _buffer.Slice(count);
                return true;
            }

            public readonly bool TryPeek<T>(out T value) where T : struct
            {
                int size = Marshal.SizeOf<T>();
                if (_buffer.Length < size)
                {
                    value = default;
                    return false;
                }

                value = MemoryMarshal.Read<T>(_buffer);
                return true;
            }

            public bool TryReadWith<T>(SpanParser<T> parser, out T value)
            {
                if (parser(_buffer, out value, out int bytesConsumed))
                {
                    _buffer = _buffer.Slice(bytesConsumed);
                    return true;
                }
                
                value = default!;
                return false;
            }

            public bool TryAlignFrom(int alignment, int startOffset)
            {
                int padding = (alignment - (startOffset % alignment)) % alignment;
                
                if (padding > 0)
                {
                    return TrySkip(padding);
                }
                
                return true;
            }

            public bool TryReadBytes(int count, out ReadOnlySpan<byte> bytes)
            {
                if (_buffer.Length < count)
                {
                    bytes = default;
                    return false;
                }

                bytes = _buffer.Slice(0, count);
                _buffer = _buffer.Slice(count);
                return true;
            }
        }
        // Simulated SBE structures
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct GroupSizeEncoding
        {
            public ushort BlockLength;
            public uint NumInGroup;
            
            public const int MESSAGE_SIZE = 6;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BidEntry
        {
            public long Price;
            public long Quantity;
            
            public const int MESSAGE_SIZE = 16;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AskEntry
        {
            public long Price;
            public long Quantity;
            
            public const int MESSAGE_SIZE = 16;
        }

        [Fact]
        public void ParseOrderBook_WithManualOffset_Works()
        {
            // Arrange - Create a buffer with sample order book data
            var buffer = CreateSampleOrderBookBuffer(numBids: 3, numAsks: 2);
            
            int offset = 0;
            int totalBids = 0;
            long totalBidQty = 0;
            int totalAsks = 0;
            long totalAskQty = 0;
            
            // Act - Parse using manual offset management (current approach)
            
            // Read bids group header
            ref readonly GroupSizeEncoding bidsHeader = ref MemoryMarshal.AsRef<GroupSizeEncoding>(buffer.Slice(offset));
            offset += GroupSizeEncoding.MESSAGE_SIZE;
            
            // Read bid entries
            for (int i = 0; i < bidsHeader.NumInGroup; i++)
            {
                ref readonly var bid = ref MemoryMarshal.AsRef<BidEntry>(buffer.Slice(offset));
                totalBids++;
                totalBidQty += bid.Quantity;
                offset += BidEntry.MESSAGE_SIZE;
            }
            
            // Read asks group header
            ref readonly GroupSizeEncoding asksHeader = ref MemoryMarshal.AsRef<GroupSizeEncoding>(buffer.Slice(offset));
            offset += GroupSizeEncoding.MESSAGE_SIZE;
            
            // Read ask entries
            for (int i = 0; i < asksHeader.NumInGroup; i++)
            {
                ref readonly var ask = ref MemoryMarshal.AsRef<AskEntry>(buffer.Slice(offset));
                totalAsks++;
                totalAskQty += ask.Quantity;
                offset += AskEntry.MESSAGE_SIZE;
            }
            
            // Assert
            Assert.Equal(3, totalBids);
            Assert.Equal(2, totalAsks);
            Assert.Equal(300, totalBidQty); // 100 + 100 + 100
            Assert.Equal(150, totalAskQty); // 75 + 75
        }

        [Fact]
        public void ParseOrderBook_WithSpanReader_Works()
        {
            // Arrange - Create a buffer with sample order book data
            var buffer = CreateSampleOrderBookBuffer(numBids: 3, numAsks: 2);
            
            var reader = new TestSpanReader(buffer);
            int totalBids = 0;
            long totalBidQty = 0;
            int totalAsks = 0;
            long totalAskQty = 0;
            
            // Act - Parse using SpanReader (proposed approach)
            
            // Read bids group
            if (reader.TryRead<GroupSizeEncoding>(out var bidsHeader))
            {
                for (int i = 0; i < bidsHeader.NumInGroup; i++)
                {
                    if (reader.TryRead<BidEntry>(out var bid))
                    {
                        totalBids++;
                        totalBidQty += bid.Quantity;
                    }
                }
            }
            
            // Read asks group
            if (reader.TryRead<GroupSizeEncoding>(out var asksHeader))
            {
                for (int i = 0; i < asksHeader.NumInGroup; i++)
                {
                    if (reader.TryRead<AskEntry>(out var ask))
                    {
                        totalAsks++;
                        totalAskQty += ask.Quantity;
                    }
                }
            }
            
            // Assert
            Assert.Equal(3, totalBids);
            Assert.Equal(2, totalAsks);
            Assert.Equal(300, totalBidQty);
            Assert.Equal(150, totalAskQty);
        }

        [Fact]
        public void SpanReader_HandlesIncompleteData_Gracefully()
        {
            // Arrange - Create incomplete buffer (truncated)
            var fullBuffer = CreateSampleOrderBookBuffer(numBids: 3, numAsks: 2);
            var incompleteBuffer = fullBuffer.Slice(0, 20); // Only partial data
            
            var reader = new TestSpanReader(incompleteBuffer);
            int entriesRead = 0;
            
            // Act
            if (reader.TryRead<GroupSizeEncoding>(out var header))
            {
                for (int i = 0; i < header.NumInGroup; i++)
                {
                    if (reader.TryRead<BidEntry>(out var entry))
                        entriesRead++;
                }
            }
            
            // Assert - Should stop gracefully when buffer runs out
            Assert.True(entriesRead < 3); // Couldn't read all 3 entries
        }

        [Fact]
        public void SpanReader_SupportsSkippingUnknownFields()
        {
            // Arrange - Simulate schema evolution where new fields were added
            var buffer = CreateSampleOrderBookBuffer(numBids: 2, numAsks: 1);
            var reader = new TestSpanReader(buffer);
            
            // Act - Read header, skip first group, read second group
            if (reader.TryRead<GroupSizeEncoding>(out var bidsHeader))
            {
                // Skip all bid entries (e.g., we only care about asks)
                int skipBytes = (int)bidsHeader.NumInGroup * BidEntry.MESSAGE_SIZE;
                reader.TrySkip(skipBytes);
            }
            
            long totalAskQty = 0;
            if (reader.TryRead<GroupSizeEncoding>(out var asksHeader))
            {
                for (int i = 0; i < asksHeader.NumInGroup; i++)
                {
                    if (reader.TryRead<AskEntry>(out var ask))
                        totalAskQty += ask.Quantity;
                }
            }
            
            // Assert
            Assert.Equal(75, totalAskQty); // Only 1 ask with quantity 75
        }

        [Fact]
        public void SpanReader_SupportsPeeking()
        {
            // Arrange
            var buffer = CreateSampleOrderBookBuffer(numBids: 1, numAsks: 0);
            var reader = new TestSpanReader(buffer);
            
            // Act - Peek at header without consuming it
            bool peeked = reader.TryPeek<GroupSizeEncoding>(out var peekedHeader);
            int bytesBeforePeek = reader.RemainingBytes;
            
            // Then actually read it
            bool read = reader.TryRead<GroupSizeEncoding>(out var readHeader);
            int bytesAfterRead = reader.RemainingBytes;
            
            // Assert
            Assert.True(peeked);
            Assert.True(read);
            Assert.Equal(peekedHeader.NumInGroup, readHeader.NumInGroup);
            Assert.Equal(bytesBeforePeek, bytesAfterRead + GroupSizeEncoding.MESSAGE_SIZE);
        }

        [Fact]
        public void SpanReader_WorksWithCallbackPattern()
        {
            // Arrange - Demonstrate callback pattern similar to ConsumeVariableLengthSegments
            var buffer = CreateSampleOrderBookBuffer(numBids: 2, numAsks: 2);
            var bidPrices = new System.Collections.Generic.List<long>();
            var askPrices = new System.Collections.Generic.List<long>();
            
            // Act - Use callbacks to process entries
            ProcessOrderBookWithCallbacks(buffer, 
                bid => bidPrices.Add(bid.Price),
                ask => askPrices.Add(ask.Price));
            
            // Assert
            Assert.Equal(2, bidPrices.Count);
            Assert.Equal(2, askPrices.Count);
            Assert.Contains(10000, bidPrices);
            Assert.Contains(10001, bidPrices);
            Assert.Contains(11000, askPrices);
            Assert.Contains(11001, askPrices);
        }

        // Helper method demonstrating SpanReader with callbacks
        private void ProcessOrderBookWithCallbacks(
            ReadOnlySpan<byte> buffer,
            Action<BidEntry> onBid,
            Action<AskEntry> onAsk)
        {
            var reader = new TestSpanReader(buffer);
            
            // Process bids
            if (reader.TryRead<GroupSizeEncoding>(out var bidsHeader))
            {
                for (int i = 0; i < bidsHeader.NumInGroup; i++)
                {
                    if (reader.TryRead<BidEntry>(out var bid))
                        onBid(bid);
                }
            }
            
            // Process asks
            if (reader.TryRead<GroupSizeEncoding>(out var asksHeader))
            {
                for (int i = 0; i < asksHeader.NumInGroup; i++)
                {
                    if (reader.TryRead<AskEntry>(out var ask))
                        onAsk(ask);
                }
            }
        }

        // Helper to create sample order book buffer
        private ReadOnlySpan<byte> CreateSampleOrderBookBuffer(int numBids, int numAsks)
        {
            int bufferSize = 
                GroupSizeEncoding.MESSAGE_SIZE + (numBids * BidEntry.MESSAGE_SIZE) +
                GroupSizeEncoding.MESSAGE_SIZE + (numAsks * AskEntry.MESSAGE_SIZE);
            
            var buffer = new byte[bufferSize];
            int offset = 0;
            
            // Write bids group header
            ref var bidsHeader = ref MemoryMarshal.AsRef<GroupSizeEncoding>(buffer.AsSpan(offset));
            bidsHeader.BlockLength = (ushort)BidEntry.MESSAGE_SIZE;
            bidsHeader.NumInGroup = (uint)numBids;
            offset += GroupSizeEncoding.MESSAGE_SIZE;
            
            // Write bid entries
            for (int i = 0; i < numBids; i++)
            {
                ref var bid = ref MemoryMarshal.AsRef<BidEntry>(buffer.AsSpan(offset));
                bid.Price = 10000 + i;
                bid.Quantity = 100;
                offset += BidEntry.MESSAGE_SIZE;
            }
            
            // Write asks group header
            ref var asksHeader = ref MemoryMarshal.AsRef<GroupSizeEncoding>(buffer.AsSpan(offset));
            asksHeader.BlockLength = (ushort)AskEntry.MESSAGE_SIZE;
            asksHeader.NumInGroup = (uint)numAsks;
            offset += GroupSizeEncoding.MESSAGE_SIZE;
            
            // Write ask entries
            for (int i = 0; i < numAsks; i++)
            {
                ref var ask = ref MemoryMarshal.AsRef<AskEntry>(buffer.AsSpan(offset));
                ask.Price = 11000 + i;
                ask.Quantity = 75;
                offset += AskEntry.MESSAGE_SIZE;
            }
            
            return buffer;
        }
    }

    /// <summary>
    /// Integration tests for SpanReader extensibility features.
    /// Demonstrates schema evolution, custom parsing, and alignment in real-world scenarios.
    /// </summary>
    public class SpanReaderExtensibilityIntegrationTests
    {
        // Local ref struct matching SpanReader (for integration testing without runtime reference)
        private ref struct TestSpanReader
        {
            private ReadOnlySpan<byte> _buffer;

            public TestSpanReader(ReadOnlySpan<byte> buffer)
            {
                _buffer = buffer;
            }

            public readonly int RemainingBytes => _buffer.Length;

            public bool TryRead<T>(out T value) where T : struct
            {
                int size = Marshal.SizeOf<T>();
                if (_buffer.Length < size)
                {
                    value = default;
                    return false;
                }

                value = MemoryMarshal.Read<T>(_buffer);
                _buffer = _buffer.Slice(size);
                return true;
            }

            public bool TrySkip(int count)
            {
                if (_buffer.Length < count)
                    return false;

                _buffer = _buffer.Slice(count);
                return true;
            }

            public bool TryReadWith<T>(SpanParser<T> parser, out T value)
            {
                if (parser(_buffer, out value, out int bytesConsumed))
                {
                    _buffer = _buffer.Slice(bytesConsumed);
                    return true;
                }
                
                value = default!;
                return false;
            }

            public bool TryAlignFrom(int alignment, int startOffset)
            {
                int padding = (alignment - (startOffset % alignment)) % alignment;
                
                if (padding > 0)
                {
                    return TrySkip(padding);
                }
                
                return true;
            }

            public bool TryReadBytes(int count, out ReadOnlySpan<byte> bytes)
            {
                if (_buffer.Length < count)
                {
                    bytes = default;
                    return false;
                }

                bytes = _buffer.Slice(0, count);
                _buffer = _buffer.Slice(count);
                return true;
            }
        }

        // Simulated versioned message structures
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MessageHeaderV1
        {
            public ushort Version;
            public ushort TemplateId;
            public uint MessageLength;
            
            public const int MESSAGE_SIZE = 8;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct OrderV1
        {
            public long OrderId;
            public int Quantity;
            
            public const int MESSAGE_SIZE = 12;
        }

        private struct OrderV2  // Non-blittable for testing custom parser
        {
            public long OrderId;
            public int Quantity;
            public long Price;  // Added in V2
            public ushort Version;
        }

        [Fact]
        public void ParseVersionedMessage_WithCustomParser_HandlesSchemaEvolution()
        {
            // Arrange - Create V2 message buffer
            var buffer = new byte[100];
            int offset = 0;

            // Write header
            ref var header = ref MemoryMarshal.AsRef<MessageHeaderV1>(buffer.AsSpan(offset));
            header.Version = 2;
            header.TemplateId = 100;
            header.MessageLength = 22;  // V2 order size
            offset += MessageHeaderV1.MESSAGE_SIZE;

            // Write V2 order data
            MemoryMarshal.Write(buffer.AsSpan(offset), (long)123456);     // OrderId
            MemoryMarshal.Write(buffer.AsSpan(offset + 8), (int)100);     // Quantity
            MemoryMarshal.Write(buffer.AsSpan(offset + 12), (long)5000);  // Price (V2 field)

            var reader = new TestSpanReader(buffer);

            // Custom parser for versioned orders
            static bool ParseOrder(ReadOnlySpan<byte> buf, out OrderV2 order, out int consumed)
            {
                order = default;
                consumed = 0;

                // Read header first (already consumed by main logic)
                // In real scenario, version would be passed from header
                
                // For this test, assume V2 format
                if (buf.Length < 20)
                    return false;

                order = new OrderV2
                {
                    OrderId = MemoryMarshal.Read<long>(buf),
                    Quantity = MemoryMarshal.Read<int>(buf.Slice(8)),
                    Price = MemoryMarshal.Read<long>(buf.Slice(12)),
                    Version = 2
                };
                consumed = 20;
                return true;
            }

            // Act
            Assert.True(reader.TryRead<MessageHeaderV1>(out var parsedHeader));
            Assert.True(reader.TryReadWith<OrderV2>(ParseOrder, out var order));

            // Assert
            Assert.Equal(2, parsedHeader.Version);
            Assert.Equal(100, parsedHeader.TemplateId);
            Assert.Equal(123456, order.OrderId);
            Assert.Equal(100, order.Quantity);
            Assert.Equal(5000, order.Price);
            Assert.Equal(2, order.Version);
        }

        [Fact]
        public void ParseAlignedProtocol_WithAlignment_ReadsCorrectly()
        {
            // Arrange - Create buffer with alignment padding
            var buffer = new byte[100];
            int offset = 0;

            // Write unaligned header (5 bytes)
            buffer[offset++] = 1;  // Version
            buffer[offset++] = 2;  // Type
            buffer[offset++] = 3;  // Flags
            MemoryMarshal.Write(buffer.AsSpan(offset), (ushort)100);  // Length
            offset += 2;  // Total: 5 bytes

            // Add padding to align to 8-byte boundary (3 bytes of padding)
            offset += 3;

            // Write aligned data (8 bytes) at offset 8
            MemoryMarshal.Write(buffer.AsSpan(offset), (long)999888777);

            var reader = new TestSpanReader(buffer);

            // Act
            Assert.True(reader.TryReadBytes(5, out var headerBytes));  // Read 5-byte header
            int consumed = 5;
            
            // Align to 8-byte boundary
            Assert.True(reader.TryAlignFrom(8, consumed));
            consumed += 3;  // padding

            // Read aligned value
            Assert.True(reader.TryRead<long>(out var alignedValue));

            // Assert
            Assert.Equal(1, headerBytes[0]);
            Assert.Equal(2, headerBytes[1]);
            Assert.Equal(3, headerBytes[2]);
            Assert.Equal(999888777, alignedValue);
        }

        [Fact]
        public void ParseMixedContent_UsingMultipleExtensibilityFeatures_Works()
        {
            // Arrange - Create complex buffer with:
            // 1. Standard message header
            // 2. Versioned content (custom parser)
            // 3. Aligned section
            // 4. Repeating group
            
            var buffer = new byte[200];
            int offset = 0;

            // 1. Standard header
            ref var header = ref MemoryMarshal.AsRef<MessageHeaderV1>(buffer.AsSpan(offset));
            header.Version = 2;
            header.TemplateId = 50;
            header.MessageLength = 100;
            offset += MessageHeaderV1.MESSAGE_SIZE;

            // 2. Custom versioned section (write V2 order)
            MemoryMarshal.Write(buffer.AsSpan(offset), (long)111);    // OrderId
            MemoryMarshal.Write(buffer.AsSpan(offset + 8), (int)50);  // Quantity
            MemoryMarshal.Write(buffer.AsSpan(offset + 12), (long)2500); // Price
            offset += 20;

            // 3. Alignment padding (offset=28, align to 32)
            offset = 32;

            // 4. Group header
            MemoryMarshal.Write(buffer.AsSpan(offset), (ushort)8);   // BlockLength
            MemoryMarshal.Write(buffer.AsSpan(offset + 2), (uint)2); // NumInGroup
            offset += 6;

            // Group entries
            MemoryMarshal.Write(buffer.AsSpan(offset), (long)100);
            offset += 8;
            MemoryMarshal.Write(buffer.AsSpan(offset), (long)200);

            // Custom parser for V2 order
            static bool ParseOrderV2(ReadOnlySpan<byte> buf, out OrderV2 order, out int consumed)
            {
                if (buf.Length < 20)
                {
                    order = default;
                    consumed = 0;
                    return false;
                }

                order = new OrderV2
                {
                    OrderId = MemoryMarshal.Read<long>(buf),
                    Quantity = MemoryMarshal.Read<int>(buf.Slice(8)),
                    Price = MemoryMarshal.Read<long>(buf.Slice(12)),
                    Version = 2
                };
                consumed = 20;
                return true;
            }

            var reader = new TestSpanReader(buffer);
            int totalConsumed = 0;

            // Act & Assert
            
            // 1. Read standard header
            Assert.True(reader.TryRead<MessageHeaderV1>(out var parsedHeader));
            Assert.Equal(2, parsedHeader.Version);
            totalConsumed += MessageHeaderV1.MESSAGE_SIZE;

            // 2. Read versioned content with custom parser
            Assert.True(reader.TryReadWith<OrderV2>(ParseOrderV2, out var order));
            Assert.Equal(111, order.OrderId);
            Assert.Equal(50, order.Quantity);
            Assert.Equal(2500, order.Price);
            totalConsumed += 20;

            // 3. Align to 32-byte boundary
            int padding = (32 - totalConsumed) % 32;
            Assert.True(reader.TrySkip(padding));
            totalConsumed = 32;

            // 4. Read group
            Assert.True(reader.TryRead<ushort>(out var blockLength));
            Assert.True(reader.TryRead<uint>(out var numInGroup));
            Assert.Equal(8, blockLength);
            Assert.Equal(2u, numInGroup);

            Assert.True(reader.TryRead<long>(out var entry1));
            Assert.True(reader.TryRead<long>(out var entry2));
            Assert.Equal(100, entry1);
            Assert.Equal(200, entry2);
        }

        [Fact]
        public void RealWorldScenario_MarketDataFeed_ParsesEfficiently()
        {
            // Simulate a real market data feed message with:
            // - Header with version info
            // - Variable number of price levels
            // - Custom parsing for non-standard fields

            var buffer = new byte[500];
            int offset = 0;

            // Header
            ref var header = ref MemoryMarshal.AsRef<MessageHeaderV1>(buffer.AsSpan(offset));
            header.Version = 1;
            header.TemplateId = 1;  // Market data snapshot
            header.MessageLength = 200;
            offset += MessageHeaderV1.MESSAGE_SIZE;

            // Symbol (8 bytes, null-terminated)
            System.Text.Encoding.ASCII.GetBytes("AAPL").CopyTo(buffer.AsSpan(offset));
            offset += 8;

            // Number of bid levels
            int numBids = 3;
            MemoryMarshal.Write(buffer.AsSpan(offset), numBids);
            offset += 4;

            // Bid levels (price + qty)
            for (int i = 0; i < numBids; i++)
            {
                MemoryMarshal.Write(buffer.AsSpan(offset), (long)(15000 + i * 10));  // Price
                MemoryMarshal.Write(buffer.AsSpan(offset + 8), (int)(100 * (i + 1))); // Qty
                offset += 12;
            }

            // Number of ask levels
            int numAsks = 2;
            MemoryMarshal.Write(buffer.AsSpan(offset), numAsks);
            offset += 4;

            // Ask levels
            for (int i = 0; i < numAsks; i++)
            {
                MemoryMarshal.Write(buffer.AsSpan(offset), (long)(15100 + i * 10));
                MemoryMarshal.Write(buffer.AsSpan(offset + 8), (int)(50 * (i + 1)));
                offset += 12;
            }

            var reader = new TestSpanReader(buffer);

            // Parse
            Assert.True(reader.TryRead<MessageHeaderV1>(out var msgHeader));
            Assert.Equal(1, msgHeader.TemplateId);

            Assert.True(reader.TryReadBytes(8, out var symbolBytes));
            string symbol = System.Text.Encoding.ASCII.GetString(symbolBytes).TrimEnd('\0');
            Assert.Equal("AAPL", symbol);

            // Read bids
            Assert.True(reader.TryRead<int>(out var bidCount));
            Assert.Equal(3, bidCount);

            long[] bidPrices = new long[bidCount];
            int[] bidQtys = new int[bidCount];
            for (int i = 0; i < bidCount; i++)
            {
                Assert.True(reader.TryRead<long>(out bidPrices[i]));
                Assert.True(reader.TryRead<int>(out bidQtys[i]));
            }

            Assert.Equal(15000, bidPrices[0]);
            Assert.Equal(100, bidQtys[0]);

            // Read asks
            Assert.True(reader.TryRead<int>(out var askCount));
            Assert.Equal(2, askCount);

            long[] askPrices = new long[askCount];
            int[] askQtys = new int[askCount];
            for (int i = 0; i < askCount; i++)
            {
                Assert.True(reader.TryRead<long>(out askPrices[i]));
                Assert.True(reader.TryRead<int>(out askQtys[i]));
            }

            Assert.Equal(15100, askPrices[0]);
            Assert.Equal(50, askQtys[0]);
        }
    }
}
