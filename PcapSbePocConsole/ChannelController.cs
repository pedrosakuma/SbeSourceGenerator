namespace PcapSbePocConsole
{
    public class ChannelController
    {
        private readonly byte number;
        private uint sequenceNumber;

        public ChannelController(byte number,
            MarketDataClient instrumentDefinitionClient
            //SnapshotClient snapshotClient,
            //IncrementalClient incrementalClient
            )
        {
            this.number = number;
        }
    }
}
