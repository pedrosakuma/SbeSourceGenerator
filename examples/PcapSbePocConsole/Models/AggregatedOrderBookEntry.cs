namespace PcapSbePocConsole.Models
{
    public class AggregatedOrderBookEntry
    {
        public decimal? Price { get; internal set; }
        public long Quantity { get; internal set; }
    }
}
