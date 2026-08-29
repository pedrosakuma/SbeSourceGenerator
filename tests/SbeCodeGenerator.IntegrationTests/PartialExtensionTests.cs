using System;
using System.Runtime.InteropServices;
using Edge.Cases.Test.V0;
using V0Versioning = Versioning.Test.V2;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Issue #167: verifies that non-blittable generated types are emitted as <c>partial</c>,
    /// so consumers can extend them safely without forking the generated code.
    /// </summary>
    public class PartialExtensionTests
    {
        // --- Extend the generated SbeDispatcher with a custom helper method ---

        [Fact]
        public void SbeDispatcher_PartialExtension_IsCompiledAndCallable()
        {
            Span<byte> buffer = stackalloc byte[MessageHeader.MESSAGE_SIZE + TradeData.MESSAGE_SIZE];
            ref var header = ref MemoryMarshal.AsRef<MessageHeader>(buffer);
            header.BlockLength = (ushort)TradeData.BLOCK_LENGTH;
            header.TemplateId = (ushort)TradeData.MESSAGE_ID;
            header.SchemaId = 1;
            header.Version = 0;

            var handler = new SilentHandler();

            // Calls a method defined in our partial extension below.
            bool dispatched = SbeDispatcher.DispatchAndCount(buffer, ref handler, out int dispatchCount);

            Assert.True(dispatched);
            Assert.Equal(1, dispatchCount);
        }

        // --- Extend the generated ISbeMessageHandler interface with a default method ---

        [Fact]
        public void ISbeMessageHandler_PartialExtension_DefaultMethodIsAvailable()
        {
            ISbeMessageHandler handler = new SilentHandler();
            // DescribeSelf is defined on our partial interface below.
            Assert.Equal("ISbeMessageHandler", handler.DescribeSelf());
        }

        // --- Extend a generated VersionMap with a custom lookup ---

        [Fact]
        public void VersionMap_PartialExtension_AddsCustomLookup()
        {
            // GetVersionOrDefault is defined in our partial extension below.
            int v = V0Versioning.EvolvingOrderVersionMap.GetVersionOrDefault(blockLength: 16, fallback: -42);
            Assert.Equal(0, v);

            int unknown = V0Versioning.EvolvingOrderVersionMap.GetVersionOrDefault(blockLength: 999, fallback: -42);
            Assert.Equal(-42, unknown);
        }

        // --- Extend a generated DataReader ref struct with a custom helper ---

        [Fact]
        public void DataReader_PartialExtension_AddsCustomHelper()
        {
            Span<byte> buffer = stackalloc byte[TradeData.MESSAGE_SIZE];
            ref var trade = ref MemoryMarshal.AsRef<TradeData>(buffer);
            trade.Quantity = 42;

            Assert.True(TradeData.TryParse(buffer, out var reader));
            // QuantityDoubled is defined in the partial extension below.
            Assert.Equal(84, reader.QuantityDoubled());
        }

        private struct SilentHandler : ISbeMessageHandler
        {
            public void OnTrade(in TradeDataReader reader, int blockLength, int version) { }
            public void OnTextMessage(in TextMessageDataReader reader, int blockLength, int version) { }
            public void OnMarketData(in MarketDataDataReader reader, int blockLength, int version) { }
            public void OnUnknownMessage(int templateId, int blockLength, int version, ReadOnlySpan<byte> payload) { }
        }
    }
}

namespace Edge.Cases.Test.V0
{
    // Partial extension of the generated dispatcher static class.
    public static partial class SbeDispatcher
    {
        public static bool DispatchAndCount<T>(ReadOnlySpan<byte> buffer, ref T handler, out int count)
            where T : struct, ISbeMessageHandler
        {
            bool ok = Dispatch(buffer, ref handler);
            count = ok ? 1 : 0;
            return ok;
        }
    }

    // Partial extension of the generated handler interface (default interface method).
    public partial interface ISbeMessageHandler
    {
        string DescribeSelf() => nameof(ISbeMessageHandler);
    }

    // Partial extension of the generated DataReader ref struct.
    public ref partial struct TradeDataReader
    {
        public long QuantityDoubled() => Data.Quantity * 2;
    }
}

namespace Versioning.Test.V2
{
    // Partial extension of the generated VersionMap static class.
    public static partial class EvolvingOrderVersionMap
    {
        public static int GetVersionOrDefault(int blockLength, int fallback)
            => TryGetVersion(blockLength, out var v) ? v : fallback;
    }
}
