using B3.Market.Data.Messages;

namespace PcapSbePocConsole.Models
{
    public class TheoreticalOpeningPrice(Security Security)
    {
        public MatchEventIndicator MatchEventIndicator { get; set; }
        public MDUpdateAction MDUpdateAction { get; set; }
        public DateOnly? TradeDate { get; set; }
        public decimal? MDEntryPx { get; set; }
        public long? MDEntrySize { get; set; }
        public DateTime? MDEntryTimestamp { get; set; }
    }
}