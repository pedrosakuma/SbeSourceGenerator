using PacketDotNet;
using PcapSbePocConsole.Models;
using SharpPcap;
using SharpPcap.LibPcap;

namespace PcapSbePocConsole
{
    public partial class Program
    {
        static readonly TaskCompletionSource completion = new TaskCompletionSource();
        static readonly Dictionary<ulong, InstrumentDefinition> instrumentsById = new();
        static readonly Dictionary<string, InstrumentDefinition> instrumentsBySymbol = new();
        static readonly MarketDataHandler parser = new MarketDataHandler(instrumentsById, instrumentsBySymbol);

        static void Main(string[] args)
        {
            parser.StateChanged += Parser_StateChanged;
            parser.Init();
            completion.Task.Wait();

            foreach (var stat in parser.Statistics.OrderBy(k => k.Key))
            {
                Console.WriteLine("{0} - {1}", stat.Key, stat.Value);
            }
        }

        private static void StartCapture(string file)
        {
            StartCapture(file, () => { });
        }
        private static void StartCapture(string file, Action end)
        {
            var device = new CaptureFileReaderDevice(file);
            device.Open(new DeviceConfiguration { 
                BufferSize = 524288
            });
            device.OnPacketArrival += Device_OnPacketArrival;
            device.OnCaptureStopped += (sender, e) =>
            {
                Console.WriteLine("Capture stopped {0}", ((CaptureFileReaderDevice)sender).FileName);
                end();
            };
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
                        Environment.GetEnvironmentVariable("snapshot") ?? throw new NullReferenceException("snapshot is required"),
                        () => parser.ChangeState(MarketDataState.Incrementals)
                    );
                    break;
                case MarketDataState.Incrementals:
                    StartCapture(
                        Environment.GetEnvironmentVariable("incrementalsA") ?? throw new NullReferenceException("snapshot is required"),
                        () => parser.ChangeState(MarketDataState.End)
                    );
                    //StartCapture(
                    //    Environment.GetEnvironmentVariable("incrementalsB") ?? throw new NullReferenceException("snapshot is required")
                    //);
                    break;
                case MarketDataState.End:
                    completion.SetResult();
                    break;
            }
        }

        private unsafe static void Device_OnPacketArrival(object sender, SharpPcap.PacketCapture e)
        {
            var p = e.GetPacket().GetPacket();
            var udp = p.Extract<UdpPacket>();
            var data = udp.PayloadData.AsSpan();

            parser.Parse(data);
        }
    }
}