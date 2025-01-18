using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;

namespace PcapSbePocConsole.Handlers
{
    public static class ExecutionStatisticsHandler
    {
        public static void Handle(this ExecutionStatistics_56Data message, ExecutionStatistics executionStatistics)
        {
            executionStatistics.MatchEventIndicator = message.MatchEventIndicator;
            executionStatistics.TradingSessionID = message.TradingSessionID;
            executionStatistics.TradeDate = message.TradeDate.Date;
            executionStatistics.TradeVolume = message.TradeVolume.Value;
            executionStatistics.VwapPx = message.VwapPx?.Value;
            executionStatistics.NetChgPrevDay = message.NetChgPrevDay?.Value;
            executionStatistics.NumberOfTrades = message.NumberOfTrades.Value;
            executionStatistics.MDEntryTimestamp = message.MDEntryTimestamp.Value;
        }
    }
}