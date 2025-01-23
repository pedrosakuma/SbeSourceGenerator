using B3.Market.Data.Messages;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PcapSbePocConsole.Models
{
    public record OrderBook
    {
        private static readonly IComparer<AggregatedOrderBookEntry> BidsComparer = new BidAggregatedOrderBookEntryComparer();
        private static readonly IComparer<AggregatedOrderBookEntry> OffersComparer = new OfferAggregatedOrderBookEntryComparer();
        public OrderBook(Security security, int capacity)
        {
            Security = security;
            Bids = new List<OrderBookEntry>(capacity);
            Offers = new List<OrderBookEntry>(capacity);
            AggregatedBids = new List<AggregatedOrderBookEntry>(capacity);
            AggregatedOffers = new List<AggregatedOrderBookEntry>(capacity);
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
        public List<AggregatedOrderBookEntry> AggregatedBids { get; }
        public List<AggregatedOrderBookEntry> AggregatedOffers { get; }
        public List<AggregatedOrderBookEntry> AggregatedEntriesByType(MDEntryType type)
        {
            switch (type)
            {
                case MDEntryType.BID:
                    return AggregatedBids;
                case MDEntryType.OFFER:
                    return AggregatedOffers;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        public IComparer<AggregatedOrderBookEntry> AggregatedEntriesComparerByType(MDEntryType type)
        {
            switch (type)
            {
                case MDEntryType.BID:
                    return BidsComparer;
                case MDEntryType.OFFER:
                    return OffersComparer;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public void Add(MDEntryType type, int position, uint? enteringFirm, DateTime? insertTimestamp, decimal? entryPx, long entrySize)
        {
            var entries = EntriesByType(type);
            entries.Insert(position - 1,
                new OrderBookEntry
                {
                    EnteringFirm = enteringFirm,
                    Timestamp = insertTimestamp,
                    Price = entryPx,
                    Quantity = entrySize,
                });
            AddAggregatedEntry(type, entryPx, entrySize);
        }

        private void AddAggregatedEntry(MDEntryType type, decimal? entryPx, long entrySize)
        {
            var aggregatedEntries = AggregatedEntriesByType(type);
            var aggregatedEntry = new AggregatedOrderBookEntry
            {
                Quantity = entrySize,
                Price = entryPx
            };
            var index = aggregatedEntries.BinarySearch(aggregatedEntry, AggregatedEntriesComparerByType(type));
            if (index < 0)
                aggregatedEntries.Insert(~index, aggregatedEntry);
            else
                aggregatedEntries[index].Quantity += aggregatedEntry.Quantity;
        }

        public void Update(MDEntryType type, int position, uint? enteringFirm, DateTime? insertTimestamp, decimal? entryPx, long entrySize)
        {
            var entries = EntriesByType(type);
            var entry = entries[position - 1];
            var originalQuantity = entry.Quantity;
            entry.Price = entryPx;
            entry.Quantity = entrySize;
            entry.EnteringFirm = enteringFirm;
            entry.Timestamp = insertTimestamp;
            
            UpdateAggregatedEntry(type, entryPx, entrySize, originalQuantity);
        }

        private void UpdateAggregatedEntry(MDEntryType type, decimal? entryPx, long entrySize, long originalQuantity)
        {
            var aggregatedEntries = AggregatedEntriesByType(type);
            var aggregatedEntry = new AggregatedOrderBookEntry
            {
                Price = entryPx
            };
            var index = aggregatedEntries.BinarySearch(aggregatedEntry, AggregatedEntriesComparerByType(type));
            if (index >= 0)
                aggregatedEntries[index].Quantity += entrySize - originalQuantity;
        }

        public void Remove(MDEntryType type, int position)
        {
            var entries = EntriesByType(type);
            var entry = entries[position - 1];
            entries.RemoveAt(position - 1);
            RemoveAggregatedEntry(type, entry.Price, entry.Quantity);
        }

        private void RemoveAggregatedEntry(MDEntryType type, decimal? price, long quantity)
        {
            var aggregatedEntries = AggregatedEntriesByType(type);
            var aggregatedEntry = new AggregatedOrderBookEntry
            {
                Price = price
            };
            var index = aggregatedEntries.BinarySearch(aggregatedEntry, AggregatedEntriesComparerByType(type));
            if (index >= 0)
            {
                var currentEntry = aggregatedEntries[index];
                currentEntry.Quantity -= quantity;
                if (currentEntry.Quantity == 0)
                    aggregatedEntries.RemoveAt(index);
            }
        }

        public void Clear()
        {
            Offers.Clear();
            Bids.Clear();
            AggregatedOffers.Clear();
            AggregatedBids.Clear();
        }

        public void DeleteThru(MDEntryType type, int position)
        {
            var entries = EntriesByType(type);
            RemoveAggregatedEntries(type, CollectionsMarshal.AsSpan(entries).Slice(position - 1, entries.Count - position));
            entries.RemoveRange(position - 1, entries.Count - position);
        }

        private void RemoveAggregatedEntries(MDEntryType type, ReadOnlySpan<OrderBookEntry> removedEntries)
        {
            var aggregatedEntries = AggregatedEntriesByType(type);
            var aggregatedEntry = new AggregatedOrderBookEntry();
            foreach (var entry in removedEntries)
            {
                aggregatedEntry.Price = entry.Price;
                aggregatedEntry.Quantity= entry.Quantity;

                var index = aggregatedEntries.BinarySearch(aggregatedEntry, AggregatedEntriesComparerByType(type));
                if (index >= 0)
                {
                    var currentEntry = aggregatedEntries[index];
                    currentEntry.Quantity -= aggregatedEntry.Quantity;
                    if(currentEntry.Quantity == 0)
                        aggregatedEntries.RemoveAt(index);
                }
            }
        }

        public void DeleteFrom(MDEntryType type, int position)
        {
            var entries = EntriesByType(type);
            RemoveAggregatedEntries(type, CollectionsMarshal.AsSpan(entries).Slice(0, position));
            entries.RemoveRange(0, position);
        }
    }
}