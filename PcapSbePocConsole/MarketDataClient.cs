using PcapSbePocConsole.Connection;

namespace PcapSbePocConsole
{
    public class MarketDataClient
    {
        private readonly IMarketDataConnectionProvider marketDataConnection;

        public MarketDataClient(IMarketDataConnectionProvider marketDataConnection)
        {
            this.marketDataConnection = marketDataConnection;
        }

        public ChannelState SyncInstrumentDefinitions(byte channel)
        {
            var execution = new InstrumentDefinitionSyncExecutionState(this.marketDataConnection);
            return execution.Sync(channel);
        }
    }
}