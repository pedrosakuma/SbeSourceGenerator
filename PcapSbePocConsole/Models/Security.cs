namespace PcapSbePocConsole.Models
{
    public record Security
    {
        public Security(Definition definition, int orderBookCapacity)
        {
            Definition = definition;
            LastTradePrice = new LastTradePrice(this);
            Status = new Status(this);
            Phase = new Phase(this);
            Summary = new Summary(this);
            Bands = new Bands(this);
            OpenInterest = new OpenInterest(this);
            TheoreticalOpeningPrice = new TheoreticalOpeningPrice(this);
            OrderBook = new OrderBook(this, orderBookCapacity);
            AuctionImbalance = new AuctionImbalance(this);
            ExecutionStatistics = new ExecutionStatistics(this);
        }
        public Definition Definition{ get; }
        public LastTradePrice LastTradePrice { get; }
        public Status Status { get; } 
        public Phase Phase { get; }
        public Summary Summary { get; }
        public Bands Bands { get; }
        public OpenInterest OpenInterest { get; }
        public TheoreticalOpeningPrice TheoreticalOpeningPrice { get; }
        public OrderBook OrderBook { get; }
        public AuctionImbalance AuctionImbalance { get; }
        public ExecutionStatistics ExecutionStatistics { get; }

    }
}
