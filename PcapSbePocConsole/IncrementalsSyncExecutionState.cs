using B3.Market.Data.Messages;
using System.Collections.Concurrent;
using PcapSbePocConsole.Models;

namespace PcapSbePocConsole
{
    public class IncrementalsSyncExecutionState
    {
        private readonly MessageParser parser;
        private readonly IMarketDataConnectionProvider connectionProvider;
        private readonly Feeds feed;
        private readonly BlockingCollection<byte[]> enqueuedMessages;
        
        private ChannelState channelState;

        public IncrementalsSyncExecutionState(IMarketDataConnectionProvider connectionProvider, Feeds feed)
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
            this.feed = feed;
        }

        private void Trade_53MessageReceived(ref readonly Trade_53Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                var trade = security.LastTradePrice;
                trade.MatchEventIndicator = message.MatchEventIndicator;
                trade.TradingSessionID = message.TradingSessionID;
                trade.TradeCondition = message.TradeCondition;
                trade.MDEntryPx = message.MDEntryPx.Value;
                trade.MDEntrySize = message.MDEntrySize.Value;
                trade.TradeID = message.TradeID.Value;
                trade.MDEntryBuyer = message.MDEntryBuyer.Value;
                trade.MDEntrySeller = message.MDEntrySeller.Value;
                trade.TradeDate = message.TradeDate.Date;
                trade.MDEntryTimestamp = message.MDEntryTimestamp.Value;
            }
        }

        private void TradeBust_57MessageReceived(ref readonly TradeBust_57Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(TradeBust_57Data));
        }
        private void OrderBookUpdated(InstrumentDefinition security, OrderBook orderBook)
        {
            if (security.Symbol == "CSNA3")
            {
                Console.SetCursorPosition(0,0);
                if (orderBook.Bids.Count > 20 && orderBook.Offers.Count > 20)
                { 
                for (int i = 0; i < 20; i++)
                {
                    var bid = orderBook.Bids[i];
                    var offer = orderBook.Offers[i];
                    Console.WriteLine($"{bid.EnteringFirm}\t{bid.Quantity}\t{bid.Price}\t\t{offer.Price}\t{offer.Quantity}\t{offer.EnteringFirm}");
                }
                }
            }
        }


        private void Order_MBO_50MessageReceived(ref readonly Order_MBO_50Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                var entries = security.OrderBook.EntriesByType(message.MDEntryType);
                switch (message.MDUpdateAction)
                {
                    case MDUpdateAction.NEW:
                        entries.Insert(
                            (int)message.MDEntryPositionNo.Value - 1, 
                            new OrderBookEntry
                            {
                                Price = message.MDEntryPx?.Value,
                                Quantity = message.MDEntrySize.Value,
                                EnteringFirm = message.EnteringFirm.Value,
                                Timestamp = message.MDInsertTimestamp.Value
                            }
                        );
                        break;
                    case MDUpdateAction.CHANGE:
                        var entry = entries[(int)message.MDEntryPositionNo.Value - 1];
                        entry.Price = message.MDEntryPx?.Value;
                        entry.Quantity = message.MDEntrySize.Value;
                        entry.EnteringFirm = message.EnteringFirm.Value;
                        entry.Timestamp = message.MDInsertTimestamp.Value;
                        break;
                    case MDUpdateAction.DELETE:
                        entries.RemoveAt((int)message.MDEntryPositionNo.Value - 1);
                        break;
                    case MDUpdateAction.OVERLAY:
                        break;
                    default:
                        break;
                }
                OrderBookUpdated(security, security.OrderBook);
            }
        }

        private void MassDeleteOrders_MBO_52MessageReceived(ref readonly MassDeleteOrders_MBO_52Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                var entries = security.OrderBook.EntriesByType(message.MDEntryType);
                switch (message.MDUpdateAction)
                {
                    case MDUpdateAction.DELETE_THRU:
                        entries.RemoveRange((int)message.MDEntryPositionNo.Value - 1, entries.Count - (int)message.MDEntryPositionNo.Value);
                        break;
                    case MDUpdateAction.DELETE_FROM:
                        entries.RemoveRange(0, (int)message.MDEntryPositionNo.Value);
                        break;
                    default:
                        throw new ArgumentException("Not expected", nameof(message.MDUpdateAction));
                }
                OrderBookUpdated(security, security.OrderBook);

            }
        }

        private void EmptyBook_9MessageReceived(ref readonly EmptyBook_9Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                var book = security.OrderBook;
                book.Offers.Clear();
                book.Bids.Clear();
                OrderBookUpdated(security, security.OrderBook);

            }
        }

        private void DeleteOrder_MBO_51MessageReceived(ref readonly DeleteOrder_MBO_51Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                var entries = security.OrderBook.EntriesByType(message.MDEntryType);
                entries.RemoveAt((int)message.MDEntryPositionNo.Value - 1);
                OrderBookUpdated(security, security.OrderBook);
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
                var status = security.Status;
                status.TradingSessionID = message.TradingSessionID;
                status.SecurityTradingStatus = message.SecurityTradingStatus;
                status.SecurityTradingEvent = message.SecurityTradingEvent;
                status.TradeDate = message.TradeDate.Date;
                status.TradSesOpenTime = message.TradSesOpenTime?.Value;
            }
        }

        private void SecurityGroupPhase_10MessageReceived(ref readonly SecurityGroupPhase_10Data message, ReadOnlySpan<byte> variablePart)
        {
            var securityGroup = message.SecurityGroup.ToString();
            if (channelState.InstrumentsBySecurityGroup.TryGetValue(securityGroup, out var instruments))
            {
                foreach (var security in instruments)
                {
                    var phase = security.Phase;
                    phase.TradingSessionID = message.TradingSessionID;
                    phase.TradingSessionSubID = message.TradingSessionSubID;
                    phase.SecurityTradingEvent = message.SecurityTradingEvent;
                    phase.TradeDate = message.TradeDate.Date;
                    phase.TradSesOpenTime = message.TradSesOpenTime?.Value;
                }
            }
        }

        private void OpeningPrice_15MessageReceived(ref readonly OpeningPrice_15Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                var summary = security.Summary;
                summary.OpeningPrice = message.MDEntryPx.Value;
                summary.OpeningPriceNetChange = message.NetChgPrevDay?.Value;
                summary.OpeningTradeDate = message.TradeDate.Date;
            }
        }

        private void TheoreticalOpeningPrice_16MessageReceived(ref readonly TheoreticalOpeningPrice_16Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                var theoreticalOpeningPrice = security.TheoreticalOpeningPrice;
                theoreticalOpeningPrice.MatchEventIndicator = message.MatchEventIndicator;
                theoreticalOpeningPrice.MDUpdateAction = message.MDUpdateAction;
                theoreticalOpeningPrice.TradeDate = message.TradeDate.Date;
                theoreticalOpeningPrice.MDEntryPx = message.MDEntryPx?.Value;
                theoreticalOpeningPrice.MDEntrySize = message.MDEntrySize.Value;
                theoreticalOpeningPrice.MDEntryTimestamp = message.MDEntryTimestamp.Value;
            }
        }

        private void ClosingPrice_17MessageReceived(ref readonly ClosingPrice_17Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                var summary = security.Summary;
                summary.ClosingPrice = message.MDEntryPx.Value;
                summary.ClosingTradeDate = message.TradeDate.Date;

            }
        }

        private void AuctionImbalance_19MessageReceived(ref readonly AuctionImbalance_19Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var instrument))
            {
                var auctionImbalance = instrument.AuctionImbalance;
                auctionImbalance.MatchEventIndicator = message.MatchEventIndicator;
                auctionImbalance.MDUpdateAction = message.MDUpdateAction;
                auctionImbalance.ImbalanceCondition = message.ImbalanceCondition;
                auctionImbalance.MDEntrySize = message.MDEntrySize.Value;
                auctionImbalance.MDEntryTimestamp = message.MDEntryTimestamp.Value;
            }
        }

        private void QuantityBand_21MessageReceived(ref readonly QuantityBand_21Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var instrument))
            {
                var bands = instrument.Bands;
                bands.AvgDailyTradedQty = message.AvgDailyTradedQty.Value;
                bands.MaxTradeVol = message.MaxTradeVol.Value;
            }
        }

        private void PriceBand_22MessageReceived(ref readonly PriceBand_22Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var instrument))
            {
                var bands = instrument.Bands;
                bands.PriceBandType = message.PriceBandType;
                bands.PriceLimitType = message.PriceLimitType;
                bands.PriceBandMidpointPriceType = message.PriceBandMidpointPriceType;
                bands.LowLimitPrice = message.LowLimitPrice?.Value;
                bands.HighLimitPrice = message.HighLimitPrice?.Value;
                bands.TradingReferencePrice = message.TradingReferencePrice?.Value;
            }
        }

        private void HighPrice_24MessageReceived(ref readonly HighPrice_24Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                var summary = security.Summary;
                summary.HighPrice = message.MDEntryPx.Value;
                summary.HighTradeDate = message.TradeDate.Date;
            }
        }

        private void LowPrice_25MessageReceived(ref readonly LowPrice_25Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var security))
            {
                var summary = security.Summary;
                summary.LowPrice = message.MDEntryPx.Value;
                summary.LowTradeDate = message.TradeDate.Date;
            }
        }

        private void LastTradePrice_27MessageReceived(ref readonly LastTradePrice_27Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(LastTradePrice_27Data));
        }

        private void OpenInterest_29MessageReceived(ref readonly OpenInterest_29Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var instrument))
            {
                var openInterest = instrument.OpenInterest;
                openInterest.MatchEventIndicator = message.MatchEventIndicator;
                openInterest.TradeDate = message.TradeDate.Date;
                openInterest.MDEntrySize = message.MDEntrySize.Value;
                openInterest.MDEntryTimestamp = message.MDEntryTimestamp.Value;
            }
        }

        private void SnapshotFullRefresh_Header_30MessageReceived(ref readonly SnapshotFullRefresh_Header_30Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(SnapshotFullRefresh_Header_30Data));
        }
        private void ExecutionStatistics_56MessageReceived(ref readonly ExecutionStatistics_56Data message, ReadOnlySpan<byte> variablePart)
        {
            if (channelState.InstrumentsById.TryGetValue(message.SecurityID.Value, out var instrument))
            {
                var executionStatistics = instrument.ExecutionStatistics;
                executionStatistics.MatchEventIndicator = message.MatchEventIndicator;
                executionStatistics.TradingSessionID = message.TradingSessionID;
                executionStatistics.TradeDate = message.TradeDate.Date;
                executionStatistics.TradeVolume = message.TradeVolume.Value;
                executionStatistics.VwapPx = message.VwapPx?.Value;
                executionStatistics.NetChgPrevDay = message.NetChgPrevDay?.Value;
                executionStatistics.NumberOfTrades = message.NumberOfTrades.Value;
                executionStatistics.MDEntryTimestamp = message.MDEntryTimestamp.Value;
            }
        }


        public async Task PrepareAsync(byte channel)
        {
            var buffer = new byte[1024 * 2];
            using (var connection = connectionProvider.ConnectIncrementals(channel, feed))
            {
                connection.Connect();
                while (connection.IsConnected)
                {
                    int length = await connection.ReceiveAsync(buffer);
                    if (length != 0)
                        enqueuedMessages.Add(buffer.AsSpan(0, length).ToArray());
                }
            }
        }

        public void Sync(ChannelState channelState)
        {
            this.channelState = channelState;
            foreach (var message in enqueuedMessages.GetConsumingEnumerable())
                parser.Parse(message);
        }

        private bool ShouldConsume(ref readonly PacketHeader packet, ReadOnlySpan<byte> data)
        {
            if (packet.SequenceNumber <= channelState.LastSequence)
                return false;
            if (packet.SequenceNumber != channelState.LastSequence + 1)
                throw new InvalidOperationException();
            channelState.LastSequence = packet.SequenceNumber;
            return true;
        }

    }
}