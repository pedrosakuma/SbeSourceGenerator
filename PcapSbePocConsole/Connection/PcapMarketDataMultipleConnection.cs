using B3.Market.Data.Messages;
using PcapSbePocConsole.Configs;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace PcapSbePocConsole.Connection
{
    public class PcapMarketDataMultipleConnection : IMarketDataConnection
    {
        private uint lastConsumed;
        private readonly PcapReplayer[] replayers;
        private readonly UdpClient[] clients;
        private readonly AddressConfig[] configs;
        private readonly Task[] consumer;
        private readonly Channel<(IMemoryOwner<byte>, int)> channel;

        public bool IsConnected
        {
            get
            {
                foreach (var replayer in replayers)
                {
                    if (!replayer.Connected)
                        return false;
                }
                return true;
            }
        }

        public PcapMarketDataMultipleConnection(AddressConfig[] configs)
        {
            this.channel = Channel.CreateUnbounded<(IMemoryOwner<byte>, int)>(
                new UnboundedChannelOptions { 
                    SingleReader = true
                }
            );
            this.configs = configs;
            replayers = new PcapReplayer[configs.Length];
            clients = new UdpClient[configs.Length];
            consumer = new Task[configs.Length];
            for (int i = 0; i < configs.Length; i++)
            {
                replayers[i] = new PcapReplayer(configs[i]);
                clients[i] = new UdpClient(configs[i].MulticastEndpoint.Port);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < replayers.Length; i++)
            {
                replayers[i].Dispose();
                clients[i].Dispose();
            }
        }

        public int Receive(byte[] buffer)
        {
            if(channel.Reader.TryRead(out var data))
            {
                data.Item1.Memory.Span.Slice(0, data.Item2).CopyTo(buffer);
                data.Item1.Dispose();
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
                consumer[index] = Task.Run(() => Consume(clients[index], configs[index].MulticastEndpoint));
                replayers[index].Start();
            }
        }

        private void Consume(UdpClient udpClient, EndPoint socketEndpoint)
        {
            var memory = MemoryPool<byte>.Shared.Rent(2048);
            while (IsConnected)
            {
                int length = udpClient.Client.ReceiveFrom(memory.Memory.Span, ref socketEndpoint);
                if (length > 0)
                {
                    if (ShouldReturn(memory.Memory.Span.Slice(0, length)))
                    { 
                        channel.Writer.TryWrite((memory, length));
                        memory = MemoryPool<byte>.Shared.Rent(2048);
                    }
                }
            }
        }
    }
}