namespace PcapSbePocConsole.Models
{
    /// <summary>
    /// Compares two <see cref="AggregatedOrderBookEntry"/> instances by their price.
    /// Where the price is the key for the comparison and should be in descending order
    /// When price is null, should be on top of the list
    /// </summary>
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
    /// <summary>
    /// Compares two <see cref="AggregatedOrderBookEntry"/> instances by their price.
    /// Where the price is the key for the comparison and should be in ascending order
    /// When price is null, should be on top of the list
    /// </summary>
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
