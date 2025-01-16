namespace PcapSbePocConsole
{
    internal class PcapMarketDataConnectionProvider : IMarketDataConnectionProvider
    {
        private readonly MarketConfig config;
        private readonly DateTime readFrom;

        public PcapMarketDataConnectionProvider(MarketConfig config, DateTime readFrom)
        {
            this.config = config;
            this.readFrom = readFrom;
        }

        public IMarketDataConnection ConnectIncrementals(byte channel, Feeds feeds)
        {
            if (!this.config.Channels.TryGetValue(channel, out var c))
                throw new ArgumentException("Channel not found");
            var addresses = new string[int.PopCount(feeds.GetHashCode())];
            int addressesIndex = 0;
            if (feeds.HasFlag(Feeds.FeedA))
                addresses[addressesIndex++] = c.Incrementals.AddressFeedA;
            if (feeds.HasFlag(Feeds.FeedB))
                addresses[addressesIndex++] = c.Incrementals.AddressFeedB;
            if (feeds.HasFlag(Feeds.FeedC))
                addresses[addressesIndex++] = c.Incrementals.AddressFeedC;
            return new PcapMarketDataMultipleConnection(addresses, 1024);
        }

        public IMarketDataConnection ConnectInstrumentDefinition(byte channel)
        {
            if (!this.config.Channels.TryGetValue(channel, out var c))
                throw new ArgumentException("Channel not found");
            return new PcapMarketDataConnection(c.InstrumentDefinition.Address, 1024, readFrom);
        }

        public IMarketDataConnection ConnectSnapshot(byte channel)
        {
            if (!this.config.Channels.TryGetValue(channel, out var c))
                throw new ArgumentException("Channel not found");
            return new PcapMarketDataConnection(c.Snapshot.Address, 1024, readFrom);
        }
    }
}