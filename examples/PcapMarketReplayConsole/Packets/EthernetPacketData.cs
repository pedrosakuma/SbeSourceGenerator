using PacketDotNet;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace PcapMarketReplayConsole.Packets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct EthernetPacketData
    {
        [FieldOffset(0)]
        public MacAddress DestinationMac;
        [FieldOffset(6)]
        public MacAddress SourceMac;
        [FieldOffset(12)]
        private ushort type;
        public EthernetType Type => (EthernetType)BinaryPrimitives.ReverseEndianness(type);
    }
}
