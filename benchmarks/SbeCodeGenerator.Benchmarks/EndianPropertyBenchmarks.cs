using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace SbeCodeGenerator.Benchmarks;

/// <summary>
/// Benchmarks comparing field access strategies for endianness support:
/// 1. Direct field access (current approach)
/// 2. Property passthrough (no bswap) — proposed for little-endian schemas
/// 3. Property with BinaryPrimitives.ReverseEndianness — proposed for big-endian schemas
/// </summary>
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
[SimpleJob(warmupCount: 5, iterationCount: 20)]
public class EndianPropertyBenchmarks
{
    private byte[] _buffer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new byte[1024];
        // Pre-fill buffer with known values
        var writer = new Span<byte>(_buffer);
        BinaryPrimitives.WriteInt64LittleEndian(writer, 123456789L);
        BinaryPrimitives.WriteInt64LittleEndian(writer.Slice(8), 995000L);
        BinaryPrimitives.WriteInt64LittleEndian(writer.Slice(16), 100L);
        BinaryPrimitives.WriteInt64LittleEndian(writer.Slice(24), 42L);
        BinaryPrimitives.WriteInt32LittleEndian(writer.Slice(32), 7);
        BinaryPrimitives.WriteInt16LittleEndian(writer.Slice(36), 3);
    }

    // ─── DECODE BENCHMARKS ─────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Decode: Direct Fields")]
    public long DecodeDirectFields()
    {
        var msg = MemoryMarshal.Read<DirectFieldMessage>(_buffer);
        return msg.OrderId + msg.Price + msg.Quantity + msg.Extra + msg.Status + msg.Flags;
    }

    [Benchmark(Description = "Decode: Property Passthrough")]
    public long DecodePropertyPassthrough()
    {
        var msg = MemoryMarshal.Read<PropertyPassthroughMessage>(_buffer);
        return msg.OrderId + msg.Price + msg.Quantity + msg.Extra + msg.Status + msg.Flags;
    }

    [Benchmark(Description = "Decode: Property + Ternary bswap")]
    public long DecodePropertyReverseTernary()
    {
        var msg = MemoryMarshal.Read<PropertyReverseTernaryMessage>(_buffer);
        return msg.OrderId + msg.Price + msg.Quantity + msg.Extra + msg.Status + msg.Flags;
    }

    [Benchmark(Description = "Decode: Property + If bswap")]
    public long DecodePropertyReverseIf()
    {
        var msg = MemoryMarshal.Read<PropertyReverseIfMessage>(_buffer);
        return msg.OrderId + msg.Price + msg.Quantity + msg.Extra + msg.Status + msg.Flags;
    }

    // ─── ENCODE BENCHMARKS ─────────────────────────────────────

    [Benchmark(Description = "Encode: Direct Fields")]
    public bool EncodeDirectFields()
    {
        var msg = new DirectFieldMessage
        {
            OrderId = 123456789L,
            Price = 995000L,
            Quantity = 100L,
            Extra = 42L,
            Status = 7,
            Flags = 3
        };
        MemoryMarshal.Write(_buffer, in msg);
        return true;
    }

    [Benchmark(Description = "Encode: Property Passthrough")]
    public bool EncodePropertyPassthrough()
    {
        var msg = new PropertyPassthroughMessage();
        msg.OrderId = 123456789L;
        msg.Price = 995000L;
        msg.Quantity = 100L;
        msg.Extra = 42L;
        msg.Status = 7;
        msg.Flags = 3;
        MemoryMarshal.Write(_buffer, in msg);
        return true;
    }

    [Benchmark(Description = "Encode: Property + Ternary bswap")]
    public bool EncodePropertyReverseTernary()
    {
        var msg = new PropertyReverseTernaryMessage();
        msg.OrderId = 123456789L;
        msg.Price = 995000L;
        msg.Quantity = 100L;
        msg.Extra = 42L;
        msg.Status = 7;
        msg.Flags = 3;
        MemoryMarshal.Write(_buffer, in msg);
        return true;
    }

    [Benchmark(Description = "Encode: Property + If bswap")]
    public bool EncodePropertyReverseIf()
    {
        var msg = new PropertyReverseIfMessage();
        msg.OrderId = 123456789L;
        msg.Price = 995000L;
        msg.Quantity = 100L;
        msg.Extra = 42L;
        msg.Status = 7;
        msg.Flags = 3;
        MemoryMarshal.Write(_buffer, in msg);
        return true;
    }

    // ─── ROUND-TRIP BENCHMARKS ─────────────────────────────────

    [Benchmark(Description = "RoundTrip: Direct Fields")]
    public long RoundTripDirectFields()
    {
        var msg = new DirectFieldMessage
        {
            OrderId = 123456789L,
            Price = 995000L,
            Quantity = 100L,
            Extra = 42L,
            Status = 7,
            Flags = 3
        };
        MemoryMarshal.Write(_buffer, in msg);
        var decoded = MemoryMarshal.Read<DirectFieldMessage>(_buffer);
        return decoded.OrderId + decoded.Price + decoded.Quantity + decoded.Extra + decoded.Status + decoded.Flags;
    }

    [Benchmark(Description = "RoundTrip: Property Passthrough")]
    public long RoundTripPropertyPassthrough()
    {
        var msg = new PropertyPassthroughMessage();
        msg.OrderId = 123456789L;
        msg.Price = 995000L;
        msg.Quantity = 100L;
        msg.Extra = 42L;
        msg.Status = 7;
        msg.Flags = 3;
        MemoryMarshal.Write(_buffer, in msg);
        var decoded = MemoryMarshal.Read<PropertyPassthroughMessage>(_buffer);
        return decoded.OrderId + decoded.Price + decoded.Quantity + decoded.Extra + decoded.Status + decoded.Flags;
    }

    [Benchmark(Description = "RoundTrip: Property + Ternary bswap")]
    public long RoundTripPropertyReverseTernary()
    {
        var msg = new PropertyReverseTernaryMessage();
        msg.OrderId = 123456789L;
        msg.Price = 995000L;
        msg.Quantity = 100L;
        msg.Extra = 42L;
        msg.Status = 7;
        msg.Flags = 3;
        MemoryMarshal.Write(_buffer, in msg);
        var decoded = MemoryMarshal.Read<PropertyReverseTernaryMessage>(_buffer);
        return decoded.OrderId + decoded.Price + decoded.Quantity + decoded.Extra + decoded.Status + decoded.Flags;
    }

    [Benchmark(Description = "RoundTrip: Property + If bswap")]
    public long RoundTripPropertyReverseIf()
    {
        var msg = new PropertyReverseIfMessage();
        msg.OrderId = 123456789L;
        msg.Price = 995000L;
        msg.Quantity = 100L;
        msg.Extra = 42L;
        msg.Status = 7;
        msg.Flags = 3;
        MemoryMarshal.Write(_buffer, in msg);
        var decoded = MemoryMarshal.Read<PropertyReverseIfMessage>(_buffer);
        return decoded.OrderId + decoded.Price + decoded.Quantity + decoded.Extra + decoded.Status + decoded.Flags;
    }
}

// ─── Approach 1: Direct public fields (current) ───────────────

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct DirectFieldMessage
{
    [FieldOffset(0)] public long OrderId;
    [FieldOffset(8)] public long Price;
    [FieldOffset(16)] public long Quantity;
    [FieldOffset(24)] public long Extra;
    [FieldOffset(32)] public int Status;
    [FieldOffset(36)] public short Flags;
}

// ─── Approach 2: Private fields + property passthrough (little-endian) ─

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct PropertyPassthroughMessage
{
    [FieldOffset(0)] private long orderId;
    [FieldOffset(8)] private long price;
    [FieldOffset(16)] private long quantity;
    [FieldOffset(24)] private long extra;
    [FieldOffset(32)] private int status;
    [FieldOffset(36)] private short flags;

    public long OrderId
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => orderId;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => orderId = value;
    }
    public long Price
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => price;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => price = value;
    }
    public long Quantity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => quantity;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => quantity = value;
    }
    public long Extra
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => extra;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => extra = value;
    }
    public int Status
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => status;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => status = value;
    }
    public short Flags
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => flags;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => flags = value;
    }
}

// ─── Approach 3: Ternary with ReverseEndianness (big-endian, ternary branch) ─

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct PropertyReverseTernaryMessage
{
    [FieldOffset(0)] private long orderId;
    [FieldOffset(8)] private long price;
    [FieldOffset(16)] private long quantity;
    [FieldOffset(24)] private long extra;
    [FieldOffset(32)] private int status;
    [FieldOffset(36)] private short flags;

    public long OrderId
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(orderId) : orderId;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => orderId = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }
    public long Price
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(price) : price;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => price = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }
    public long Quantity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(quantity) : quantity;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => quantity = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }
    public long Extra
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(extra) : extra;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => extra = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }
    public int Status
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(status) : status;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => status = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }
    public short Flags
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(flags) : flags;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => flags = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    }
}

// ─── Approach 4: If-statement with ReverseEndianness (big-endian, if branch) ─

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct PropertyReverseIfMessage
{
    [FieldOffset(0)] private long orderId;
    [FieldOffset(8)] private long price;
    [FieldOffset(16)] private long quantity;
    [FieldOffset(24)] private long extra;
    [FieldOffset(32)] private int status;
    [FieldOffset(36)] private short flags;

    public long OrderId
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { if (BitConverter.IsLittleEndian) return BinaryPrimitives.ReverseEndianness(orderId); return orderId; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set { if (BitConverter.IsLittleEndian) orderId = BinaryPrimitives.ReverseEndianness(value); else orderId = value; }
    }
    public long Price
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { if (BitConverter.IsLittleEndian) return BinaryPrimitives.ReverseEndianness(price); return price; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set { if (BitConverter.IsLittleEndian) price = BinaryPrimitives.ReverseEndianness(value); else price = value; }
    }
    public long Quantity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { if (BitConverter.IsLittleEndian) return BinaryPrimitives.ReverseEndianness(quantity); return quantity; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set { if (BitConverter.IsLittleEndian) quantity = BinaryPrimitives.ReverseEndianness(value); else quantity = value; }
    }
    public long Extra
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { if (BitConverter.IsLittleEndian) return BinaryPrimitives.ReverseEndianness(extra); return extra; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set { if (BitConverter.IsLittleEndian) extra = BinaryPrimitives.ReverseEndianness(value); else extra = value; }
    }
    public int Status
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { if (BitConverter.IsLittleEndian) return BinaryPrimitives.ReverseEndianness(status); return status; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set { if (BitConverter.IsLittleEndian) status = BinaryPrimitives.ReverseEndianness(value); else status = value; }
    }
    public short Flags
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { if (BitConverter.IsLittleEndian) return BinaryPrimitives.ReverseEndianness(flags); return flags; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set { if (BitConverter.IsLittleEndian) flags = BinaryPrimitives.ReverseEndianness(value); else flags = value; }
    }
}
