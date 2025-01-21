using B3.Market.Data.Messages;
namespace PcapSbePocConsole.Models
{
    public class Bands(Security Security)
    {
        public long? AvgDailyTradedQty { get; set; }
        public long? MaxTradeVol { get; set; }
        public PriceBandType? PriceBandType { get; set; }
        public PriceLimitType? PriceLimitType { get; set; }
        public PriceBandMidpointPriceType? PriceBandMidpointPriceType { get; set; }
        public decimal? LowLimitPrice { get; set; }
        public decimal? HighLimitPrice { get; set; }
        public decimal? TradingReferencePrice { get; set; }
    }
}