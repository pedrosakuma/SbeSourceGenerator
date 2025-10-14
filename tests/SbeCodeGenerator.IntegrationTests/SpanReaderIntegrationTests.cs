using System;
using System.Runtime.InteropServices;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
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
}
