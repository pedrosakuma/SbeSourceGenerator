using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcapSbePocConsole.Handlers
{
    public static class StatusHandler
    {
        public static void Handle(this SecurityStatus_3Data message, Status status)
        {
            status.TradingSessionID = message.TradingSessionID;
            status.SecurityTradingStatus = message.SecurityTradingStatus;
            status.SecurityTradingEvent = message.SecurityTradingEvent;
            status.TradeDate = message.TradeDate.Date;
            status.TradSesOpenTime = message.TradSesOpenTime?.Value;
        }
    }
}
