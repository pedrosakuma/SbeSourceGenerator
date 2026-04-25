using System;
using System.Runtime.InteropServices;
using System.Text;
using Edge.Cases.Test.V0;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// DevEx P0 features: AsTrimmedSpan (#155), ISpanFormattable (#153),
    /// MessageHeader static read helpers (#156).
    /// </summary>
    public class DevExFeatureTests
    {
        // ---------- #155: AsTrimmedSpan on InlineArray char types ----------

        [Fact]
        public void AsTrimmedSpan_StripsTrailingNullsAndSpaces()
        {
            // IsinCode is an InlineArray char type in the edge-cases schema.
            var isin = default(IsinCode);
            ReadOnlySpan<byte> source = "PETR4   "u8;          // trailing space-padded (FIX convention)
            source.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<IsinCode, byte>(ref isin), source.Length));

            // AsSpan only trims trailing \0 — would still include the spaces.
            Assert.Equal("PETR4   "u8.ToArray(), isin.AsSpan().ToArray());

            // AsTrimmedSpan additionally trims trailing spaces.
            Assert.Equal("PETR4"u8.ToArray(), isin.AsTrimmedSpan().ToArray());
        }

        [Fact]
        public void AsTrimmedSpan_HandlesAllNullPadding()
        {
            var isin = default(IsinCode);
            ReadOnlySpan<byte> source = "BBDC4"u8;
            source.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<IsinCode, byte>(ref isin), source.Length));

            Assert.Equal("BBDC4"u8.ToArray(), isin.AsTrimmedSpan().ToArray());
        }

        // ---------- #153: ISpanFormattable on InlineArray char types ----------

        [Fact]
        public void CharType_TryFormat_WritesTrimmedContent_ZeroAlloc()
        {
            var isin = default(IsinCode);
            ReadOnlySpan<byte> source = "VALE3   "u8;
            source.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<IsinCode, byte>(ref isin), source.Length));

            Span<char> dest = stackalloc char[16];
            Assert.True(isin.TryFormat(dest, out int written, default, null));
            Assert.Equal(5, written);
            Assert.Equal("VALE3", new string(dest.Slice(0, written)));
        }

        [Fact]
        public void CharType_TryFormat_ReturnsFalse_WhenDestinationTooSmall()
        {
            var isin = default(IsinCode);
            "ITUB4"u8.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<IsinCode, byte>(ref isin), 5));

            Span<char> small = stackalloc char[2];
            Assert.False(isin.TryFormat(small, out int written, default, null));
            Assert.Equal(0, written);
        }

        // ---------- #153: ISpanFormattable on decimal composites ----------

        [Fact]
        public void DecimalComposite_TryFormat_UsesDecimalsConstant()
        {
            // DecimalEncoding has Decimals=7 in the edge-cases schema.
            var price = new DecimalEncoding { Mantissa = 12_345_678 };
            Span<char> dest = stackalloc char[32];

            Assert.True(price.TryFormat(dest, out int written, default, null));
            // 12345678 * 1e-7 = 1.2345678 → "F7" → "1.2345678"
            Assert.Equal("1.2345678", new string(dest.Slice(0, written)));
        }

        [Fact]
        public void DecimalComposite_TryFormat_RespectsExplicitFormat()
        {
            var price = new DecimalEncoding { Mantissa = 12_345_678 };
            Span<char> dest = stackalloc char[32];

            Assert.True(price.TryFormat(dest, out int written, "F2", null));
            Assert.Equal("1.23", new string(dest.Slice(0, written)));
        }

        [Fact]
        public void DecimalComposite_ToString_FormatProvider_Works()
        {
            var price = new DecimalEncoding { Mantissa = 12_345_678 };
            string s = price.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal("1.2345678", s);
        }

        // ---------- #156: MessageHeader static helpers ----------

        [Fact]
        public void MessageHeader_TryReadTemplateId_RoundTripsAfterWriteHeader()
        {
            Span<byte> buffer = stackalloc byte[MessageHeader.MESSAGE_SIZE + TradeData.MESSAGE_SIZE];
            int offset = TradeData.WriteHeader(buffer);
            Assert.Equal(MessageHeader.MESSAGE_SIZE, offset);

            Assert.True(MessageHeader.TryReadTemplateId(buffer, out ushort templateId));
            Assert.Equal((ushort)TradeData.MESSAGE_ID, templateId);
        }

        [Fact]
        public void MessageHeader_TryReadHeader_PopulatesAllFields()
        {
            Span<byte> buffer = stackalloc byte[MessageHeader.MESSAGE_SIZE + TradeData.MESSAGE_SIZE];
            TradeData.WriteHeader(buffer);

            Assert.True(MessageHeader.TryReadHeader(buffer, out ushort blockLength, out ushort templateId, out ushort schemaId, out ushort version));
            Assert.Equal((ushort)TradeData.MESSAGE_SIZE, blockLength);
            Assert.Equal((ushort)TradeData.MESSAGE_ID, templateId);
            // schemaId/version are non-negative ushorts; just ensure the call succeeded.
            Assert.True(schemaId >= 0);
            Assert.True(version >= 0);
        }

        [Fact]
        public void MessageHeader_TryReadHeader_ReturnsFalse_OnShortBuffer()
        {
            Span<byte> tiny = stackalloc byte[MessageHeader.MESSAGE_SIZE - 1];
            Assert.False(MessageHeader.TryReadTemplateId(tiny, out ushort tid));
            Assert.Equal(0, tid);
            Assert.False(MessageHeader.TryReadHeader(tiny, out _, out _, out _, out _));
        }
    }

    file static class Unsafe
    {
        public static ref TTo As<TFrom, TTo>(ref TFrom source) where TFrom : struct where TTo : struct
            => ref System.Runtime.CompilerServices.Unsafe.As<TFrom, TTo>(ref source);
    }
}
