using PacketDotNet;
using PcapSbePocConsole.Configs;
using PcapSbePocConsole.Connection.Packets;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace PcapSbePocConsole.Connection
{
    public class PcapReplayer
    {
        private readonly CaptureFileReaderDevice device;
        private readonly UdpClient client;
        private readonly AddressConfig config;
        private readonly Task consumer;

        public bool Connected => device.Opened;

        public PcapReplayer(AddressConfig config)
        {
            device = new CaptureFileReaderDevice(config.Address);
            device.Open(new DeviceConfiguration
            {
                BufferSize = 524288,
                Immediate = true,
            });
            consumer = new Task(Consume, TaskCreationOptions.LongRunning);

            client = new UdpClient(AddressFamily.InterNetwork);
            client.MulticastLoopback = true;
            this.config = config;
        }

        private unsafe static ReadOnlySpan<byte> GetPayloadSpan(ReadOnlySpan<byte> raw)
        {
            var remainderData = raw;
            ref readonly var eth = ref MemoryMarshal.AsRef<EthernetPacketData>(remainderData);
            remainderData = remainderData.Slice(sizeof(EthernetPacketData));
            var type = eth.Type;
            while (true)
            {
                switch (type)
                {
                    case EthernetType.IPv4:
                        ref readonly var ipv4 = ref MemoryMarshal.AsRef<IPv4Data>(remainderData);
                        remainderData = remainderData.Slice(sizeof(IPv4Data));
                        switch (ipv4.Protocol)
                        {
                            case PacketDotNet.ProtocolType.Udp:
                                ref readonly var udpd = ref MemoryMarshal.AsRef<UdpData>(remainderData);
                                return remainderData.Slice(sizeof(UdpData), udpd.Length - sizeof(UdpData));
                            default:
                                throw new InvalidOperationException();
                        }
                    case EthernetType.VLanTaggedFrame:
                    case EthernetType.ProviderBridging:
                    case EthernetType.QInQ:
                        ref readonly var ieee8021 = ref MemoryMarshal.AsRef<Ieee8021QData>(remainderData);
                        remainderData = remainderData.Slice(sizeof(Ieee8021QData));
                        type = ieee8021.Type;
                        break;
                    default:
                        break;
                }
            }
        }

        public void Dispose()
        {
            device.Dispose();
            client.Dispose();
        }

        public void Start()
        {
            client.Connect(config.MulticastEndpoint.Address, config.MulticastEndpoint.Port);
            consumer.Start();
        }

        private unsafe void Consume()
        {
            nint header = IntPtr.Zero;
            nint data = IntPtr.Zero;
            while (Connected)
            {
                device.GetNextPacketPointers(ref header, ref data);
                var dataSpan = new Span<byte>(
                    data.ToPointer(),
                    Marshal.ReadInt32(header + Header.CaptureLengthOffset));

                client.Client.Send(
                    GetPayloadSpan(dataSpan),
                    SocketFlags.None);
            }
        }
    }
}
