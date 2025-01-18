using System.Net;

namespace PcapSbePocConsole
{
    public record AddressConfig(string Address, IPEndPoint MulticastEndpoint);
}