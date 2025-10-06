using System.Net;

namespace PcapMarketReplayConsole
{
    public record PcapReplayConfig
    {

        public PcapReplayConfig(string address, IPEndPoint multicastEndpoint)
        {
            this.Address = address;
            this.MulticastEndpoint = multicastEndpoint;
            this.MulticastSocketAddressEndpoint = multicastEndpoint.Serialize();
        }

        public string Address { get; }
        public IPEndPoint MulticastEndpoint { get; }
        public SocketAddress MulticastSocketAddressEndpoint { get; }
    }
}