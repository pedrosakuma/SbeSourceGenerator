using System.Runtime.InteropServices;

namespace PcapMarketReplayConsole.Packets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct IPv4Data
    {
        [FieldOffset(0)]
        public byte VersionAndHeaderLength;
        [FieldOffset(1)]
        public byte DifferentiatedServices;
        [FieldOffset(2)]
        public ushort TotalLength;
        [FieldOffset(4)]
        public ushort Identification;
        [FieldOffset(6)]
        public ushort FlagsAndFragmentOffset;
        [FieldOffset(8)]
        public byte TimeToLive;
        [FieldOffset(9)]
        public PacketDotNet.ProtocolType Protocol;
        [FieldOffset(10)]
        public ushort HeaderChecksum;
        [FieldOffset(12)]
        public uint SourceAddress;
        [FieldOffset(16)]
        public uint DestinationAddress;
    }
}
