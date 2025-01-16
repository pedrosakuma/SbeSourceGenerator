using B3.Market.Data.Messages;

namespace PcapSbePocConsole.Models
{
    public record OrderBook
    {
        public List<OrderBookEntry> Bids { get; } = new();
        public List<OrderBookEntry> Offers { get; } = new();
        public List<OrderBookEntry> EntriesByType(MDEntryType type) {
            switch (type)
            {
                case MDEntryType.BID:
                    return Bids;
                case MDEntryType.OFFER:
                    return Offers;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}