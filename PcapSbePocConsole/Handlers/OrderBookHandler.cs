using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;

namespace PcapSbePocConsole.Handlers
{
    public static class OrderBookHandler
    {
        public static void Handle(this SnapshotFullRefresh_Orders_MBO_71Data message, ReadOnlySpan<byte> variablePart, OrderBook orderBook)
        {
            message.ConsumeVariableLengthSegments(variablePart, entry =>
            {
                orderBook.Add(
                    entry.MDEntryType,
                    (int)entry.MDEntryPositionNo.Value,
                    entry.EnteringFirm.Value,
                    entry.MDInsertTimestamp.Value,
                    entry.MDEntryPx.Value,
                    entry.MDEntrySize.Value);
            });
        }
        public static int Handle(this DeleteOrder_MBO_51Data message, OrderBook orderBook)
        {
            return -orderBook.Remove(message.MDEntryType, (int)message.MDEntryPositionNo.Value);
        }
        public static void Handle(this EmptyBook_9Data message, OrderBook orderBook)
        {
            orderBook.Clear();
        }
        public static int Handle(this MassDeleteOrders_MBO_52Data message, OrderBook orderBook)
        {
            var entries = orderBook.EntriesByType(message.MDEntryType);
            return message.MDUpdateAction switch
            {
                MDUpdateAction.DELETE_THRU => -orderBook.DeleteThru(message.MDEntryType, (int)message.MDEntryPositionNo.Value),
                MDUpdateAction.DELETE_FROM => -orderBook.DeleteFrom(message.MDEntryType, (int)message.MDEntryPositionNo.Value),
                _ => throw new ArgumentException("Not expected", nameof(message.MDUpdateAction)),
            };
        }
        public static int Handle(this Order_MBO_50Data message, OrderBook orderBook)
        {
            var entries = orderBook.EntriesByType(message.MDEntryType);
            return message.MDUpdateAction switch
            {
                MDUpdateAction.NEW => -orderBook.Add(
                    message.MDEntryType,
                    (int)message.MDEntryPositionNo.Value,
                    message.EnteringFirm.Value,
                    message.MDInsertTimestamp.Value,
                    message.MDEntryPx.Value,
                    message.MDEntrySize.Value
                ),
                MDUpdateAction.CHANGE => orderBook.Update(
                    message.MDEntryType,
                    (int)message.MDEntryPositionNo.Value,
                    message.EnteringFirm.Value,
                    message.MDInsertTimestamp.Value,
                    message.MDEntryPx.Value,
                    message.MDEntrySize.Value
                ),
                MDUpdateAction.DELETE => -orderBook.Remove(
                    message.MDEntryType,
                    (int)message.MDEntryPositionNo.Value
                ),
                _ => 0,
            };
        }
    }
}