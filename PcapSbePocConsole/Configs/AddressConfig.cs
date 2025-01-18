using System.Net;

namespace PcapSbePocConsole.Configs
{
    public record AddressConfig(string Address, IPEndPoint MulticastEndpoint);
}