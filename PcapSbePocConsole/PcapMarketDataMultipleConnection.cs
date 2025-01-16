using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using B3.Market.Data.Messages;

namespace PcapSbePocConsole
{
    public class PcapMarketDataMultipleConnection : IMarketDataConnection
    {
        private uint lastConsumed;
        private readonly Channel<byte[]> queue;
        private readonly CaptureFileReaderDevice[] devices;

        public bool IsConnected => !queue.Reader.Completion.IsCompleted;

        public PcapMarketDataMultipleConnection(string[] files, int capacity)
        {
            queue = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions()
            {
                SingleReader = true
            });
            devices = new CaptureFileReaderDevice[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                var device = new CaptureFileReaderDevice(files[i]);
                devices[i] = device;
                device.Open(new DeviceConfiguration
                {
                    BufferSize = 524288
                });
                device.OnPacketArrival += Device_OnPacketArrival;
                device.OnCaptureStopped += Device_OnCaptureStopped;
            }
        }

        private void Device_OnCaptureStopped(object sender, CaptureStoppedEventStatus status)
        {
            if(devices.All(d => !d.Opened))
                queue.Writer.Complete();
        }

        private readonly DateTime readFrom;
        private void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            var p = e.GetPacket().GetPacket();
            var udp = p.Extract<UdpPacket>();
            queue.Writer.TryWrite(udp.PayloadData);
        }

        public void Dispose()
        {
            for (int i = 0; i < devices.Length; i++)
                devices[i].Dispose();
        }

        public async Task<int> ReceiveAsync(byte[] buffer)
        {
            if (!devices.All(d => d.Opened))
                throw new InvalidOperationException("Device is not open");

            while(await queue.Reader.WaitToReadAsync())
            {
                var data = await queue.Reader.ReadAsync();
                if (ShouldReturn(data))
                {
                    data.AsSpan().CopyTo(buffer);
                    return data.Length;
                }
            }
            return 0;
        }

        private bool ShouldReturn(byte[] data)
        {
            ref readonly PacketHeader packet = ref MemoryMarshal.AsRef<PacketHeader>(data);
            if (packet.SequenceNumber <= lastConsumed)
                return false;
            lastConsumed = packet.SequenceNumber;
            return true;
        }

        public void Connect()
        {
            for (int i = 0; i < devices.Length; i++)
                devices[i].StartCapture();
        }
    }
}