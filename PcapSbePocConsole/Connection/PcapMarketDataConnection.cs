using PcapSbePocConsole.Configs;
using System.Net;
using System.Net.Sockets;

namespace PcapSbePocConsole.Connection
{
    public class PcapMarketDataConnection : IMarketDataConnection
    {
        private readonly AddressConfig config;
        private readonly PcapReplayer replayer;
        private readonly UdpClient client;

        public bool IsConnected => client.Client.IsBound;

        public PcapMarketDataConnection(AddressConfig config)
        {
            this.config = config;
            replayer = new PcapReplayer(config);
            client = new UdpClient(config.MulticastEndpoint.Port);
        }

        public void Dispose()
        {
            replayer.Dispose();
            client.Dispose();
        }

        public int Receive(byte[] buffer)
        {
            var endpoint = (EndPoint)config.MulticastEndpoint;
            return client.Client.ReceiveFrom(buffer, ref endpoint);
        }

        public void Connect()
        {
            client.JoinMulticastGroup(config.MulticastEndpoint.Address);
            replayer.Start();
        }
    }
}