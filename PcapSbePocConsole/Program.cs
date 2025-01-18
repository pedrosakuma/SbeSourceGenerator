using PcapSbePocConsole.Configs;
using PcapSbePocConsole.Connection;
using System.Net;

namespace PcapSbePocConsole
{
    public partial class Program
    {
        static async Task Main(string[] args)
        {
            var c = new MarketConfig
            (
                new Dictionary<byte, ChannelConfig>
                {
                    { 84, new ChannelConfig
                        (
                            84,
                            new AddressConfig
                            (
                                Environment.GetEnvironmentVariable("instrumentDefinition")!,
                                IPEndPoint.Parse("224.100.0.1:10101")
                            ),
                            new AddressConfig
                            (
                                Environment.GetEnvironmentVariable("snapshot")!,
                                IPEndPoint.Parse("224.100.0.2:10102")
                            ),
                            new IncrementalsConfig
                            {
                                FeedA = new AddressConfig(
                                    Environment.GetEnvironmentVariable("incrementalsA")!,
                                    IPEndPoint.Parse("224.100.0.3:10103")
                                ),
                                FeedB = new AddressConfig(
                                    Environment.GetEnvironmentVariable("incrementalsB")!,
                                    IPEndPoint.Parse("224.100.0.4:10104")
                                )
                            }
                        )
                    }
                }
            );
            byte channel = 84;
            var p = new PcapMarketDataConnectionProvider(c, new DateTime(2020, 9, 9, 15, 50, 00));
            var client = new MarketDataClient(p);
            InstrumentDefinitionSyncExecutionState instrumentSync = new InstrumentDefinitionSyncExecutionState(p);
            IncrementalsSyncExecutionState incrementalsSync = new IncrementalsSyncExecutionState(p, Feeds.FeedA | Feeds.FeedB);
            SnapshotSyncExecutionState snapshotSync = new SnapshotSyncExecutionState(p);
            var incrementalPrepare = Task.Run(() => incrementalsSync.Prepare(channel));
            var snapshotPrepare = Task.Run(() => snapshotSync.Prepare(channel));
            var state = instrumentSync.Sync(channel);
            snapshotSync.Sync(state);
            await snapshotPrepare;
            incrementalsSync.Sync(state);
            await incrementalPrepare;
            Console.ReadLine();
        }
    }
}