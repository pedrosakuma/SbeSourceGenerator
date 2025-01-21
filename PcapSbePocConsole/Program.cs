using PcapSbePocConsole.Configs;
using PcapSbePocConsole.Connection;
using System.Net;

namespace PcapSbePocConsole
{
    public partial class Program
    {
        static async Task Main(string[] args)
        {
            byte channelEqt = 84;
            byte channelDrv = 72;

            var c = new MarketConfig
            (
                new Dictionary<byte, ChannelConfig>
                {
                    { channelEqt, new ChannelConfig
                        (
                            channelEqt,
                            new AddressConfig
                            (
                                Environment.GetEnvironmentVariable("Eqt__instrumentDefinition")!,
                                IPEndPoint.Parse("224.100.0.1:10101")
                            ),
                            new AddressConfig
                            (
                                Environment.GetEnvironmentVariable("Eqt__snapshot")!,
                                IPEndPoint.Parse("224.100.0.2:10102")
                            ),
                            new IncrementalsConfig
                            {
                                FeedA = new AddressConfig(
                                    Environment.GetEnvironmentVariable("Eqt__incrementalsA")!,
                                    IPEndPoint.Parse("224.100.0.3:10103")
                                ),
                                FeedB = new AddressConfig(
                                    Environment.GetEnvironmentVariable("Eqt__incrementalsB")!,
                                    IPEndPoint.Parse("224.100.0.4:10104")
                                )
                            }
                        )
                    },
                    { channelDrv, new ChannelConfig
                        (
                            channelDrv,
                            new AddressConfig
                            (
                                Environment.GetEnvironmentVariable("Drv__instrumentDefinition")!,
                                IPEndPoint.Parse("224.200.0.1:10201")
                            ),
                            new AddressConfig
                            (
                                Environment.GetEnvironmentVariable("Drv__snapshot")!,
                                IPEndPoint.Parse("224.200.0.2:10202")
                            ),
                            new IncrementalsConfig
                            {
                                FeedA = new AddressConfig(
                                    Environment.GetEnvironmentVariable("Drv__incrementalsA")!,
                                    IPEndPoint.Parse("224.200.0.3:10203")
                                ),
                                FeedB = new AddressConfig(
                                    Environment.GetEnvironmentVariable("Drv__incrementalsB")!,
                                    IPEndPoint.Parse("224.200.0.4:10204")
                                )
                            }
                        )
                    }
                }
            );
            var p = new PcapMarketDataConnectionProvider(c);

            CancellationTokenSource source = new CancellationTokenSource();

            InstrumentDefinitionSyncExecutionState instrumentSyncEqt = new InstrumentDefinitionSyncExecutionState(p, channelEqt);
            IncrementalsSyncExecutionState incrementalsSyncEqt = new IncrementalsSyncExecutionState(p, channelEqt, Feeds.FeedA | Feeds.FeedB);
            SnapshotSyncExecutionState snapshotSyncEqt = new SnapshotSyncExecutionState(p, channelEqt);

            InstrumentDefinitionSyncExecutionState instrumentSyncDrv = new InstrumentDefinitionSyncExecutionState(p, channelDrv);
            IncrementalsSyncExecutionState incrementalsSyncDrv = new IncrementalsSyncExecutionState(p, channelDrv, Feeds.FeedA | Feeds.FeedB);
            SnapshotSyncExecutionState snapshotSyncDrv = new SnapshotSyncExecutionState(p, channelDrv);

            var incrementalPrepareEqt = incrementalsSyncEqt.Prepare(source.Token);
            var incrementalPrepareDrv = incrementalsSyncDrv.Prepare(source.Token);

            var snapshotPrepareEqt = snapshotSyncEqt.Prepare();
            var snapshotPrepareDrv = snapshotSyncDrv.Prepare();

            var stateEqt = instrumentSyncEqt.Sync();
            var stateDrv = instrumentSyncDrv.Sync();

            snapshotSyncEqt.Sync(stateEqt);
            snapshotSyncDrv.Sync(stateDrv);

            await snapshotPrepareEqt;
            incrementalsSyncEqt.Sync(stateEqt);
            await snapshotPrepareDrv;
            incrementalsSyncDrv.Sync(stateDrv);

            Console.ReadLine();
            source.Cancel();
            await incrementalPrepareEqt;
            await incrementalPrepareDrv;
        }
    }
}