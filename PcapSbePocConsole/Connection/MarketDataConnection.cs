using PcapSbePocConsole.Configs;
using System.Net;
using System.Net.Sockets;

namespace PcapSbePocConsole.Connection
{
    public class MarketDataConnection : IMarketDataConnection
    {
        private readonly AddressConfig config;
        private readonly UdpClient client;

        public bool IsConnected => client.Client.IsBound;

        public MarketDataConnection(AddressConfig config)
        {
            this.config = config;
            client = new UdpClient(config.MulticastEndpoint.Port);
        }

        public void Dispose()
        {
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
        }
    }
}