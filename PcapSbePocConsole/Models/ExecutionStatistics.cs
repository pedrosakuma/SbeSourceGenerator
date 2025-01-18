using B3.Market.Data.Messages;

namespace PcapSbePocConsole.Models
{
    public class ExecutionStatistics
    {
        public MatchEventIndicator MatchEventIndicator { get; set; }
        public TradingSessionID TradingSessionID { get; set; }
        public DateOnly? TradeDate { get; set; }
        public long TradeVolume { get; set; }
        public decimal? VwapPx { get; set; }
        public decimal? NetChgPrevDay { get; set; }
        public long NumberOfTrades { get; set; }
        public DateTime? MDEntryTimestamp { get; set; }
    }
}