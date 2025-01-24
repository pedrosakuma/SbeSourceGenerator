using System.Net;

namespace PcapSbePocConsole.Configs
{
    public record AddressConfig
    {
        public AddressConfig(string address, IPEndPoint multicastEndpoint)
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