using System.Net;

namespace PcapSbePocConsole.Configs
{
    public record AddressConfig
    {
        public AddressConfig(IPEndPoint multicastEndpoint)
        {
            this.MulticastEndpoint = multicastEndpoint;
            this.MulticastSocketAddressEndpoint = multicastEndpoint.Serialize();
        }

        public IPEndPoint MulticastEndpoint { get; }
        public SocketAddress MulticastSocketAddressEndpoint { get; }
    }
}