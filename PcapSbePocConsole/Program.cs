using B3.Market.Data.Messages;
using SharpPcap.LibPcap;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace PcapSbePocConsole
{
    [StructLayout(LayoutKind.Explicit)]
    public struct SBEHeader
    {
        [FieldOffset(0)]
        public SBEFramingHeader Framing;
        [FieldOffset(4)]
        public SBEMessageHeader Message;
        public override string ToString()
        {
            return $"\tFraming: {Framing}\n\tMessage: {Message}\n";
        }
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct SBEPacketHeader {
        [FieldOffset(0)]
        public byte ChannelId;
        [FieldOffset(1)]
        public byte Reserved;
        [FieldOffset(2)]
        public ushort SequenceVersion;
        [FieldOffset(4)]
        public uint SequenceNumber;
        [FieldOffset(8)]
        public ulong SendingTime;
        public override string ToString()
        {
            return $"ChannelId: {ChannelId}, Reserved: {Reserved}, SequenceVersion: {SequenceVersion}, SequenceNumber: {SequenceNumber}, SendingTime: {DateTimeOffset.UnixEpoch.AddMicroseconds(SendingTime / 1000)}";
        }
    }
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct SBEFramingHeader
    {
        [FieldOffset(0)]
        public ushort MessageLength;
        [FieldOffset(2)]
        public fixed byte EncodingType[2];
        public override string ToString()
        {
            fixed (void* p = EncodingType)
                return $"MessageLength: {MessageLength}, EncodingType: {Convert.ToHexString(new ReadOnlySpan<byte>(p, 2))}";
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SBEMessageHeader
    {
        [FieldOffset(0)]
        public ushort BlockSize;
        [FieldOffset(2)]
        public ushort TemplateId;
        [FieldOffset(4)]
        public ushort SchemaId;
        [FieldOffset(6)]
        public ushort SchemaVersion;
        public override string ToString()
        {
            return $"BlockSize: {BlockSize}, TemplateId: {TemplateId}, SchemaId: {SchemaId}, SchemaVersion: {SchemaVersion}";
        }
    }
    public partial struct Price8
    {
        /// <summary>
        /// Mantissa (for fixed-point decimal numbers).
        /// </summary>
        public long Mantissa;
        /// <summary>
        /// Exponent (for fixed-point decimal numbers).
        /// </summary>
        public const sbyte Exponent = -8;
        public decimal Value => Mantissa * (decimal)Math.Pow(10, Exponent);
        public decimal Value2
        {
            get
            {
                return ToDecimalWithPrecision(Mantissa, -Exponent);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal ToDecimalWithPrecision(long value, byte scale)
        {
            bool negative = value < 0;
            if (negative)
                value = -value;
            return new decimal((int)value, (int)(value >> 32), 0, negative, scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal ToDecimalWithPrecision(int value, byte scale)
        {
            bool negative = value < 0;
            if (negative)
                value = -value;
            return new decimal(value, 0, 0, negative, scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal ToDecimalWithPrecision(short value, byte scale)
        {
            bool negative = value < 0;
            if (negative)
                value = (short)-value;
            return new decimal(value, 0, 0, negative, scale);
        }
    }
    
    public partial class Program
    {
        private const int PCAPHeaderSize = 46;
        static void Main(string[] args)
        {
            return;
            using var device = new CaptureFileReaderDevice(args[0]);
            device.Open(new SharpPcap.DeviceConfiguration { 
            });
            device.OnPacketArrival += Device_OnPacketArrival;
            device.Capture();
        }

        private static void Device_OnPacketArrival(object sender, SharpPcap.PacketCapture e)
        {
            var data = e.Data.Slice(PCAPHeaderSize);

            ref readonly SBEPacketHeader packet = ref MemoryMarshal.AsRef<SBEPacketHeader>(data);
            Console.WriteLine(packet);
            data = data.Slice(Unsafe.SizeOf<SBEPacketHeader>());
            do
            {
                ref readonly SBEHeader header = ref MemoryMarshal.AsRef<SBEHeader>(data);
                data = data.Slice(Unsafe.SizeOf<SBEHeader>());
                var length = header.Framing.MessageLength - Unsafe.SizeOf<SBEHeader>();
                var body = data.Slice(0, length);
                Console.WriteLine(header);
                data = data.Slice(length);
            }
            while (data.Length != 0);
        }
    }
}