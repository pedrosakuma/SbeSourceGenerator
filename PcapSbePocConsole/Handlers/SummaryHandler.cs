using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;

namespace PcapSbePocConsole.Handlers
{
    public static class SummaryHandler
    {
        public static void Handle(this OpeningPrice_15Data message, Summary summary)
        {
            summary.OpeningPrice = message.MDEntryPx.Value;
            summary.OpeningPriceNetChange = message.NetChgPrevDay?.Value;
            summary.OpeningTradeDate = message.TradeDate.Date;
        }
        public static void Handle(this ClosingPrice_17Data message, Summary summary)
        {
            summary.ClosingPrice = message.MDEntryPx.Value;
            summary.ClosingTradeDate = message.TradeDate.Date;
        }
        public static void Handle(this HighPrice_24Data message, Summary summary)
        {
            summary.HighPrice = message.MDEntryPx.Value;
            summary.HighTradeDate = message.TradeDate.Date;
        }
        public static void Handle(this LowPrice_25Data message, Summary summary)
        {
            summary.LowPrice = message.MDEntryPx.Value;
            summary.LowTradeDate = message.TradeDate.Date;
        }
    }
}