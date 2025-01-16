using PcapSbePocConsole.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B3.Market.Data.Messages;

namespace PcapSbePocConsole
{
    public partial class MarketDataHandler
    {
        public void Callback(ref readonly Trade_53Data message, ReadOnlySpan<byte> variablePart)
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
    }
}
