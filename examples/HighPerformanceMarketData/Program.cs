using System.Buffers;
using System.Diagnostics;
using HighPerf.MarketData;

namespace HighPerformanceMarketData;

/// <summary>
/// Demonstrates high-performance SBE message processing using best practices:
/// - Zero allocations
/// - Buffer pooling
/// - Efficient group processing
/// - Minimal copying
/// </summary>
class Program
{
    private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;
    
    static void Main(string[] args)
    {
        Console.WriteLine("High-Performance Market Data Example");
        Console.WriteLine("=====================================\n");
        
        // Run different scenarios
        RunQuoteProcessing();
        RunTradeProcessing();
        RunDepthSnapshotProcessing();
        RunBatchProcessing();
        RunPerformanceTest();
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Demonstrates simple quote message processing
    /// </summary>
    static void RunQuoteProcessing()
    {
        Console.WriteLine("1. Quote Processing");
        Console.WriteLine("-------------------");
        
        // Create a quote
        var quote = new QuoteData
        {
            InstrumentId = 1001,
            Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            BidPrice = 995000, // 99.5000
            BidQuantity = 100,
            AskPrice = 995500, // 99.5500
            AskQuantity = 200
        };
        
        // Use stack allocation for small messages
        Span<byte> buffer = stackalloc byte[QuoteData.MESSAGE_SIZE];
        
        // Encode
        if (quote.TryEncode(buffer, out int written))
        {
            Console.WriteLine($"✓ Encoded quote: {written} bytes");
            
            // Decode
            if (QuoteData.TryParse(buffer, out var decoded, out _))
            {
                Console.WriteLine($"  Instrument: {decoded.InstrumentId}");
                Console.WriteLine($"  Bid: {decoded.BidPrice / 10000.0:F4} x {decoded.BidQuantity}");
                Console.WriteLine($"  Ask: {decoded.AskPrice / 10000.0:F4} x {decoded.AskQuantity}");
                Console.WriteLine($"  Spread: {(decoded.AskPrice - decoded.BidPrice) / 10000.0:F4}");
            }
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates trade message processing
    /// </summary>
    static void RunTradeProcessing()
    {
        Console.WriteLine("2. Trade Processing");
        Console.WriteLine("-------------------");
        
        var trade = new TradeData
        {
            InstrumentId = 1001,
            Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            TradeId = 123456789,
            Price = 995250, // 99.5250
            Quantity = 50,
            Side = Side.Buy
        };
        
        Span<byte> buffer = stackalloc byte[TradeData.MESSAGE_SIZE];
        
        if (trade.TryEncode(buffer, out int written))
        {
            Console.WriteLine($"✓ Encoded trade: {written} bytes");
            
            if (TradeData.TryParse(buffer, out var decoded, out _))
            {
                Console.WriteLine($"  Trade ID: {decoded.TradeId}");
                Console.WriteLine($"  Price: {decoded.Price / 10000.0:F4}");
                Console.WriteLine($"  Quantity: {decoded.Quantity}");
                Console.WriteLine($"  Side: {decoded.Side}");
            }
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates depth snapshot processing with repeating groups
    /// </summary>
    static void RunDepthSnapshotProcessing()
    {
        Console.WriteLine("3. Depth Snapshot Processing (with Groups)");
        Console.WriteLine("------------------------------------------");
        
        // Create depth snapshot
        var snapshot = new DepthSnapshotData
        {
            InstrumentId = 1001,
            Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        
        // Create bids (sorted descending by price)
        var bids = new BidsData[5];
        for (int i = 0; i < 5; i++)
        {
            bids[i] = new BidsData
            {
                Price = 995000 - (i * 100), // Decreasing prices
                Quantity = 100 + (i * 10),
                OrderCount = (uint)(3 + i)
            };
        }
        
        // Create asks (sorted ascending by price)
        var asks = new AsksData[5];
        for (int i = 0; i < 5; i++)
        {
            asks[i] = new AsksData
            {
                Price = 995500 + (i * 100), // Increasing prices
                Quantity = 50 + (i * 10),
                OrderCount = (uint)(2 + i)
            };
        }
        
        // Use pooled buffer for larger messages with groups
        byte[] pooledBuffer = BufferPool.Rent(4096);
        try
        {
            // Encode
            snapshot.BeginEncoding(pooledBuffer, out var writer);
            DepthSnapshotData.TryEncodeBids(ref writer, bids);
            DepthSnapshotData.TryEncodeAsks(ref writer, asks);
            
            int totalWritten = writer.Position;
            Console.WriteLine($"✓ Encoded depth snapshot: {totalWritten} bytes");
            
            // Decode and process groups
            if (DepthSnapshotData.TryParse(pooledBuffer, out var decoded, out var variableData))
            {
                Console.WriteLine($"  Instrument: {decoded.InstrumentId}");
                Console.WriteLine("  Bids:");
                
                int bidCount = 0;
                long totalBidVolume = 0;
                
                decoded.ConsumeVariableLengthSegments(
                    variableData,
                    bid => 
                    {
                        Console.WriteLine($"    {bid.Price / 10000.0:F4} x {bid.Quantity} ({bid.OrderCount} orders)");
                        bidCount++;
                        totalBidVolume += bid.Quantity;
                        return true;
                    },
                    ask => 
                    {
                        if (bidCount == bids.Length) // Print asks header after bids
                        {
                            Console.WriteLine($"  Total Bid Volume: {totalBidVolume}");
                            Console.WriteLine("  Asks:");
                        }
                        Console.WriteLine($"    {ask.Price / 10000.0:F4} x {ask.Quantity} ({ask.OrderCount} orders)");
                        return true;
                    }
                );
            }
        }
        finally
        {
            BufferPool.Return(pooledBuffer);
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates batch processing of multiple messages
    /// </summary>
    static void RunBatchProcessing()
    {
        Console.WriteLine("4. Batch Processing");
        Console.WriteLine("-------------------");
        
        const int batchSize = 1000;
        byte[] buffer = BufferPool.Rent(batchSize * QuoteData.MESSAGE_SIZE);
        
        try
        {
            int offset = 0;
            
            // Encode batch of quotes
            for (int i = 0; i < batchSize; i++)
            {
                var quote = new QuoteData
                {
                    InstrumentId = (uint)(1001 + (i % 10)),
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    BidPrice = 990000 + i,
                    BidQuantity = 100,
                    AskPrice = 990500 + i,
                    AskQuantity = 200
                };
                
                if (quote.TryEncode(buffer.AsSpan(offset), out int written))
                {
                    offset += written;
                }
            }
            
            Console.WriteLine($"✓ Encoded {batchSize} quotes in {offset} bytes");
            
            // Decode and process batch
            offset = 0;
            int decodedCount = 0;
            long totalSpread = 0;
            
            for (int i = 0; i < batchSize; i++)
            {
                if (QuoteData.TryParse(buffer.AsSpan(offset), out var quote, out _))
                {
                    offset += QuoteData.MESSAGE_SIZE;
                    decodedCount++;
                    totalSpread += (quote.AskPrice - quote.BidPrice);
                }
            }
            
            Console.WriteLine($"✓ Decoded {decodedCount} quotes");
            Console.WriteLine($"  Average spread: {totalSpread / decodedCount / 10000.0:F4}");
        }
        finally
        {
            BufferPool.Return(buffer);
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// Performance test to measure throughput
    /// </summary>
    static void RunPerformanceTest()
    {
        Console.WriteLine("5. Performance Test");
        Console.WriteLine("-------------------");
        
        const int iterations = 1_000_000;
        byte[] buffer = BufferPool.Rent(QuoteData.MESSAGE_SIZE);
        
        try
        {
            var quote = new QuoteData
            {
                InstrumentId = 1001,
                Timestamp = 123456789,
                BidPrice = 995000,
                BidQuantity = 100,
                AskPrice = 995500,
                AskQuantity = 200
            };
            
            // Warmup
            for (int i = 0; i < 1000; i++)
            {
                quote.TryEncode(buffer, out _);
                QuoteData.TryParse(buffer, out _, out _);
            }
            
            // Measure encoding
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                quote.TryEncode(buffer, out _);
            }
            sw.Stop();
            
            double encodeNs = (double)sw.ElapsedTicks / iterations * 1_000_000_000 / Stopwatch.Frequency;
            double encodeThroughput = iterations / sw.Elapsed.TotalSeconds;
            
            Console.WriteLine($"  Encode: {encodeNs:F2} ns/msg ({encodeThroughput / 1_000_000:F2} M msg/s)");
            
            // Measure decoding
            quote.TryEncode(buffer, out _);
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                QuoteData.TryParse(buffer, out _, out _);
            }
            sw.Stop();
            
            double decodeNs = (double)sw.ElapsedTicks / iterations * 1_000_000_000 / Stopwatch.Frequency;
            double decodeThroughput = iterations / sw.Elapsed.TotalSeconds;
            
            Console.WriteLine($"  Decode: {decodeNs:F2} ns/msg ({decodeThroughput / 1_000_000:F2} M msg/s)");
            
            // Measure round-trip
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                quote.TryEncode(buffer, out _);
                QuoteData.TryParse(buffer, out _, out _);
            }
            sw.Stop();
            
            double roundTripNs = (double)sw.ElapsedTicks / iterations * 1_000_000_000 / Stopwatch.Frequency;
            double roundTripThroughput = iterations / sw.Elapsed.TotalSeconds;
            
            Console.WriteLine($"  Round-trip: {roundTripNs:F2} ns/msg ({roundTripThroughput / 1_000_000:F2} M msg/s)");
        }
        finally
        {
            BufferPool.Return(buffer);
        }
        
        Console.WriteLine();
    }
}
