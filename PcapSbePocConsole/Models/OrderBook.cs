using B3.Market.Data.Messages;

namespace PcapSbePocConsole.Models
{
    public record OrderBook
    {
        public OrderBook(Security security, int capacity)
        {
            Security = security;
            Bids = new List<OrderBookEntry>(capacity);
            Offers = new List<OrderBookEntry>(capacity);
        }

        public Security Security { get; }
        public List<OrderBookEntry> Bids { get; }
        public List<OrderBookEntry> Offers { get; }
        public List<OrderBookEntry> EntriesByType(MDEntryType type)
        {
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