namespace PcapSbePocConsole.Models
{
    public class BidAggregatedOrderBookEntryComparer : IComparer<AggregatedOrderBookEntry>
    {
        public int Compare(AggregatedOrderBookEntry? x, AggregatedOrderBookEntry? y)
        {
            if (x?.Price == null && y?.Price == null)
            {
                return 0;
            }
            if (x?.Price == null)
            {
                return -1;
            }
            if (y?.Price == null)
            {
                return 1;
            }
            return y!.Price!.Value.CompareTo(x!.Price!.Value);
        }
    }
    public class OfferAggregatedOrderBookEntryComparer : IComparer<AggregatedOrderBookEntry>
    {
        public int Compare(AggregatedOrderBookEntry? x, AggregatedOrderBookEntry? y)
        {
            if (x?.Price == null && y?.Price == null)
            {
                return 0;
            }
            if (x?.Price == null)
            {
                return -1;
            }
            if (y?.Price == null)
            {
                return 1;
            }
            return x!.Price!.Value.CompareTo(y!.Price!.Value);
        }
    }
}
