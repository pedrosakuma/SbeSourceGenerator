namespace PcapSbePocConsole
{
    public partial class Program
    {
        static async Task Main(string[] args)
        {
            var p = new PcapMarketDataConnectionProvider(new MarketConfig
            {
                Channels = new Dictionary<byte, ChannelConfig> 
                {
                    { 84, new ChannelConfig
                        {
                            Channel = 84,
                            Incrementals = new IncrementalsConfig
                            {
                                AddressFeedA = Environment.GetEnvironmentVariable("incrementalsA")
                                    ?? throw new NullReferenceException("incrementalsA is required"),
                                AddressFeedB = Environment.GetEnvironmentVariable("incrementalsB")
                                    ?? throw new NullReferenceException("incrementalsB is required")
                            },
                            InstrumentDefinition = new InstrumentDefinitionConfig
                            {
                                Address = Environment.GetEnvironmentVariable("instrumentDefinition")
                                    ?? throw new NullReferenceException("instrumentDefinition is required")
                            },
                            Snapshot = new SnapshotConfig
                            {
                                Address = Environment.GetEnvironmentVariable("snapshot")
                                    ?? throw new NullReferenceException("snapshot is required")
                            }
                        }
                    }
                }
            }, new DateTime(2020, 9, 9, 15, 50, 00));
            byte channel = 84;
            var client = new MarketDataClient(p);
            InstrumentDefinitionSyncExecutionState instrumentSync = new InstrumentDefinitionSyncExecutionState(p);
            IncrementalsSyncExecutionState incrementalsSync = new IncrementalsSyncExecutionState(p, Feeds.FeedA | Feeds.FeedB);
            SnapshotSyncExecutionState snapshotSync = new SnapshotSyncExecutionState(p);
            var instrumentSyncTask = instrumentSync.SyncAsync(channel);
            var incrementalsSyncTask = incrementalsSync.PrepareAsync(channel);
            await snapshotSync.PrepareAsync(channel);
            var state = await instrumentSyncTask;
            snapshotSync.Sync(state);
            incrementalsSync.Sync(state);
            await incrementalsSyncTask;
        }
    }
}