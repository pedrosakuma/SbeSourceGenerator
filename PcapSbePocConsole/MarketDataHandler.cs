using B3.Market.Data.Messages;
using System.Runtime.InteropServices;

namespace PcapSbePocConsole
{
    public partial class MarketDataHandler : BaseParser
    {
        public Dictionary<int, int> Statistics = new Dictionary<int, int>();
        private Dictionary<ulong, InstrumentDefinition> instrumentsById;
        private Dictionary<string, InstrumentDefinition> instrumentsBySymbol;
        private MarketDataState state;

        public event Action<MarketDataState>? StateChanged;

        public MarketDataHandler(Dictionary<ulong, InstrumentDefinition> instrumentsById, Dictionary<string, InstrumentDefinition> instrumentsBySymbol)
        {
            this.state = MarketDataState.None;
            this.instrumentsById = instrumentsById;
            this.instrumentsBySymbol = instrumentsBySymbol;
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
                case MarketDataState.Snapshot:
                    state = MarketDataState.Incrementals;
                    StateChanged?.Invoke(state);
                    break;
            }
        }
        public void RegisterStatistics(int type)
        {
            lock (Statistics)
            {
                ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(Statistics, type, out _);
                count++;
            }
        }

        public override void Callback(ref readonly AuctionImbalance_19Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(AuctionImbalance_19Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly ChannelReset_11Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(ChannelReset_11Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly ClosingPrice_17Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(ClosingPrice_17Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly DeleteOrder_MBO_51Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(DeleteOrder_MBO_51Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly EmptyBook_9Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(EmptyBook_9Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly ExecutionStatistics_56Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(ExecutionStatistics_56Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly ExecutionSummary_55Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(ExecutionSummary_55Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly ForwardTrade_54Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(ForwardTrade_54Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly HighPrice_24Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(HighPrice_24Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly LowPrice_25Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(LowPrice_25Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly MassDeleteOrders_MBO_52Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(MassDeleteOrders_MBO_52Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly News_5Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(News_5Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly OpeningPrice_15Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(OpeningPrice_15Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly Order_MBO_50Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(Order_MBO_50Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly PriceBand_22Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(PriceBand_22Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly QuantityBand_21Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(QuantityBand_21Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly SecurityGroupPhase_10Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(SecurityGroupPhase_10Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly SecurityStatus_3Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(SecurityStatus_3Data.MESSAGE_ID);
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
        public override void Callback(ref readonly SnapshotFullRefresh_Orders_MBO_71Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(SnapshotFullRefresh_Orders_MBO_71Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly TheoreticalOpeningPrice_16Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(TheoreticalOpeningPrice_16Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly TradeBust_57Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(TradeBust_57Data.MESSAGE_ID);
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly Trade_53Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(Trade_53Data.MESSAGE_ID);
            if (instrumentsById.TryGetValue(message.SecurityID.Value, out var instrument))
            {
                LastTradePrice lastTradePrice = instrument.LastTradePrice;
                lastTradePrice.MatchEventIndicator = message.MatchEventIndicator;
                lastTradePrice.TradingSessionID = message.TradingSessionID;
                lastTradePrice.TradeCondition = message.TradeCondition;
                lastTradePrice.MDEntryPx = message.MDEntryPx.Value;
                lastTradePrice.MDEntrySize = message.MDEntrySize.Value;
                lastTradePrice.TradeID = message.TradeID.Value;
                lastTradePrice.MDEntryBuyer = message.MDEntryBuyer.Value;
                lastTradePrice.MDEntrySeller = message.MDEntrySeller.Value;
                lastTradePrice.TradeDate = message.TradeDate.Date;
                lastTradePrice.MDEntryTimestamp = message.MDEntryTimestamp.Value;
                lastTradePrice.RptSeq = message.RptSeq.Value;
            }
        }

        internal void Init()
        {
            StateChanged?.Invoke(MarketDataState.None);
        }
    }
}
