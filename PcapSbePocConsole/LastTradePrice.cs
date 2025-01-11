using B3.Market.Data.Messages;

namespace PcapSbePocConsole
{
    public record LastTradePrice
    {
        public MatchEventIndicator MatchEventIndicator { get; set; }
        public TradingSessionID TradingSessionID { get; set; }
        public TradeCondition TradeCondition { get; set; }
        public decimal MDEntryPx { get; set; }
        public decimal MDEntrySize { get; set; }
        public uint TradeID { get; set; }
        public uint? MDEntryBuyer { get; set; }
        public uint? MDEntrySeller { get; set; }
        public DateOnly TradeDate { get; set; }
        public DateTime? MDEntryTimestamp { get; set; }
        public uint? RptSeq { get; set; }
    }
}