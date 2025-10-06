using B3.Market.Data.Messages;
using PcapSbePocConsole.Configs;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace PcapSbePocConsole.Connection
{
    public class MarketDataMultipleConnection : IMarketDataConnection
    {
        private uint lastConsumed;
        private readonly UdpClient[] clients;
        private readonly AddressConfig[] configs;
        private readonly Task[] consumer;
        private readonly BlockingCollection<(byte[], int)> channel;

        public bool IsConnected => true;

        public MarketDataMultipleConnection(AddressConfig[] configs)
        {
            this.channel = new BlockingCollection<(byte[], int)>();
            this.configs = configs;
            clients = new UdpClient[configs.Length];
            consumer = new Task[configs.Length];
            for (int i = 0; i < configs.Length; i++)
            {
                clients[i] = new UdpClient(configs[i].MulticastEndpoint.Port);
                var index = i;
                consumer[i] = new Task(
                    () => Consume(clients[index], configs[index].MulticastEndpoint),
                    TaskCreationOptions.LongRunning);
            }
        }

        public void Dispose()
        {
            foreach (var client in clients)
                client.Dispose();
        }

        public int Receive(byte[] buffer)
        {
            if (channel.TryTake(out var data))
            {
                data.Item1.AsSpan(0, data.Item2).CopyTo(buffer);
                ArrayPool<byte>.Shared.Return(data.Item1);
                return data.Item2;
            }
            return 0;
        }

        private bool ShouldReturn(ReadOnlySpan<byte> data)
        {
            ref readonly PacketHeader packet = ref MemoryMarshal.AsRef<PacketHeader>(data);
            return packet.SequenceNumber == 0
                || Interlocked.CompareExchange(ref lastConsumed, packet.SequenceNumber, packet.SequenceNumber - 1) == packet.SequenceNumber - 1;
        }

        public void Connect()
        {
            for (int i = 0; i < configs.Length; i++)
            {
                var index = i;
                clients[index].JoinMulticastGroup(configs[index].MulticastEndpoint.Address);
                consumer[index].Start();
            }
        }

        private void Consume(UdpClient udpClient, EndPoint socketEndpoint)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(2048);
            while (IsConnected)
            {
                int length = udpClient.Client.Receive(buffer);
                if (length > 0)
                {
                    if (ShouldReturn(buffer.AsSpan(0, length)))
                    {
                        channel.Add((buffer, length));
                        buffer = ArrayPool<byte>.Shared.Rent(2048);
                    }
                }
            }
        }
    }
}