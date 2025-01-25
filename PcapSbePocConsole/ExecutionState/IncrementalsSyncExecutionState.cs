using B3.Market.Data.Messages;
using PcapSbePocConsole.Connection;
using PcapSbePocConsole.Handlers;
using PcapSbePocConsole.Models;
using System.Collections.Concurrent;

namespace PcapSbePocConsole
{
    public class IncrementalsSyncExecutionState
    {
        private readonly MessageParser parser;
        private readonly IMarketDataConnectionProvider connectionProvider;
        private readonly byte channel;
        private readonly Feeds feed;
        private readonly BlockingCollection<byte[]> enqueuedMessages;

        private ChannelState? channelState;
        private CyclicalSyncState state;
        private CancellationToken token;

        public IncrementalsSyncExecutionState(IMarketDataConnectionProvider connectionProvider, byte channel, Feeds feed)
        {
            this.enqueuedMessages = new BlockingCollection<byte[]>();
            this.parser = new MessageParser(ShouldConsume)
            {
                SequenceReset_1MessageReceived = SequenceReset_1MessageReceived,
                Sequence_2MessageReceived = Sequence_2MessageReceived,
                SecurityStatus_3MessageReceived = SecurityStatus_3MessageReceived,
                SecurityGroupPhase_10MessageReceived = SecurityGroupPhase_10MessageReceived,
                OpeningPrice_15MessageReceived = OpeningPrice_15MessageReceived,
                TheoreticalOpeningPrice_16MessageReceived = TheoreticalOpeningPrice_16MessageReceived,
                ClosingPrice_17MessageReceived = ClosingPrice_17MessageReceived,
                AuctionImbalance_19MessageReceived = AuctionImbalance_19MessageReceived,
                QuantityBand_21MessageReceived = QuantityBand_21MessageReceived,
                PriceBand_22MessageReceived = PriceBand_22MessageReceived,
                HighPrice_24MessageReceived = HighPrice_24MessageReceived,
                LowPrice_25MessageReceived = LowPrice_25MessageReceived,
                LastTradePrice_27MessageReceived = LastTradePrice_27MessageReceived,
                OpenInterest_29MessageReceived = OpenInterest_29MessageReceived,
                SnapshotFullRefresh_Header_30MessageReceived = SnapshotFullRefresh_Header_30MessageReceived,
                ExecutionStatistics_56MessageReceived = ExecutionStatistics_56MessageReceived,

                News_5MessageReceived = News_5MessageReceived,

                EmptyBook_9MessageReceived = EmptyBook_9MessageReceived,
                DeleteOrder_MBO_51MessageReceived = DeleteOrder_MBO_51MessageReceived,
                MassDeleteOrders_MBO_52MessageReceived = MassDeleteOrders_MBO_52MessageReceived,
                Order_MBO_50MessageReceived = Order_MBO_50MessageReceived,
                TradeBust_57MessageReceived = TradeBust_57MessageReceived,
                Trade_53MessageReceived = Trade_53MessageReceived,

            };
            this.connectionProvider = connectionProvider;
            this.channel = channel;
            this.feed = feed;
        }

        private void Trade_53MessageReceived(ref readonly Trade_53Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.LastTradePrice);
                LastTradeUpdated(security.LastTradePrice);
            }
        }

        private void TradeBust_57MessageReceived(ref readonly TradeBust_57Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(TradeBust_57Data));
        }
        private void LastTradeUpdated(LastTradePrice lastTrade)
        {
            //if (lastTrade.Security.Definition.Symbol == "WINJ24")
            //{
            //    Console.SetCursorPosition(0, 0);
            //    Console.WriteLine($"{lastTrade.Security.Definition.Symbol,-12} {lastTrade.MDEntryPx,-15} {lastTrade.MDEntrySize,-5} {lastTrade.MDEntryTimestamp,-15}");
            //}
        }
        //public Dictionary<ulong, int> orderBookStatistics = new Dictionary<ulong, int>();
        //int totalCount = 0;
        private void OrderBookUpdated(OrderBook orderBook, uint index)
        {
            //ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(orderBookStatistics, orderBook.Security.Definition.SecurityID, out _);
            //count++;
            //totalCount++;
            //if (totalCount % 100000 == 0)
            //{
            //    var position = this.channel switch {
            //        72 => 0,
            //        _ => 11,
            //    };
            //    Console.SetCursorPosition(0, position);
            //    foreach (var item in orderBookStatistics.OrderByDescending(v => v.Value).Take(10))
            //    {
            //        Console.WriteLine($"{channelState.InstrumentsById[item.Key].Definition.Symbol,-10}\t{item.Value}");
            //    }
            //}
            //return;
            //const int bookDepth = 10;
            //if (orderBook.Security.Definition.Symbol == "WINJ24")
            //{
            //    Console.SetCursorPosition(0, 1);
            //    Console.WriteLine($"{"Bid",17} {" Offer",-17}");
            //    Console.WriteLine($"{"Qty",5} {"Price ",12} {"Price",-12}{"Qty",-5}");
            //    if (orderBook.AggregatedBids.Count > bookDepth && orderBook.AggregatedOffers.Count > bookDepth)
            //    {
            //        for (int i = 0; i < bookDepth; i++)
            //        {
            //            var bid = orderBook.AggregatedBids[i];
            //            var offer = orderBook.AggregatedOffers[i];
            //            Console.WriteLine($"{bid.Quantity,5} {bid.Price,-12}{offer.Price,12} {offer.Quantity,-5}");
            //        }
            //    }
            //}
        }


        private void Order_MBO_50MessageReceived(ref readonly Order_MBO_50Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.OrderBook);
                OrderBookUpdated(security.OrderBook, message.MDEntryPositionNo.Value);
            }
        }

        private void MassDeleteOrders_MBO_52MessageReceived(ref readonly MassDeleteOrders_MBO_52Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.OrderBook);
                OrderBookUpdated(security.OrderBook, message.MDEntryPositionNo.Value);

            }
        }

        private void EmptyBook_9MessageReceived(ref readonly EmptyBook_9Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.OrderBook);
                OrderBookUpdated(security.OrderBook, 0);
            }
        }

        private void DeleteOrder_MBO_51MessageReceived(ref readonly DeleteOrder_MBO_51Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.OrderBook);
                OrderBookUpdated(security.OrderBook, message.MDEntryPositionNo.Value);
            }
        }

        private void News_5MessageReceived(ref readonly News_5Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(News_5Data));
        }

        private void SequenceReset_1MessageReceived(ref readonly SequenceReset_1Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(SequenceReset_1Data));
        }

        private void Sequence_2MessageReceived(ref readonly Sequence_2Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(Sequence_2Data));
        }
        private void SecurityStatus_3MessageReceived(ref readonly SecurityStatus_3Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.Status);
            }
        }

        private void SecurityGroupPhase_10MessageReceived(ref readonly SecurityGroupPhase_10Data message, ReadOnlySpan<byte> variablePart)
        {
            var securityGroup = message.SecurityGroup.ToString();
            if (channelState.InstrumentsBySecurityGroup.TryGetValue(securityGroup, out var instruments))
            {
                foreach (var security in instruments)
                {
                    message.Handle(security.Phase);
                }
            }
        }

        private void OpeningPrice_15MessageReceived(ref readonly OpeningPrice_15Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.Summary);
            }
        }

        private void TheoreticalOpeningPrice_16MessageReceived(ref readonly TheoreticalOpeningPrice_16Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.TheoreticalOpeningPrice);
            }
        }

        private void ClosingPrice_17MessageReceived(ref readonly ClosingPrice_17Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.Summary);
            }
        }

        private void AuctionImbalance_19MessageReceived(ref readonly AuctionImbalance_19Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.AuctionImbalance);
            }
        }

        private void QuantityBand_21MessageReceived(ref readonly QuantityBand_21Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.Bands);
            }
        }

        private void PriceBand_22MessageReceived(ref readonly PriceBand_22Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.Bands);
            }
        }

        private void HighPrice_24MessageReceived(ref readonly HighPrice_24Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.Summary);
            }
        }

        private void LowPrice_25MessageReceived(ref readonly LowPrice_25Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.Summary);
            }
        }

        private void LastTradePrice_27MessageReceived(ref readonly LastTradePrice_27Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.LastTradePrice);
                LastTradeUpdated(security.LastTradePrice);
            }
        }

        private void OpenInterest_29MessageReceived(ref readonly OpenInterest_29Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.OpenInterest);
            }
        }

        private void SnapshotFullRefresh_Header_30MessageReceived(ref readonly SnapshotFullRefresh_Header_30Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(SnapshotFullRefresh_Header_30Data));
        }
        private void ExecutionStatistics_56MessageReceived(ref readonly ExecutionStatistics_56Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                message.Handle(security.ExecutionStatistics);
            }
        }


        public Task Prepare(CancellationToken token)
        {
            this.token = token;
            return Task.Run(PrepareInternal, token);
        }

        private void PrepareInternal()
        {
            Console.WriteLine("Incrementals Starting");
            var buffer = new byte[1024 * 2];
            using (var connection = connectionProvider.ConnectIncrementals(channel, feed))
            {
                connection.Connect();
                state = CyclicalSyncState.SeekingStart;
                while (state != CyclicalSyncState.Synced)
                {
                    int length = connection.Receive(buffer);
                    if (length != 0)
                        enqueuedMessages.Add(buffer.AsSpan(0, length).ToArray());
                }
                Console.WriteLine("Incrementals Consuming enqueued");
                enqueuedMessages.CompleteAdding();
                foreach (var message in enqueuedMessages.GetConsumingEnumerable())
                    parser.Parse(message);
                Console.WriteLine("Incrementals Consumed enqueued");
                Console.Clear();
                while (!token.IsCancellationRequested)
                {
                    int length = connection.Receive(buffer);
                    if (length != 0)
                        parser.Parse(buffer.AsSpan(0, length));
                }
            }
        }

        public void Sync(ChannelState channelState)
        {
            this.channelState = channelState;
            while (enqueuedMessages.TryTake(out var message, 10))
                parser.Parse(message);
            state = CyclicalSyncState.Synced;
        }

        private bool ShouldConsume(ref readonly PacketHeader packet, ReadOnlySpan<byte> data)
        {
            if (packet.SequenceNumber <= channelState.LastSequence)
                return false;
            if (packet.SequenceNumber != channelState.LastSequence + 1)
                throw new InvalidOperationException(
                    $"packet.SequenceNumber: {packet.SequenceNumber}, channelState.LastSequence: {channelState.LastSequence}");
            channelState.LastSequence = packet.SequenceNumber;
            return true;
        }

    }
}