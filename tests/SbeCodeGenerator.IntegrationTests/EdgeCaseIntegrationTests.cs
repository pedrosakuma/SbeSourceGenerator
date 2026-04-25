using System;
using System.Runtime.InteropServices;
using System.Text;
using Edge.Cases.Test.V0;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
    public class EdgeCaseIntegrationTests
    {
        // --- Sparse Enum Tests ---

        [Fact]
        public void StatusCode_HasCorrectSparseValues()
        {
            Assert.Equal((byte)0, (byte)StatusCode.New);
            Assert.Equal((byte)3, (byte)StatusCode.PartiallyFilled);
            Assert.Equal((byte)5, (byte)StatusCode.Filled);
            Assert.Equal((byte)10, (byte)StatusCode.Cancelled);
            Assert.Equal((byte)99, (byte)StatusCode.Rejected);
        }

        [Fact]
        public void Venue_HasCorrectSparseValues()
        {
            Assert.Equal((byte)1, (byte)Venue.Nyse);
            Assert.Equal((byte)2, (byte)Venue.Nasdaq);
            Assert.Equal((byte)10, (byte)Venue.Bats);
            Assert.Equal((byte)50, (byte)Venue.Iex);
        }

        [Fact]
        public void StatusCode_RoundTrips_ThroughByte()
        {
            byte raw = 99;
            var status = (StatusCode)raw;
            Assert.Equal(StatusCode.Rejected, status);
        }

        // --- Large Set (uint16) Tests ---

        [Fact]
        public void TradingFlags_IsUInt16Based()
        {
            Assert.Equal(2, sizeof(TradingFlags));
        }

        [Fact]
        public void TradingFlags_HighBitChoices_Work()
        {
            var flags = TradingFlags.Negotiated | TradingFlags.OffExchange;
            Assert.True((flags & TradingFlags.Negotiated) != 0);
            Assert.True((flags & TradingFlags.OffExchange) != 0);
            Assert.True((flags & TradingFlags.Regular) == 0);

            // Verify high bit values (>8 bits)
            Assert.Equal((ushort)256, (ushort)TradingFlags.Negotiated);
            Assert.Equal((ushort)512, (ushort)TradingFlags.OffExchange);
        }

        [Fact]
        public void TradingFlags_AllChoices_Combine()
        {
            var all = TradingFlags.Regular | TradingFlags.OddLot | TradingFlags.DarkPool
                    | TradingFlags.BlockTrade | TradingFlags.CrossTrade | TradingFlags.PreMarket
                    | TradingFlags.AfterHours | TradingFlags.Auction | TradingFlags.Negotiated
                    | TradingFlags.OffExchange;
            Assert.Equal((ushort)0x3FF, (ushort)all); // 10 bits set = 1023
        }

        [Fact]
        public void TradingFlags_Extensions_IsXxx_Work()
        {
            // Issue #144: aggressively-inlined bit-test extension methods.
            var flags = TradingFlags.OddLot | TradingFlags.OffExchange;
            Assert.True(flags.IsOddLot());
            Assert.True(flags.IsOffExchange());
            Assert.False(flags.IsDarkPool());
            Assert.False(flags.IsAuction());
        }

        [Fact]
        public void TradingFlags_Extensions_Has_Composite()
        {
            // Has(flag) supports composite flags: every bit must be present.
            var multi = TradingFlags.OddLot | TradingFlags.BlockTrade;
            Assert.True(multi.Has(TradingFlags.OddLot));
            Assert.True(multi.Has(TradingFlags.BlockTrade));
            Assert.True(multi.Has(TradingFlags.OddLot | TradingFlags.BlockTrade));
            Assert.False(multi.Has(TradingFlags.OddLot | TradingFlags.DarkPool));
        }

        // --- Issue #145: Derived constants on decimal composites ---

        [Fact]
        public void DecimalEncoding_DerivedConstants_AreCorrect()
        {
            // Schema: <type name="exponent" primitiveType="int8" presence="constant">-7</type>
            Assert.Equal((sbyte)-7, DecimalEncoding.Exponent);
            Assert.Equal(7, DecimalEncoding.Decimals);
            Assert.Equal(1e-7, DecimalEncoding.Multiplier);
            Assert.Equal(1e-7m, DecimalEncoding.MultiplierDecimal);
            Assert.Equal(10_000_000L, DecimalEncoding.Divisor);
        }

        // --- Issue #147: SbeDispatcher zero-cost dispatch ---

        private struct CountingHandler : ISbeMessageHandler
        {
            public int TradeCount;
            public int TextCount;
            public int MarketCount;
            public int UnknownCount;
            public long LastTradeId;
            public int LastBlockLength;

            public void OnTrade(in TradeDataReader reader, int blockLength, int version)
            {
                TradeCount++;
                LastTradeId = reader.Data.TradeId;
                LastBlockLength = blockLength;
            }
            public void OnTextMessage(in TextMessageDataReader reader, int blockLength, int version)
            {
                TextCount++;
            }
            public void OnMarketData(in MarketDataDataReader reader, int blockLength, int version)
            {
                MarketCount++;
            }
            public void OnUnknownMessage(int templateId, int blockLength, int version, ReadOnlySpan<byte> payload)
            {
                UnknownCount++;
            }
        }

        [Fact]
        public void SbeDispatcher_RoutesByTemplateId()
        {
            // Encode a Trade message with header into a buffer.
            Span<byte> buffer = stackalloc byte[MessageHeader.MESSAGE_SIZE + TradeData.MESSAGE_SIZE];
            ref var header = ref MemoryMarshal.AsRef<MessageHeader>(buffer);
            header.BlockLength = (ushort)TradeData.BLOCK_LENGTH;
            header.TemplateId = (ushort)TradeData.MESSAGE_ID;
            header.SchemaId = 5;
            header.Version = 0;

            ref var trade = ref MemoryMarshal.AsRef<TradeData>(buffer.Slice(MessageHeader.MESSAGE_SIZE));
            trade.TradeId = 4242;
            trade.Quantity = 100;

            var handler = new CountingHandler();
            bool dispatched = SbeDispatcher.Dispatch(buffer, ref handler);

            Assert.True(dispatched);
            Assert.Equal(1, handler.TradeCount);
            Assert.Equal(0, handler.TextCount);
            Assert.Equal(0, handler.MarketCount);
            Assert.Equal(0, handler.UnknownCount);
            Assert.Equal(4242, handler.LastTradeId);
            Assert.Equal(TradeData.BLOCK_LENGTH, handler.LastBlockLength);
        }

        [Fact]
        public void SbeDispatcher_UnknownTemplateId_CallsOnUnknownMessage()
        {
            Span<byte> buffer = stackalloc byte[MessageHeader.MESSAGE_SIZE];
            ref var header = ref MemoryMarshal.AsRef<MessageHeader>(buffer);
            header.BlockLength = 0;
            header.TemplateId = 9999; // unknown
            header.SchemaId = 5;
            header.Version = 0;

            var handler = new CountingHandler();
            bool dispatched = SbeDispatcher.Dispatch(buffer, ref handler);

            Assert.False(dispatched);
            Assert.Equal(0, handler.TradeCount);
            Assert.Equal(1, handler.UnknownCount);
        }

        [Fact]
        public void SbeDispatcher_BufferTooSmall_ReturnsFalse()
        {
            Span<byte> tiny = stackalloc byte[3]; // smaller than header
            var handler = new CountingHandler();
            bool dispatched = SbeDispatcher.Dispatch(tiny, ref handler);

            Assert.False(dispatched);
            Assert.Equal(0, handler.UnknownCount); // header parse failed before reaching switch
        }

        // --- Composite with Ref Tests ---

        [Fact]
        public void Instrument_HasCorrectMessageSize()
        {
            // int64(8) + IsinCode InlineArray(12) = 20
            Assert.Equal(20, Instrument.MESSAGE_SIZE);
        }

        [Fact]
        public void Instrument_TryParse_WithIsinRef()
        {
            Span<byte> buffer = stackalloc byte[Instrument.MESSAGE_SIZE];
            ref var inst = ref MemoryMarshal.AsRef<Instrument>(buffer);
            inst.InstrumentId = 12345;

            // Write ISIN code into the Isin field
            IsinCode isin = default;
            Encoding.Latin1.GetBytes("US0378331005").CopyTo(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref isin, 1)).Slice(0, 12));
            inst.Isin = isin;

            Assert.True(Instrument.TryParse(buffer, out var parsed, out _));
            Assert.Equal(12345L, parsed.InstrumentId);
            Assert.Equal("US0378331005", parsed.Isin.ToString());
        }

        // --- DecimalEncoding (constant exponent) Tests ---

        [Fact]
        public void DecimalEncoding_HasConstantExponent()
        {
            Assert.Equal((sbyte)-7, DecimalEncoding.Exponent);
        }

        [Fact]
        public void DecimalEncoding_MessageSize_ExcludesConstant()
        {
            // Only mantissa (int64 = 8), exponent is constant
            Assert.Equal(8, DecimalEncoding.MESSAGE_SIZE);
        }

        [Fact]
        public void DecimalEncoding_RoundTrip()
        {
            Span<byte> buffer = stackalloc byte[DecimalEncoding.MESSAGE_SIZE];
            ref var dec = ref MemoryMarshal.AsRef<DecimalEncoding>(buffer);
            dec.Mantissa = 12345678900L;

            Assert.True(DecimalEncoding.TryParse(buffer, out var parsed, out _));
            Assert.Equal(12345678900L, parsed.Mantissa);
        }

        // --- Multiple Types Blocks ---

        [Fact]
        public void Venue_FromSecondTypesBlock_IsAccessible()
        {
            // Venue enum is defined in the second <types> block
            Assert.Equal(Venue.Iex, (Venue)50);
        }

        // --- Trade Message (composite refs, sparse enum, large set) ---

        [Fact]
        public unsafe void Trade_HasCorrectMessageSize()
        {
            // int64(8) + Instrument(20) + DecimalEncoding(8) + int64(8) + StatusCode(1) + TradingFlags(2) + Venue(1) = 48
            Assert.Equal(48, TradeData.MESSAGE_SIZE);
            Assert.Equal(TradeData.MESSAGE_SIZE, sizeof(TradeData));
        }

        [Fact]
        public void Trade_FixedFieldsRoundTrip()
        {
            Span<byte> buffer = stackalloc byte[TradeData.MESSAGE_SIZE];
            ref var trade = ref MemoryMarshal.AsRef<TradeData>(buffer);

            trade.TradeId = 999L;
            trade.Instrument = new Instrument { InstrumentId = 42L };
            trade.Price = new DecimalEncoding { Mantissa = 15000000L };
            trade.Quantity = 100L;
            trade.Status = StatusCode.Filled;
            trade.Flags = TradingFlags.Regular | TradingFlags.DarkPool;
            trade.Venue = Venue.Nasdaq;

            Assert.True(TradeData.TryParse(buffer, out var parsed));
            Assert.Equal(999L, parsed.Data.TradeId);
            Assert.Equal(42L, parsed.Data.Instrument.InstrumentId);
            Assert.Equal(15000000L, parsed.Data.Price.Mantissa);
            Assert.Equal(100L, parsed.Data.Quantity);
            Assert.Equal(StatusCode.Filled, parsed.Data.Status);
            Assert.True((parsed.Data.Flags & TradingFlags.DarkPool) != 0);
            Assert.Equal(Venue.Nasdaq, parsed.Data.Venue);
        }

        [Fact]
        public void Trade_TryEncode_Works()
        {
            TradeData trade = default;
            trade.TradeId = 1L;
            trade.Quantity = 50L;
            trade.Status = StatusCode.New;
            trade.Venue = Venue.Nyse;

            Span<byte> buffer = stackalloc byte[TradeData.MESSAGE_SIZE];
            Assert.True(trade.TryEncode(buffer, out int bytesWritten));
            Assert.Equal(TradeData.MESSAGE_SIZE, bytesWritten);
        }

        [Fact]
        public void Trade_ToString_IncludesFields()
        {
            TradeData trade = default;
            trade.TradeId = 42L;
            trade.Status = StatusCode.Cancelled;
            var str = trade.ToString();
            Assert.Contains("TradeId", str);
            Assert.Contains("42", str);
        }

        [Fact]
        public void Trade_WriteHeader_PopulatesCorrectly()
        {
            Span<byte> buffer = stackalloc byte[MessageHeader.MESSAGE_SIZE + TradeData.MESSAGE_SIZE];
            var headerSize = TradeData.WriteHeader(buffer);
            ref var header = ref MemoryMarshal.AsRef<MessageHeader>(buffer);

            Assert.Equal(MessageHeader.MESSAGE_SIZE, headerSize);
            Assert.Equal((ushort)TradeData.MESSAGE_SIZE, header.BlockLength);
            Assert.Equal((ushort)TradeData.MESSAGE_ID, header.TemplateId);
        }

        // --- MarketData Message (large set + sparse enum together) ---

        [Fact]
        public void MarketData_RoundTrip()
        {
            Span<byte> buffer = stackalloc byte[MarketDataData.MESSAGE_SIZE];
            ref var md = ref MemoryMarshal.AsRef<MarketDataData>(buffer);

            md.SymbolId = 12345;
            md.Flags = TradingFlags.PreMarket | TradingFlags.Auction | TradingFlags.OffExchange;
            md.Status = StatusCode.PartiallyFilled;
            md.Venue = Venue.Bats;

            Assert.True(MarketDataData.TryParse(buffer, out var parsed));
            Assert.Equal(12345, parsed.Data.SymbolId);
            Assert.True((parsed.Data.Flags & TradingFlags.PreMarket) != 0);
            Assert.True((parsed.Data.Flags & TradingFlags.OffExchange) != 0);
            Assert.True((parsed.Data.Flags & TradingFlags.Regular) == 0);
            Assert.Equal(StatusCode.PartiallyFilled, parsed.Data.Status);
            Assert.Equal(Venue.Bats, parsed.Data.Venue);
        }

        // --- TextMessage (group with only data, no fixed fields) ---

        [Fact]
        public void TextMessage_GroupWithOnlyData_Compiles()
        {
            // TextMessage has a group "attachments" with only <data> (no fixed fields)
            // Verifying the generated code compiles and basic structure works
            Assert.True(TextMessageData.MESSAGE_SIZE > 0);
            Assert.Equal(21, TextMessageData.MESSAGE_ID);
        }

        [Fact]
        public void TextMessage_ConsumeVariableLength_WithDataOnlyGroup()
        {
            var buffer = new byte[512];
            var span = buffer.AsSpan();
            int offset = 0;

            // Write TextMessage fixed fields
            ref TextMessageData msg = ref MemoryMarshal.AsRef<TextMessageData>(span);
            msg.MessageId = 42L;
            offset += TextMessageData.MESSAGE_SIZE;

            // attachments group: 1 entry (group has no fixed fields, only data)
            ref var groupHeader = ref MemoryMarshal.AsRef<GroupSizeEncoding>(span.Slice(offset));
            groupHeader.BlockLength = 0; // no fixed fields
            groupHeader.NumInGroup = 1;
            offset += GroupSizeEncoding.MESSAGE_SIZE;

            // content data for the attachment (VarStringEncoding: uint32 length + data)
            var contentBytes = Encoding.UTF8.GetBytes("Hello World");
            BitConverter.TryWriteBytes(span.Slice(offset, 4), (uint)contentBytes.Length);
            contentBytes.CopyTo(span.Slice(offset + 4));
            offset += 4 + contentBytes.Length;

            // body data (message-level VarStringEncoding)
            var bodyBytes = Encoding.UTF8.GetBytes("Message body");
            BitConverter.TryWriteBytes(span.Slice(offset, 4), (uint)bodyBytes.Length);
            bodyBytes.CopyTo(span.Slice(offset + 4));
            offset += 4 + bodyBytes.Length;

            // Parse
            string capturedContent = "";
            string capturedBody = "";
            Assert.True(TextMessageData.TryParse(span.Slice(0, offset), out var parsedReader));
            Assert.Equal(42L, parsedReader.Data.MessageId);
            parsedReader.ReadGroups(
                (in TextMessageData.AttachmentsData attachments) => { },
                attachmentContent => { capturedContent = Encoding.UTF8.GetString(attachmentContent.VarData); },
                body => { capturedBody = Encoding.UTF8.GetString(body.VarData); }
            );

            Assert.Equal("Hello World", capturedContent);
            Assert.Equal("Message body", capturedBody);
        }
    }
}
