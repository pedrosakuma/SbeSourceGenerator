using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;

namespace PcapSbePocConsole.Handlers
{
    public static class OpenInterestHandler
    {
        public static void Handle(this OpenInterest_29Data message, OpenInterest openInterest)
        {
            openInterest.MatchEventIndicator = message.MatchEventIndicator;
            openInterest.TradeDate = message.TradeDate.Date;
            openInterest.MDEntrySize = message.MDEntrySize.Value;
            openInterest.MDEntryTimestamp = message.MDEntryTimestamp.Value;

        }
    }
}