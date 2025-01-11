using SharpPcap.LibPcap;
using System.Text.RegularExpressions;

namespace PcapSbePocConsole
{
    public partial class Program
    {
        private const int PCAPHeaderSize = 46;
        static readonly Dictionary<ulong, InstrumentDefinition> instrumentsById = new();
        static readonly Dictionary<string, InstrumentDefinition> instrumentsBySymbol = new();
        static readonly MarketDataHandler parser = new MarketDataHandler(instrumentsById, instrumentsBySymbol);
        private static string? instrumentDefinition;
        private static string? incremental;
        private static TaskCompletionSource instrumentDefinitionCompletion;
        private static TaskCompletionSource incrementalCompletion;

        static void Main(string[] args)
        {
            DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(TimeSpan.FromDays(1).Seconds).UtcDateTime);
            instrumentDefinitionCompletion = new TaskCompletionSource();
            incrementalCompletion = new TaskCompletionSource();

            instrumentDefinition = Environment.GetEnvironmentVariable("instrumentDefinition");
            incremental = Environment.GetEnvironmentVariable("incremental");
            if (instrumentDefinition == null || incremental == null)
            {
                Console.WriteLine("instrumentDefinition and incremental environment variables must be set");
                return;
            }
            parser.StateChanged += Parser_StateChanged;
            StartCapture(instrumentDefinition, instrumentDefinitionCompletion);

            Task.WaitAll(
                instrumentDefinitionCompletion.Task,
                incrementalCompletion.Task
            );
            foreach (var stat in parser.Statistics.OrderBy(k => k.Key))
            {
                Console.WriteLine("{0} - {1}", stat.Key, stat.Value);
            }
        }

        private static void StartCapture(string file, TaskCompletionSource completion)
        {
            var captures = new List<CaptureFileReaderDevice>();
            var device = new CaptureFileReaderDevice(file);
            device.Open(new SharpPcap.DeviceConfiguration
            {
            });
            device.OnPacketArrival += Device_OnPacketArrival;
            device.OnCaptureStopped += (sender, status) => Device_OnCaptureStopped(sender, status, completion);
            Console.WriteLine("Starting capture {0}", Path.GetFileName(file));
            device.StartCapture();
        }

        private static void Device_OnCaptureStopped(object sender, SharpPcap.CaptureStoppedEventStatus status, TaskCompletionSource completion)
        {
            if(sender is CaptureFileReaderDevice device)
            {
                Console.WriteLine("Capture {0} stopped", device.FileName);
                completion.SetResult();
            }
        }

        private static void Parser_StateChanged(MarketDataState obj)
        {
            if (obj == MarketDataState.Synchronized)
            {
                StartCapture(incremental, incrementalCompletion);
            }
        }

        private unsafe static void Device_OnPacketArrival(object sender, SharpPcap.PacketCapture e)
        {
            var data = e.Data.Slice(PCAPHeaderSize);
            parser.Parse(data);
        }
    }
}