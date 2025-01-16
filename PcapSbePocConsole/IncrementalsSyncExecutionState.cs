using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;
using System.Numerics;
using System.Runtime.InteropServices;
namespace PcapSbePocConsole
{
    public class IncrementalsSyncExecutionState
    {
        private readonly MessageParser parser;
        private readonly IMarketDataConnectionProvider connectionProvider;
        private readonly List<byte[]> enqueuedMessages;
        
        private CyclicalSyncState state;
        private Dictionary<ulong, InstrumentDefinition> instrumentsById;
        private uint TotalNumberReports;

        public IncrementalsSyncExecutionState(IMarketDataConnectionProvider connectionProvider)
        {
            this.enqueuedMessages = new List<byte[]>();
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
                SnapshotFullRefresh_Orders_MBO_71MessageReceived = SnapshotFullRefresh_Orders_MBO_71MessageReceived,
            };
            this.connectionProvider = connectionProvider;
        }

        private void SequenceReset_1MessageReceived(ref readonly SequenceReset_1Data message, ReadOnlySpan<byte> variablePart)
        {
            switch (state)
            {
                case CyclicalSyncState.SeekingStart:
                    state = CyclicalSyncState.Syncing;
                    break;
                case CyclicalSyncState.Syncing:
                    state = CyclicalSyncState.Synced;
                    break;
            }
        }

        private void Sequence_2MessageReceived(ref readonly Sequence_2Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(Sequence_2Data));
        }
        private void SecurityStatus_3MessageReceived(ref readonly SecurityStatus_3Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(SecurityStatus_3Data));
        }

        private void SecurityGroupPhase_10MessageReceived(ref readonly SecurityGroupPhase_10Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(SecurityGroupPhase_10Data));
        }

        private void OpeningPrice_15MessageReceived(ref readonly OpeningPrice_15Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(OpeningPrice_15Data));
        }

        private void TheoreticalOpeningPrice_16MessageReceived(ref readonly TheoreticalOpeningPrice_16Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(TheoreticalOpeningPrice_16Data));
        }

        private void ClosingPrice_17MessageReceived(ref readonly ClosingPrice_17Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(ClosingPrice_17Data));
        }

        private void AuctionImbalance_19MessageReceived(ref readonly AuctionImbalance_19Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(AuctionImbalance_19Data));
        }

        private void SnapshotFullRefresh_Orders_MBO_71MessageReceived(ref readonly SnapshotFullRefresh_Orders_MBO_71Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(SnapshotFullRefresh_Orders_MBO_71Data));
        }

        private void QuantityBand_21MessageReceived(ref readonly QuantityBand_21Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(QuantityBand_21Data));
        }

        private void PriceBand_22MessageReceived(ref readonly PriceBand_22Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(PriceBand_22Data));
        }

        private void HighPrice_24MessageReceived(ref readonly HighPrice_24Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(HighPrice_24Data));
        }

        private void LowPrice_25MessageReceived(ref readonly LowPrice_25Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(LowPrice_25Data));
        }

        private void LastTradePrice_27MessageReceived(ref readonly LastTradePrice_27Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(LastTradePrice_27Data));
        }

        private void OpenInterest_29MessageReceived(ref readonly OpenInterest_29Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(OpenInterest_29Data));
        }

        private void SnapshotFullRefresh_Header_30MessageReceived(ref readonly SnapshotFullRefresh_Header_30Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(SnapshotFullRefresh_Header_30Data));
        }
        private void ExecutionStatistics_56MessageReceived(ref readonly ExecutionStatistics_56Data message, ReadOnlySpan<byte> variablePart)
        {
            Console.WriteLine(nameof(ExecutionStatistics_56Data));
        }


        public async Task Prepare(byte channel)
        {
            enqueuedMessages.Clear();
            var buffer = new byte[1024 * 2];
            using (var connection = connectionProvider.ConnectSnapshot(channel))
            {
                connection.Connect();
                state = CyclicalSyncState.SeekingStart;
                while (state != CyclicalSyncState.Synced)
                {
                    int length = await connection.ReceiveAsync(buffer);
                    parser.Parse(buffer.AsSpan(0, length));
                    if(state == CyclicalSyncState.Syncing)
                    {
                        enqueuedMessages.Add(buffer.AsSpan(0, length).ToArray());
                    }
                }
            }
        }

        public void Sync(Dictionary<ulong, InstrumentDefinition> instrumentsById)
        {
            this.instrumentsById = instrumentsById;
            foreach (var message in enqueuedMessages)
                parser.Parse(message);
        }

        private bool ShouldConsume(ref readonly PacketHeader packet, ReadOnlySpan<byte> data)
        {
            return true;
        }

    }
}