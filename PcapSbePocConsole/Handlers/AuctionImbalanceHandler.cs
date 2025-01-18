using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;

namespace PcapSbePocConsole.Handlers
{
    public static class AuctionImbalanceHandler
    {
        public static void Handle(this AuctionImbalance_19Data message, AuctionImbalance auctionImbalance)
        {
            auctionImbalance.MatchEventIndicator = message.MatchEventIndicator;
            auctionImbalance.MDUpdateAction = message.MDUpdateAction;
            auctionImbalance.ImbalanceCondition = message.ImbalanceCondition;
            auctionImbalance.MDEntrySize = message.MDEntrySize.Value;
            auctionImbalance.MDEntryTimestamp = message.MDEntryTimestamp.Value;
        }
    }
}