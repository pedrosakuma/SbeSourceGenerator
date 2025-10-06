using B3.Market.Data.Messages;

namespace PcapSbePocConsole.Models
{
    public record Status(Security Security)
    {
        public TradingSessionID TradingSessionID { get; set; }
        public SecurityTradingStatus SecurityTradingStatus { get; set; }
        public SecurityTradingEvent? SecurityTradingEvent { get; set; }
        public DateOnly TradeDate { get; set; }
        public DateTime? TradSesOpenTime { get; set; }
    }
}