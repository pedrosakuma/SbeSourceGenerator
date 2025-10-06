using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace PcapMarketReplayConsole.Packets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct UdpData
    {
        [FieldOffset(0)]
        public ushort SourcePort;
        [FieldOffset(2)]
        public ushort DestinationPort;
        [FieldOffset(4)]
        public ushort length;
        public ushort Length => BinaryPrimitives.ReverseEndianness(length);
        [FieldOffset(6)]
        private ushort checksum;
        public ushort Checksum => BinaryPrimitives.ReverseEndianness(checksum);
    }
}
