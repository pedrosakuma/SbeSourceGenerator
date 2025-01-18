using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;

namespace PcapSbePocConsole.Handlers
{
    public static class TheoreticalOpeningPriceHandler
    {
        public static void Handle(this TheoreticalOpeningPrice_16Data message, TheoreticalOpeningPrice theoreticalOpeningPrice)
        {
            theoreticalOpeningPrice.MatchEventIndicator = message.MatchEventIndicator;
            theoreticalOpeningPrice.MDUpdateAction = message.MDUpdateAction;
            theoreticalOpeningPrice.TradeDate = message.TradeDate.Date;
            theoreticalOpeningPrice.MDEntryPx = message.MDEntryPx?.Value;
            theoreticalOpeningPrice.MDEntrySize = message.MDEntrySize.Value;
            theoreticalOpeningPrice.MDEntryTimestamp = message.MDEntryTimestamp.Value;
        }
    }
}