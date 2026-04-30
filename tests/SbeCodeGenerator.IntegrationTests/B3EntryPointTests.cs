using B3.Entrypoint.Fixp.Sbe.V6;
using Xunit;
using B3Boolean = B3.Entrypoint.Fixp.Sbe.V6.Boolean;

namespace SbeCodeGenerator.IntegrationTests;

/// <summary>
/// End-to-end smoke tests for the B3 Binary EntryPoint v8.4.2 vendor schema.
/// These tests exercise the fixes from issue #164: aliased encodingType resolution
/// (e.g. <c>Boolean : uint8EnumEncoding</c>) and BCL name collisions
/// (the schema declares <c>&lt;enum name="Boolean"&gt;</c>).
/// </summary>
public class B3EntryPointTests
{
    [Fact]
    public void BooleanEnum_HasByteUnderlyingType()
    {
        // Validates fix for aliased encodingType: <enum name="Boolean" encodingType="uint8EnumEncoding">
        // must resolve uint8EnumEncoding -> byte (not emit "enum Boolean : uint8EnumEncoding").
        Assert.Equal(typeof(byte), System.Enum.GetUnderlyingType(typeof(B3Boolean)));
        Assert.Equal((byte)0, (byte)B3Boolean.FALSE_VALUE);
        Assert.Equal((byte)1, (byte)B3Boolean.TRUE_VALUE);
    }

    [Fact]
    public void FlowTypeEnum_HasByteUnderlyingType()
    {
        Assert.Equal(typeof(byte), System.Enum.GetUnderlyingType(typeof(FlowType)));
    }

    [Fact]
    public void SequenceMessage_RoundTrip()
    {
        // Smoke test: encode + decode a message containing a constant valueRef
        // (MessageType.Sequence) and a simple field.
        SequenceData outbound = new() { NextSeqNo = 42u };

        System.Span<byte> buffer = stackalloc byte[SequenceData.MESSAGE_SIZE + 16];
        Assert.True(outbound.TryEncode(buffer, out int written));
        Assert.True(written > 0);

        Assert.True(SequenceData.TryParse(buffer, out SequenceDataReader reader));
        Assert.Equal(42u, (uint)reader.Data.NextSeqNo);
        Assert.Equal(MessageType.Sequence, SequenceData.MESSAGE_TYPE);
    }

    [Fact]
    public void SchemaConstants_AreExposed()
    {
        Assert.Equal(9, SequenceData.MESSAGE_ID);
        Assert.True(SequenceData.BLOCK_LENGTH > 0);
    }
}
