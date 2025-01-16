using B3.Market.Data.Messages;
namespace PcapSbePocConsole.Models
{
    public record Phase
    {
        public TradingSessionID TradingSessionID { get; set; }
        public TradingSessionSubID TradingSessionSubID { get; set; }
        public SecurityTradingEvent? SecurityTradingEvent { get; set; }
        public DateOnly TradeDate { get; set; }
        public DateTime? TradSesOpenTime { get; set; }
    }
}