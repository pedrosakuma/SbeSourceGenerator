namespace PcapSbePocConsole.Models
{
    public record OrderBook
    {
        public List<OrderBookEntry> Bids { get; } = new();
        public List<OrderBookEntry> Offers { get; } = new();
    }
}