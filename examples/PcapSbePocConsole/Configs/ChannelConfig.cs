namespace PcapSbePocConsole.Configs
{
    public record ChannelConfig(
        byte Channel,
        AddressConfig InstrumentDefinition,
        AddressConfig Snapshot,
        IncrementalsConfig Incrementals
    );
}