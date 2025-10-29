# Examples

This folder contains example applications demonstrating the use of the SBE Code Generator.

## Projects

### PcapSbePocConsole

A comprehensive example demonstrating SBE encoding/decoding with B3 market data.

**Features**:
- Real-time market data processing
- Multiple feed handling (A, B, C)
- Snapshot and incremental synchronization
- Order book management
- Market phase tracking

**Schemas**:
- `b3-market-data-messages-1.8.0.xml` - B3 market data protocol
- `b3-entrypoint-messages-8.0.0.xml` - B3 entrypoint protocol

### PcapMarketReplayConsole

A PCAP-based market data replay application.

**Features**:
- PCAP file parsing
- Network packet processing
- Market data replay functionality
- Ethernet/IP/UDP packet handling

**Dependencies**:
- SharpPcap library for packet capture

### SbeBinanceConsole

Binance market data processing example.

**Features**:
- Binance SBE schema integration
- Cryptocurrency market data handling
- Real-time data processing
- Spectre.Console live dashboard with interactive commands (`add`, `remove`, `api`, `help`, `quit`) for managing subscriptions and viewing trades/best bid-ask tables

**Schemas**:
- `binance-stream-1.0.xml` - Binance streaming protocol
- `binance-spot-3.1.xml` - Binance spot trading protocol

### HighPerformanceMarketData ⭐ NEW

A comprehensive example demonstrating high-performance SBE message processing best practices.

**Features**:
- Zero-allocation message processing
- Buffer pooling with ArrayPool
- Efficient repeating group handling
- Batch processing techniques
- Performance benchmarking
- Demonstrates all optimization best practices

**Key Techniques**:
- Stack allocation for small messages
- ArrayPool for larger buffers
- Streaming group processing
- Incremental decoding
- Multi-million messages/second throughput

**Perfect for**: Learning performance optimization patterns

## Running the Examples

Each example is a standalone console application. To run:

```bash
# From the repository root
cd examples/PcapSbePocConsole
dotnet run

# Or
cd examples/PcapMarketReplayConsole
dotnet run

# Or
cd examples/SbeBinanceConsole
dotnet run
```
When running `SbeBinanceConsole`, provide your Binance `X-MBX-APIKEY` as the first argument or enter it when prompted. The interactive console accepts the commands `add`, `remove`, `api`, `help`, and `quit` to manage subscriptions while streaming.

## How to Use as Templates

These examples can serve as templates for your own applications:

1. Copy an example project that's closest to your use case
2. Replace the SBE schema files with your own
3. Update the `<AdditionalFiles>` in the `.csproj` to reference your schemas
4. Modify the business logic to suit your needs
5. The source generator will automatically generate types from your schema

## Project References

All example projects reference the SBE Code Generator as an analyzer:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\SbeCodeGenerator\SbeSourceGenerator.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

This ensures the source generator runs during build and generates types from your schemas.
