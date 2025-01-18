namespace PcapSbePocConsole.Models
{
    public record Security(Definition Definition)
    {
        public LastTradePrice LastTradePrice { get; } = new();
        public Status Status { get; } = new();
        public Phase Phase { get; } = new();
        public Summary Summary { get; } = new();
        public Bands Bands { get; } = new();
        public OpenInterest OpenInterest { get; } = new();
        public TheoreticalOpeningPrice TheoreticalOpeningPrice { get; } = new();
        public OrderBook OrderBook { get; } = new();
        public AuctionImbalance AuctionImbalance { get; } = new();
        public ExecutionStatistics ExecutionStatistics { get; } = new();

    }
}
