using B3.Market.Data.Messages;
using Microsoft.Extensions.ObjectPool;
using System.Runtime.InteropServices;

namespace PcapSbePocConsole.Models
{
    public record OrderBook
    {
        private static readonly ObjectPool<OrderBookEntry> orderBookEntryPool = ObjectPool.Create<OrderBookEntry>();
        private static readonly ObjectPool<AggregatedOrderBookEntry> aggregatedOrderBookEntryPool = ObjectPool.Create<AggregatedOrderBookEntry>();

        private static readonly IComparer<AggregatedOrderBookEntry> BidsComparer = new BidAggregatedOrderBookEntryComparer();
        private static readonly IComparer<AggregatedOrderBookEntry> OffersComparer = new OfferAggregatedOrderBookEntryComparer();
        public OrderBook(Security security, int capacity)
        {
            Security = security;
            Bids = new List<OrderBookEntry>(capacity);
            Offers = new List<OrderBookEntry>(capacity);
            AggregatedBids = new List<AggregatedOrderBookEntry>(capacity / 2);
            AggregatedOffers = new List<AggregatedOrderBookEntry>(capacity / 2);
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
            var entry = orderBookEntryPool.Get();
            entry.EnteringFirm = enteringFirm;
            entry.Timestamp = insertTimestamp;
            entry.Price = entryPx;
            entry.Quantity = entrySize;
            entries.Insert(position - 1, entry);
            AddAggregatedEntry(type, entryPx, entrySize);
        }

        private void AddAggregatedEntry(MDEntryType type, decimal? entryPx, long entrySize)
        {
            var aggregatedEntries = AggregatedEntriesByType(type);
            var aggregatedEntry = aggregatedOrderBookEntryPool.Get();
            aggregatedEntry.Quantity = entrySize;
            aggregatedEntry.Price = entryPx;
            var index = aggregatedEntries.BinarySearch(aggregatedEntry, AggregatedEntriesComparerByType(type));
            if (index < 0)
                aggregatedEntries.Insert(~index, aggregatedEntry);
            else
            {
                aggregatedEntries[index].Quantity += aggregatedEntry.Quantity;
                aggregatedOrderBookEntryPool.Return(aggregatedEntry);
            }
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
            var aggregatedEntry = aggregatedOrderBookEntryPool.Get();
            aggregatedEntry.Price = entryPx;
            var index = aggregatedEntries.BinarySearch(aggregatedEntry, AggregatedEntriesComparerByType(type));
            if (index >= 0)
                aggregatedEntries[index].Quantity += entrySize - originalQuantity;
            aggregatedOrderBookEntryPool.Return(aggregatedEntry);
        }

        public void Remove(MDEntryType type, int position)
        {
            var entries = EntriesByType(type);
            var entry = entries[position - 1];
            entries.RemoveAt(position - 1);
            RemoveAggregatedEntry(type, entry.Price, entry.Quantity);
            orderBookEntryPool.Return(entry);
        }

        private void RemoveAggregatedEntry(MDEntryType type, decimal? price, long quantity)
        {
            var aggregatedEntries = AggregatedEntriesByType(type);
            var aggregatedEntry = aggregatedOrderBookEntryPool.Get();
            aggregatedEntry.Price = price;
            var index = aggregatedEntries.BinarySearch(aggregatedEntry, AggregatedEntriesComparerByType(type));
            aggregatedOrderBookEntryPool.Return(aggregatedEntry);
            if (index >= 0)
            {
                var currentEntry = aggregatedEntries[index];
                currentEntry.Quantity -= quantity;
                if (currentEntry.Quantity == 0)
                {
                    var removedEntry = aggregatedEntries[index];
                    aggregatedEntries.RemoveAt(index);
                    aggregatedOrderBookEntryPool.Return(removedEntry);
                }
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
            var toBeRemovedEntries = CollectionsMarshal.AsSpan(entries).Slice(position - 1, entries.Count - position);
            RemoveAggregatedEntries(type, toBeRemovedEntries);
            foreach (var entry in toBeRemovedEntries)
                orderBookEntryPool.Return(entry);
            entries.RemoveRange(position - 1, entries.Count - position);
        }

        private void RemoveAggregatedEntries(MDEntryType type, ReadOnlySpan<OrderBookEntry> removedEntries)
        {
            var aggregatedEntries = AggregatedEntriesByType(type);
            var aggregatedEntry = aggregatedOrderBookEntryPool.Get();
            foreach (var entry in removedEntries)
            {
                aggregatedEntry.Price = entry.Price;
                aggregatedEntry.Quantity = entry.Quantity;

                var index = aggregatedEntries.BinarySearch(aggregatedEntry, AggregatedEntriesComparerByType(type));
                if (index >= 0)
                {
                    var currentEntry = aggregatedEntries[index];
                    currentEntry.Quantity -= aggregatedEntry.Quantity;
                    if (currentEntry.Quantity == 0)
                    {
                        aggregatedEntries.RemoveAt(index);
                        aggregatedOrderBookEntryPool.Return(currentEntry);
                    }
                }
            }
            aggregatedOrderBookEntryPool.Return(aggregatedEntry);
        }

        public void DeleteFrom(MDEntryType type, int position)
        {
            var entries = EntriesByType(type);
            var toBeRemovedEntries = CollectionsMarshal.AsSpan(entries).Slice(0, position);
            RemoveAggregatedEntries(type, toBeRemovedEntries);
            foreach (var entry in toBeRemovedEntries)
                orderBookEntryPool.Return(entry);
            entries.RemoveRange(0, position);
        }
    }
}