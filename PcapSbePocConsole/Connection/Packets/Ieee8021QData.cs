using PacketDotNet;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace PcapSbePocConsole.Connection.Packets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct Ieee8021QData
    {
        [FieldOffset(0)]
        public short TagControlInformation;
        [FieldOffset(2)]
        public ushort type;
        public EthernetType Type => (EthernetType)BinaryPrimitives.ReverseEndianness(type);
    }
}
