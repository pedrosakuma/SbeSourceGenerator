
namespace PcapSbePocConsole.Models
{
    public class OrderBookEntry
    {
        public uint? EnteringFirm { get; internal set; }
        public DateTime? Timestamp { get; internal set; }
        public decimal? Price { get; internal set; }
        public long Quantity { get; internal set; }
    }
}