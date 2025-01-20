using PcapSbePocConsole.Configs;
using PcapSbePocConsole.Connection;
using System.Net;

namespace PcapSbePocConsole
{
    public partial class Program
    {
        static async Task Main(string[] args)
        {
            byte channel = 84;

            var c = new MarketConfig
            (
                new Dictionary<byte, ChannelConfig>
                {
                    { channel, new ChannelConfig
                        (
                            channel,
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
            var p = new PcapMarketDataConnectionProvider(c);
            InstrumentDefinitionSyncExecutionState instrumentSync = new InstrumentDefinitionSyncExecutionState(p);
            IncrementalsSyncExecutionState incrementalsSync = new IncrementalsSyncExecutionState(p, Feeds.FeedA | Feeds.FeedB);
            SnapshotSyncExecutionState snapshotSync = new SnapshotSyncExecutionState(p);
            CancellationTokenSource source = new CancellationTokenSource();

            var incrementalPrepare = Task.Run(() => incrementalsSync.Prepare(channel, source.Token));
            var snapshotPrepare = Task.Run(() => snapshotSync.Prepare(channel));
            var state = instrumentSync.Sync(channel);
            snapshotSync.Sync(state);
            await snapshotPrepare;
            incrementalsSync.Sync(state);
            Console.ReadLine();
            source.Cancel();
            await incrementalPrepare;
        }
    }
}