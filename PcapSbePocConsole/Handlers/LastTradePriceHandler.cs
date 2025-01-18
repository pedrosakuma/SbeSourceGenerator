using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;

namespace PcapSbePocConsole.Handlers
{
    public static class LastTradePriceHandler
    {
        public static void Handle(this Trade_53Data message, LastTradePrice lastTradePrice)
        {
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
        }
        public static void Handle(this LastTradePrice_27Data message, LastTradePrice lastTradePrice)
        {
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
        }

    }
}