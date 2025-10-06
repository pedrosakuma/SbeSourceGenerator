using B3.Market.Data.Messages;

namespace PcapSbePocConsole.Models
{
    public record OpenInterest(Security Security)
    {
        public MatchEventIndicator MatchEventIndicator { get; set; }
        public DateOnly TradeDate { get; set; }
        public long MDEntrySize { get; set; }
        public DateTime? MDEntryTimestamp { get; set; }
    }
}