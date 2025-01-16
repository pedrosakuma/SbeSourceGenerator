using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;
using System.Text;
namespace PcapSbePocConsole
{
    public class MarketDataClient
    {
        private readonly IMarketDataConnectionProvider marketDataConnection;

        public MarketDataClient(IMarketDataConnectionProvider marketDataConnection)
        {
            this.marketDataConnection = marketDataConnection;
        }

        public async Task<ChannelState> SyncInstrumentDefinitions(byte channel) 
        {
            var execution = new InstrumentDefinitionSyncExecutionState(this.marketDataConnection);
            return await execution.SyncAsync(channel);
        }
    }
}