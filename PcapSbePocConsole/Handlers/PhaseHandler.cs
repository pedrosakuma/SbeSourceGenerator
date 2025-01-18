using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;
using System.Diagnostics;

namespace PcapSbePocConsole.Handlers
{
    public static class PhaseHandler
    {
        public static void Handle(this SecurityGroupPhase_10Data message, Phase phase)
        {
            phase.TradingSessionID = message.TradingSessionID;
            phase.TradingSessionSubID = message.TradingSessionSubID;
            phase.SecurityTradingEvent = message.SecurityTradingEvent;
            phase.TradeDate = message.TradeDate.Date;
            phase.TradSesOpenTime = message.TradSesOpenTime?.Value;
        }
    }
}