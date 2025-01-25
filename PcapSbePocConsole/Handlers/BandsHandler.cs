using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;

namespace PcapSbePocConsole.Handlers
{
    public static class BandsHandler
    {
        public static void Handle(this QuantityBand_21Data message, Bands bands)
        {
            bands.AvgDailyTradedQty = message.AvgDailyTradedQty.Value;
            bands.MaxTradeVol = message.MaxTradeVol.Value;
        }
        public static void Handle(this PriceBand_22Data message, Bands bands)
        {
            bands.PriceBandType = message.PriceBandType;
            bands.PriceLimitType = message.PriceLimitType;
            bands.PriceBandMidpointPriceType = message.PriceBandMidpointPriceType;
            bands.LowLimitPrice = message.LowLimitPrice.Value;
            bands.HighLimitPrice = message.HighLimitPrice.Value;
            bands.TradingReferencePrice = message.TradingReferencePrice.Value;
        }
    }
}