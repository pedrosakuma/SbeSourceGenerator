using PacketDotNet;
using PcapSbePocConsole.Configs;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Net;
using System.Net.Sockets;

namespace PcapSbePocConsole.Connection
{
    public class PcapReplayer
    {
        private readonly CaptureFileReaderDevice device;
        private readonly UdpClient client;
        private readonly AddressConfig config;

        public bool Connected => device.Opened;

        public PcapReplayer(AddressConfig config)
        {
            device = new CaptureFileReaderDevice(config.Address);
            device.Open(new DeviceConfiguration
            {
                BufferSize = 524288,
                Immediate = false,

            });
            device.OnPacketArrival += Device_OnPacketArrival;
            device.OnCaptureStopped += Device_OnCaptureStopped;

            client = new UdpClient(AddressFamily.InterNetwork);
            client.MulticastLoopback = true;
            this.config = config;
        }

        private void Device_OnCaptureStopped(object sender, CaptureStoppedEventStatus status)
        {
            client.DropMulticastGroup(config.MulticastEndpoint.Address);
        }

        private void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            var p = e.GetPacket().GetPacket();
            var udp = p.Extract<UdpPacket>();
            client.Send(udp.PayloadData, udp.PayloadData.Length, config.MulticastEndpoint);
        }

        public void Dispose()
        {
            device.Dispose();
            client.Dispose();
        }

        public void Start()
        {
            client.JoinMulticastGroup(config.MulticastEndpoint.Address, IPAddress.Loopback);
            device.StartCapture();
        }
    }
}
