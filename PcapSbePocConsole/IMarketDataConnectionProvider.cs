namespace PcapSbePocConsole
{
    public interface IMarketDataConnectionProvider
    {
        IMarketDataConnection ConnectInstrumentDefinition(byte channel);
        IMarketDataConnection ConnectSnapshot(byte channel);
        IMarketDataConnection ConnectIncrementals(byte channel, Feeds feeds);
    }

    public interface IMarketDataConnection : IDisposable
    {
        void Connect();
        bool IsConnected { get; }
        int Receive(byte[] buffer);
    }
}
