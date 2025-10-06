using B3.Market.Data.Messages;

namespace PcapSbePocConsole.Models
{
    public class AuctionImbalance(Security Security)
    {
        public MatchEventIndicator MatchEventIndicator { get; set; }
        public MDUpdateAction MDUpdateAction { get; set; }
        public ImbalanceCondition ImbalanceCondition { get; set; }
        public long? MDEntrySize { get; set; }
        public DateTime? MDEntryTimestamp { get; set; }
    }
}