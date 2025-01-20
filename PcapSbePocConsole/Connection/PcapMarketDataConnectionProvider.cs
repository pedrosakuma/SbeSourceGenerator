using PcapSbePocConsole.Configs;

namespace PcapSbePocConsole.Connection
{
    internal class PcapMarketDataConnectionProvider : IMarketDataConnectionProvider
    {
        private readonly MarketConfig config;

        public PcapMarketDataConnectionProvider(MarketConfig config)
        {
            this.config = config;
        }

        public IMarketDataConnection ConnectIncrementals(byte channel, Feeds feeds)
        {
            if (!config.Channels.TryGetValue(channel, out var c))
                throw new ArgumentException("Channel not found");
            var addresses = new AddressConfig[int.PopCount(feeds.GetHashCode())];
            int addressesIndex = 0;
            if (feeds.HasFlag(Feeds.FeedA))
                addresses[addressesIndex++] = c.Incrementals.FeedA;
            if (feeds.HasFlag(Feeds.FeedB))
                addresses[addressesIndex++] = c.Incrementals.FeedB;
            if (feeds.HasFlag(Feeds.FeedC))
                addresses[addressesIndex++] = c.Incrementals.FeedC;
            return new PcapMarketDataMultipleConnection(addresses);
        }

        public IMarketDataConnection ConnectInstrumentDefinition(byte channel)
        {
            if (!config.Channels.TryGetValue(channel, out var c))
                throw new ArgumentException("Channel not found");
            return new PcapMarketDataConnection(c.InstrumentDefinition);
        }

        public IMarketDataConnection ConnectSnapshot(byte channel)
        {
            if (!config.Channels.TryGetValue(channel, out var c))
                throw new ArgumentException("Channel not found");
            return new PcapMarketDataConnection(c.Snapshot);
        }
    }
}