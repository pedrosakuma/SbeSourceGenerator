namespace PcapSbePocConsole
{
    public class MarketConfig
    {
        public Dictionary<byte, ChannelConfig> Channels { get; init; }
    }
    public class ChannelConfig {
        public byte Channel { get; init; }
        public InstrumentDefinitionConfig InstrumentDefinition { get; init; }
        public SnapshotConfig Snapshot { get; init; }
        public IncrementalsConfig Incrementals { get; init; }
    }
}