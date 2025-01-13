using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;
using System.Runtime.InteropServices;

namespace PcapSbePocConsole
{
    public partial class MarketDataHandler : BaseParser
    {
        public Dictionary<int, int> Statistics;
        private Dictionary<ulong, InstrumentDefinition> instrumentsById;
        private Dictionary<string, InstrumentDefinition> instrumentsBySymbol;
        private MarketDataState state;

        public event Action<MarketDataState>? StateChanged;

        public MarketDataHandler(Dictionary<ulong, InstrumentDefinition> instrumentsById, Dictionary<string, InstrumentDefinition> instrumentsBySymbol)
        {
            this.Statistics = MessageIds.ToDictionary(k => k, v => 0);
            this.state = MarketDataState.None;
            this.instrumentsById = instrumentsById;
            this.instrumentsBySymbol = instrumentsBySymbol;
        }

        public void ChangeState(MarketDataState state)
        {
            this.state = state;
            StateChanged?.Invoke(state);
        }

        public override void Callback(ref readonly SequenceReset_1Data message, ReadOnlySpan<byte> variablePart)
        {
            switch (state)
            {
                case MarketDataState.None:
                    state = MarketDataState.InstrumentDefinition;
                    StateChanged?.Invoke(state);
                    break;
                case MarketDataState.InstrumentDefinition:
                    state = MarketDataState.Snapshot;
                    StateChanged?.Invoke(state);
                    break;
            }
        }
        public void RegisterStatistics(int type)
        {
            ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(Statistics, type, out _);
            Interlocked.Increment(ref count);
        }

        public override void Callback(ref readonly AuctionImbalance_19Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(AuctionImbalance_19Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }

        public override void Callback(ref readonly SettlementPrice_28Data message, ReadOnlySpan<byte> variablePart)
        {
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly OpenInterest_29Data message, ReadOnlySpan<byte> variablePart)
        {
            base.Callback(in message, variablePart);
        }
        
        
        public override void Callback(ref readonly ChannelReset_11Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(ChannelReset_11Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }

        public override void Callback(ref readonly News_5Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(News_5Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }

        public override void Callback(ref readonly Sequence_2Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(Sequence_2Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly SnapshotFullRefresh_Header_30Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(SnapshotFullRefresh_Header_30Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }

        
        internal void Init()
        {
            StateChanged?.Invoke(MarketDataState.None);
        }
    }
}
