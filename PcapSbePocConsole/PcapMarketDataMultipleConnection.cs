using B3.Market.Data.Messages;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace PcapSbePocConsole
{
    public class PcapMarketDataMultipleConnection : IMarketDataConnection
    {
        private uint lastConsumed;
        private readonly PcapReplayer[] replayers;
        private readonly UdpClient[] clients;
        private readonly AddressConfig[] configs;

        public bool IsConnected => clients.Any(c => c.Client.Connected);

        public PcapMarketDataMultipleConnection(AddressConfig[] configs)
        {
            this.configs = configs;
            replayers = new PcapReplayer[configs.Length];
            clients = new UdpClient[configs.Length];
            for (int i = 0; i < configs.Length; i++)
            {
                replayers[i] = new PcapReplayer(configs[i]);
                clients[i] = new UdpClient(configs[i].MulticastEndpoint.Port);
            }

        }

        public void Dispose()
        {
            for (int i = 0; i < replayers.Length; i++)
            { 
                replayers[i].Dispose();
                clients[i].Dispose();
            }
        }

        public int Receive(byte[] buffer)
        {
            for (int i = 0; i < clients.Length; i++)
            {
                var socketEndpoint = (EndPoint)configs[i].MulticastEndpoint;
                int length = clients[i].Client.ReceiveFrom(buffer, ref socketEndpoint);
                if (ShouldReturn(buffer.AsSpan(0, length)))
                {
                    return length;
                }
            }
            return 0;
        }

        private bool ShouldReturn(ReadOnlySpan<byte> data)
        {
            ref readonly PacketHeader packet = ref MemoryMarshal.AsRef<PacketHeader>(data);
            if (packet.SequenceNumber != 0 
                && packet.SequenceNumber <= lastConsumed)
                return false;
            lastConsumed = packet.SequenceNumber;
            return true;
        }

        public void Connect()
        {
            for (int i = 0; i < configs.Length; i++)
            {
                replayers[i].Start();
                clients[i].JoinMulticastGroup(configs[i].MulticastEndpoint.Address);
            }
        }
    }
}