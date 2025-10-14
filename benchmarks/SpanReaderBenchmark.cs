using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SbeSourceGenerator.Runtime;

namespace SbeCodeGenerator.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 5)]
    public class SpanReaderBenchmark
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct GroupSizeEncoding
        {
            public ushort BlockLength;
            public uint NumInGroup;
            
            public const int MESSAGE_SIZE = 6;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BidData
        {
            public long Price;
            public long Quantity;
            
            public const int MESSAGE_SIZE = 16;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AskData
        {
            public long Price;
            public long Quantity;
            
            public const int MESSAGE_SIZE = 16;
        }

        private byte[] _buffer;
        private const int NumBids = 100;
        private const int NumAsks = 100;

        [GlobalSetup]
        public void Setup()
        {
            // Create buffer with test data
            int bufferSize = 
                GroupSizeEncoding.MESSAGE_SIZE + (NumBids * BidData.MESSAGE_SIZE) +
                GroupSizeEncoding.MESSAGE_SIZE + (NumAsks * AskData.MESSAGE_SIZE);
            
            _buffer = new byte[bufferSize];
            
            int offset = 0;
            
            // Write bids group header
            ref var bidsHeader = ref MemoryMarshal.AsRef<GroupSizeEncoding>(_buffer.AsSpan(offset));
            bidsHeader.BlockLength = BidData.MESSAGE_SIZE;
            bidsHeader.NumInGroup = NumBids;
            offset += GroupSizeEncoding.MESSAGE_SIZE;
            
            // Write bids
            for (int i = 0; i < NumBids; i++)
            {
                ref var bid = ref MemoryMarshal.AsRef<BidData>(_buffer.AsSpan(offset));
                bid.Price = 10000 + i;
                bid.Quantity = 100 + i;
                offset += BidData.MESSAGE_SIZE;
            }
            
            // Write asks group header
            ref var asksHeader = ref MemoryMarshal.AsRef<GroupSizeEncoding>(_buffer.AsSpan(offset));
            asksHeader.BlockLength = AskData.MESSAGE_SIZE;
            asksHeader.NumInGroup = NumAsks;
            offset += GroupSizeEncoding.MESSAGE_SIZE;
            
            // Write asks
            for (int i = 0; i < NumAsks; i++)
            {
                ref var ask = ref MemoryMarshal.AsRef<AskData>(_buffer.AsSpan(offset));
                ask.Price = 11000 + i;
                ask.Quantity = 50 + i;
                offset += AskData.MESSAGE_SIZE;
            }
        }

        [Benchmark(Baseline = true)]
        public long ParseWithOffsetManual()
        {
            ReadOnlySpan<byte> buffer = _buffer;
            int offset = 0;
            long totalBidQty = 0;
            long totalAskQty = 0;
            
            // Process bids
            ref readonly GroupSizeEncoding groupBids = ref MemoryMarshal.AsRef<GroupSizeEncoding>(buffer.Slice(offset));
            offset += GroupSizeEncoding.MESSAGE_SIZE;
            
            for (int i = 0; i < groupBids.NumInGroup; i++)
            {
                ref readonly var data = ref MemoryMarshal.AsRef<BidData>(buffer.Slice(offset));
                totalBidQty += data.Quantity;
                offset += BidData.MESSAGE_SIZE;
            }
            
            // Process asks
            ref readonly GroupSizeEncoding groupAsks = ref MemoryMarshal.AsRef<GroupSizeEncoding>(buffer.Slice(offset));
            offset += GroupSizeEncoding.MESSAGE_SIZE;
            
            for (int i = 0; i < groupAsks.NumInGroup; i++)
            {
                ref readonly var data = ref MemoryMarshal.AsRef<AskData>(buffer.Slice(offset));
                totalAskQty += data.Quantity;
                offset += AskData.MESSAGE_SIZE;
            }
            
            return totalBidQty + totalAskQty;
        }

        [Benchmark]
        public long ParseWithSpanReader()
        {
            var reader = new SpanReader(_buffer);
            long totalBidQty = 0;
            long totalAskQty = 0;
            
            // Process bids
            if (reader.TryRead<GroupSizeEncoding>(out var groupBids))
            {
                for (int i = 0; i < groupBids.NumInGroup; i++)
                {
                    if (reader.TryRead<BidData>(out var data))
                    {
                        totalBidQty += data.Quantity;
                    }
                }
            }
            
            // Process asks
            if (reader.TryRead<GroupSizeEncoding>(out var groupAsks))
            {
                for (int i = 0; i < groupAsks.NumInGroup; i++)
                {
                    if (reader.TryRead<AskData>(out var data))
                    {
                        totalAskQty += data.Quantity;
                    }
                }
            }
            
            return totalBidQty + totalAskQty;
        }

        [Benchmark]
        public long ParseWithSpanReaderNoErrorCheck()
        {
            var reader = new SpanReader(_buffer);
            long totalBidQty = 0;
            long totalAskQty = 0;
            
            // Process bids
            reader.TryRead<GroupSizeEncoding>(out var groupBids);
            for (int i = 0; i < groupBids.NumInGroup; i++)
            {
                reader.TryRead<BidData>(out var data);
                totalBidQty += data.Quantity;
            }
            
            // Process asks
            reader.TryRead<GroupSizeEncoding>(out var groupAsks);
            for (int i = 0; i < groupAsks.NumInGroup; i++)
            {
                reader.TryRead<AskData>(out var data);
                totalAskQty += data.Quantity;
            }
            
            return totalBidQty + totalAskQty;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<SpanReaderBenchmark>();
        }
    }
}
