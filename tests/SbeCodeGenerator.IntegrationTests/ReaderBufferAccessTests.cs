using System;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Issue #151: <c>{Msg}DataReader</c> exposes the underlying buffer/block as
    /// <see cref="ReadOnlySpan{T}"/> properties, so consumers can forward or
    /// buffer the raw bytes (e.g. snapshot-heal flows) without re-parsing.
    /// </summary>
    public class ReaderBufferAccessTests
    {
        [Fact]
        public void Reader_ExposesBlockAndBuffer_ForFixedLayoutMessage()
        {
            Span<byte> buffer = stackalloc byte[64];
            var trade = new Edge.Cases.Test.V0.TradeData
            {
                TradeId = 42,
                Quantity = 100,
            };
            Assert.True(trade.TryEncode(buffer, out int bytesWritten));

            Assert.True(Edge.Cases.Test.V0.TradeData.TryParse(buffer, out var reader));

            Assert.Equal(Edge.Cases.Test.V0.TradeData.MESSAGE_SIZE, reader.BlockLength);
            Assert.Equal(reader.BlockLength, reader.Block.Length);

            // Block is the slice the consumer would memcpy for replay.
            Assert.True(reader.Block.SequenceEqual(buffer.Slice(0, bytesWritten)));

            // Buffer covers the full source span (may extend past the message).
            Assert.True(reader.Buffer.Length >= reader.BlockLength);
            Assert.True(reader.Buffer.Slice(0, reader.BlockLength).SequenceEqual(reader.Block));
        }

        [Fact]
        public void Reader_BufferAndBlock_ReflectVariableDataMessage()
        {
            Span<byte> buffer = stackalloc byte[512];
            var orderBook = new Integration.Test.V0.OrderBookData { InstrumentId = 7 };
            var bids = new Integration.Test.V0.OrderBookData.BidsData[]
            {
                new() { Price = 100, Quantity = 5 },
            };
            var asks = Array.Empty<Integration.Test.V0.OrderBookData.AsksData>();

            Assert.True(Integration.Test.V0.OrderBookData.TryEncode(orderBook, buffer, bids, asks, out int bytesWritten));
            Assert.True(Integration.Test.V0.OrderBookData.TryParse(buffer, out var reader));

            // Block length matches the fixed message size, regardless of trailing groups.
            Assert.Equal(Integration.Test.V0.OrderBookData.MESSAGE_SIZE, reader.BlockLength);

            // Drive ReadGroups so BytesConsumed reflects the wire size, then ensure
            // the consumer can slice the original buffer to the full message footprint.
            reader.ReadGroups((in Integration.Test.V0.OrderBookData.BidsData _) => { },
                              (in Integration.Test.V0.OrderBookData.AsksData _) => { });
            var wireSlice = reader.Buffer.Slice(0, reader.BytesConsumed);
            Assert.Equal(bytesWritten, wireSlice.Length);
            Assert.True(wireSlice.SequenceEqual(buffer.Slice(0, bytesWritten)));
        }
    }
}
