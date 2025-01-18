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
                var entries = orderBook.EntriesByType(entry.MDEntryType);
                entries.Insert((int)entry.MDEntryPositionNo.Value - 1,
                    new OrderBookEntry
                    {
                        EnteringFirm = entry.EnteringFirm.Value,
                        Timestamp = entry.MDInsertTimestamp.Value,
                        Price = entry.MDEntryPx.Value,
                        Quantity = entry.MDEntrySize.Value,
                    });
            });
        }
        public static void Handle(this DeleteOrder_MBO_51Data message, OrderBook orderBook)
        {
            var entries = orderBook.EntriesByType(message.MDEntryType);
            entries.RemoveAt((int)message.MDEntryPositionNo.Value - 1);
        }
        public static void Handle(this EmptyBook_9Data message, OrderBook orderBook)
        {
            orderBook.Offers.Clear();
            orderBook.Bids.Clear();
        }
        public static void Handle(this MassDeleteOrders_MBO_52Data message, OrderBook orderBook)
        {
            var entries = orderBook.EntriesByType(message.MDEntryType);
            switch (message.MDUpdateAction)
            {
                case MDUpdateAction.DELETE_THRU:
                    entries.RemoveRange((int)message.MDEntryPositionNo.Value - 1, entries.Count - (int)message.MDEntryPositionNo.Value);
                    break;
                case MDUpdateAction.DELETE_FROM:
                    entries.RemoveRange(0, (int)message.MDEntryPositionNo.Value);
                    break;
                default:
                    throw new ArgumentException("Not expected", nameof(message.MDUpdateAction));
            }
        }
        public static void Handle(this Order_MBO_50Data message, OrderBook orderBook)
        {
            var entries = orderBook.EntriesByType(message.MDEntryType);
            switch (message.MDUpdateAction)
            {
                case MDUpdateAction.NEW:
                    entries.Insert(
                        (int)message.MDEntryPositionNo.Value - 1,
                        new OrderBookEntry
                        {
                            Price = message.MDEntryPx?.Value,
                            Quantity = message.MDEntrySize.Value,
                            EnteringFirm = message.EnteringFirm.Value,
                            Timestamp = message.MDInsertTimestamp.Value
                        }
                    );
                    break;
                case MDUpdateAction.CHANGE:
                    var entry = entries[(int)message.MDEntryPositionNo.Value - 1];
                    entry.Price = message.MDEntryPx?.Value;
                    entry.Quantity = message.MDEntrySize.Value;
                    entry.EnteringFirm = message.EnteringFirm.Value;
                    entry.Timestamp = message.MDInsertTimestamp.Value;
                    break;
                case MDUpdateAction.DELETE:
                    entries.RemoveAt((int)message.MDEntryPositionNo.Value - 1);
                    break;
                case MDUpdateAction.OVERLAY:
                    break;
                default:
                    break;
            }
        }
    }
}