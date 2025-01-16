using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;
namespace PcapSbePocConsole
{
    public class InstrumentDefinitionSyncExecutionState
    {
        private readonly MessageParser parser;
        private readonly IMarketDataConnectionProvider connectionProvider;
        
        private CyclicalSyncState state;
        private ChannelState? channelState;

        public InstrumentDefinitionSyncExecutionState(IMarketDataConnectionProvider connectionProvider)
        {
            this.parser = new MessageParser(ShouldConsume)
            {
                SequenceReset_1MessageReceived = SequenceReset_1MessageReceived,
                SecurityDefinition_4MessageReceived = SecurityDefinition_4MessageReceived,
                SecurityDefinition_12MessageReceived = SecurityDefinition_12MessageReceived
            };
            this.connectionProvider = connectionProvider;
        }

        public async Task<ChannelState> SyncAsync(byte channel)
        {
            var buffer = new byte[1024*2];
            this.channelState = new ChannelState();
            using (var connection = connectionProvider.ConnectInstrumentDefinition(channel))
            {
                connection.Connect();
                state = CyclicalSyncState.SeekingStart;
                while (state != CyclicalSyncState.Synced)
                {
                    int length = await connection.ReceiveAsync(buffer);
                    if(length != 0)
                        parser.Parse(buffer.AsSpan(0, length));
                }
            }
            return channelState;
        }

        private bool ShouldConsume(ref readonly PacketHeader packet, ReadOnlySpan<byte> data)
        {
            return true;
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

        public void SecurityDefinition_4MessageReceived(ref readonly SecurityDefinition_4Data message, ReadOnlySpan<byte> variablePart)
        {
            if (state != CyclicalSyncState.Syncing)
                return;

            channelState?.Add(InstrumentDefinition.Convert(message, variablePart));
        }
        private void SecurityDefinition_12MessageReceived(ref readonly SecurityDefinition_12Data message, ReadOnlySpan<byte> variablePart)
        {
            if (state != CyclicalSyncState.Syncing)
                return;

            channelState?.Add(InstrumentDefinition.Convert(message, variablePart));
        }
    }
}