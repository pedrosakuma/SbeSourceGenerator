using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Threading.Channels;

namespace PcapSbePocConsole
{
    internal class PcapMarketDataConnectionProvider : IMarketDataConnectionProvider
    {
        private readonly MarketConfig config;
        private readonly DateTime readFrom;

        public PcapMarketDataConnectionProvider(MarketConfig config, DateTime readFrom)
        {
            this.config = config;
            this.readFrom = readFrom;
        }

        public IMarketDataConnection ConnectIncrementals(byte channel, Feeds feeds)
        {
            throw new NotImplementedException();
        }

        public IMarketDataConnection ConnectInstrumentDefinition(byte channel)
        {
            if (!this.config.Channels.TryGetValue(channel, out var c))
                throw new ArgumentException("Channel not found");
            return new PcapMarketDataConnection(c.InstrumentDefinition.Address, 1024, readFrom);
        }

        public IMarketDataConnection ConnectSnapshot(byte channel)
        {
            if (!this.config.Channels.TryGetValue(channel, out var c))
                throw new ArgumentException("Channel not found");
            return new PcapMarketDataConnection(c.Snapshot.Address, 1024, readFrom);
        }
    }
    public class PcapMarketDataConnection : IMarketDataConnection
    {
        private readonly Channel<byte[]> queue;
        private readonly CaptureFileReaderDevice device;

        public bool IsConnected => !queue.Reader.Completion.IsCompleted;

        public PcapMarketDataConnection(string file, int capacity, DateTime readFrom)
        {
            this.readFrom = readFrom;

            queue = Channel.CreateUnbounded<byte[]>();
            device = new CaptureFileReaderDevice(file);
            device.Open(new DeviceConfiguration
            {
                BufferSize = 524288
            });
            device.OnPacketArrival += Device_OnPacketArrival;
            device.OnCaptureStopped += Device_OnCaptureStopped;
        }

        private void Device_OnCaptureStopped(object sender, CaptureStoppedEventStatus status)
        {
            queue.Writer.Complete();
        }

        private readonly DateTime readFrom;
        private void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            if (e.Header.Timeval.Date > readFrom)
            {
                var p = e.GetPacket().GetPacket();
                var udp = p.Extract<UdpPacket>();
                queue.Writer.TryWrite(udp.PayloadData);
            }
        }
 
        public void Dispose()
        {
            device.Dispose();
        }

        public async Task<int> ReceiveAsync(byte[] buffer)
        {
            if (!device.Opened)
                throw new InvalidOperationException("Device is not open");

            if (await queue.Reader.WaitToReadAsync())
            {
                var data = await queue.Reader.ReadAsync();
                data.AsSpan().CopyTo(buffer);
                return data.Length;
            }
            return 0;
        }

        public void Connect()
        {
            device.StartCapture();
        }
    }
}