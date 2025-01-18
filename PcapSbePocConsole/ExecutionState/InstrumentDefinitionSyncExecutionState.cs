using B3.Market.Data.Messages;
using PcapSbePocConsole.Connection;
using PcapSbePocConsole.Models;
namespace PcapSbePocConsole
{
    public class InstrumentDefinitionSyncExecutionState
    {
        private readonly MessageParser parser;
        private readonly IMarketDataConnectionProvider connectionProvider;
        private List<byte[]> instruments;

        private CyclicalSyncState state;
        private ChannelState? channelState;

        public InstrumentDefinitionSyncExecutionState(IMarketDataConnectionProvider connectionProvider)
        {
            this.instruments = new List<byte[]>(16384);
            this.parser = new MessageParser(ShouldConsume)
            {
                SequenceReset_1MessageReceived = SequenceReset_1MessageReceived,
                SecurityDefinition_4MessageReceived = SecurityDefinition_4MessageReceived,
                SecurityDefinition_12MessageReceived = SecurityDefinition_12MessageReceived
            };
            this.connectionProvider = connectionProvider;
        }

        public ChannelState Sync(byte channel)
        {
            Console.WriteLine("InstrumentDefinition SeekingStart");
            var buffer = new byte[1024 * 2];
            this.channelState = new ChannelState();
            using (var connection = connectionProvider.ConnectInstrumentDefinition(channel))
            {
                connection.Connect();
                state = CyclicalSyncState.SeekingStart;
                while (state != CyclicalSyncState.Synced)
                {
                    int length = connection.Receive(buffer);
                    if (length != 0)
                        parser.Parse(buffer.AsSpan(0, length));
                }
            }
            foreach (var instrumentData in instruments)
                parser.Parse(instrumentData);
            return channelState;
        }

        private bool ShouldConsume(ref readonly PacketHeader packet, ReadOnlySpan<byte> data)
        {
            if (state == CyclicalSyncState.Syncing)
                instruments.Add(data.ToArray());
            return true;
        }

        private void SequenceReset_1MessageReceived(ref readonly SequenceReset_1Data message, ReadOnlySpan<byte> variablePart)
        {
            switch (state)
            {
                case CyclicalSyncState.SeekingStart:
                    Console.WriteLine("InstrumentDefinition Syncing");
                    state = CyclicalSyncState.Syncing;
                    break;
                case CyclicalSyncState.Syncing:
                    Console.WriteLine("InstrumentDefinition Synced");
                    state = CyclicalSyncState.Synced;
                    break;
            }
        }

        public void SecurityDefinition_4MessageReceived(ref readonly SecurityDefinition_4Data message, ReadOnlySpan<byte> variablePart)
        {
            if (state != CyclicalSyncState.Synced)
                return;

            channelState?.Add(Definition.Convert(message, variablePart));
        }
        private void SecurityDefinition_12MessageReceived(ref readonly SecurityDefinition_12Data message, ReadOnlySpan<byte> variablePart)
        {
            if (state != CyclicalSyncState.Synced)
                return;

            channelState?.Add(Definition.Convert(message, variablePart));
        }
    }
}