using PacketDotNet;
using SharpPcap.LibPcap;

namespace PcapSbePocConsole
{
    public partial class Program
    {
        static readonly Dictionary<ulong, InstrumentDefinition> instrumentsById = new();
        static readonly Dictionary<string, InstrumentDefinition> instrumentsBySymbol = new();
        static readonly MarketDataHandler parser = new MarketDataHandler(instrumentsById, instrumentsBySymbol);

        static void Main(string[] args)
        {
            DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(TimeSpan.FromDays(1).Seconds).UtcDateTime);

            parser.StateChanged += Parser_StateChanged;
            parser.Init();
            Console.ReadLine();
            foreach (var stat in parser.Statistics.OrderBy(k => k.Key))
            {
                Console.WriteLine("{0} - {1}", stat.Key, stat.Value);
            }
        }

        private static void StartCapture(string file)
        {
            var captures = new List<CaptureFileReaderDevice>();
            var device = new CaptureFileReaderDevice(file);
            device.Open(new SharpPcap.DeviceConfiguration
            {
                Mode = SharpPcap.DeviceModes.DataTransferUdp
            });
            device.OnPacketArrival += Device_OnPacketArrival;
            Console.WriteLine("Starting capture {0}", Path.GetFileName(file));
            device.StartCapture();
        }

        private static void Parser_StateChanged(MarketDataState obj)
        {
            Console.WriteLine("State Changed: {0}", obj);
            switch (obj)
            {
                case MarketDataState.None:
                    StartCapture(
                        Environment.GetEnvironmentVariable("instrumentDefinition") ?? throw new NullReferenceException("instrumentDefinition is required")
                    );
                    break;
                case MarketDataState.Snapshot:
                    StartCapture(
                        Environment.GetEnvironmentVariable("snapshot") ?? throw new NullReferenceException("snapshot is required")
                    );
                    break;
                case MarketDataState.Incrementals:
                    StartCapture(
                        Environment.GetEnvironmentVariable("incrementalsA") ?? throw new NullReferenceException("snapshot is required")
                    );
                    //StartCapture(
                    //    Environment.GetEnvironmentVariable("incrementalsB") ?? throw new NullReferenceException("snapshot is required")
                    //);
                    break;
            }
        }

        private unsafe static void Device_OnPacketArrival(object sender, SharpPcap.PacketCapture e)
        {
            var rawPacket = e.GetPacket();
            var p = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

            var udp = p.Extract<UdpPacket>();
            var data = udp.PayloadData.AsSpan();

            parser.Parse(data);
        }
    }
}