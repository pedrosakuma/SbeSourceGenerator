using System;
using System.Collections.Generic;
using Xunit;
using OB = Integration.Test.V0.OrderBookData;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Issue #156: foreach-style enumerators on {Msg}DataReader for top-level groups.
    /// Zero alloc, no closure, independent per-group views (safe out-of-order access).
    /// </summary>
    public class GroupForeachEnumeratorTests
    {
        private static byte[] Encode(long instrumentId, OB.BidsData[] bids, OB.AsksData[] asks)
        {
            var buffer = new byte[1024];
            Assert.True(OB.TryEncode(new OB { InstrumentId = instrumentId }, buffer, bids, asks, out int written));
            Array.Resize(ref buffer, written);
            return buffer;
        }

        [Fact]
        public void Foreach_IteratesBidsAndAsks_InDeclarationOrder()
        {
            var bids = new[]
            {
                new OB.BidsData { Price = 1000, Quantity = 10 },
                new OB.BidsData { Price = 1010, Quantity = 11 },
            };
            var asks = new[]
            {
                new OB.AsksData { Price = 2000, Quantity = 20 },
                new OB.AsksData { Price = 2010, Quantity = 21 },
                new OB.AsksData { Price = 2020, Quantity = 22 },
            };
            var buffer = Encode(7, bids, asks);

            Assert.True(OB.TryParse(buffer, out var reader));

            var bidPrices = new List<long>();
            foreach (ref readonly var b in reader.Bids)
                bidPrices.Add(b.Price.Value);
            Assert.Equal(new[] { 1000L, 1010L }, bidPrices);

            var askPrices = new List<long>();
            foreach (ref readonly var a in reader.Asks)
                askPrices.Add(a.Price.Value);
            Assert.Equal(new[] { 2000L, 2010L, 2020L }, askPrices);
        }

        [Fact]
        public void Foreach_OutOfOrderAccess_StillWorks()
        {
            // Independent per-group views: accessing Asks before Bids must not corrupt state.
            var bids = new[] { new OB.BidsData { Price = 100, Quantity = 1 } };
            var asks = new[] { new OB.AsksData { Price = 200, Quantity = 2 } };
            var buffer = Encode(1, bids, asks);

            Assert.True(OB.TryParse(buffer, out var reader));

            int askCount = 0;
            foreach (ref readonly var a in reader.Asks)
            {
                askCount++;
                Assert.Equal(200L, a.Price.Value);
            }

            int bidCount = 0;
            foreach (ref readonly var b in reader.Bids)
            {
                bidCount++;
                Assert.Equal(100L, b.Price.Value);
            }

            Assert.Equal(1, askCount);
            Assert.Equal(1, bidCount);
        }

        [Fact]
        public void Foreach_RepeatedIteration_WorksOnFreshAccessor()
        {
            var bids = new[]
            {
                new OB.BidsData { Price = 1, Quantity = 1 },
                new OB.BidsData { Price = 2, Quantity = 2 },
            };
            var buffer = Encode(0, bids, Array.Empty<OB.AsksData>());

            Assert.True(OB.TryParse(buffer, out var reader));

            int sum1 = 0;
            foreach (ref readonly var b in reader.Bids) sum1 += (int)b.Price.Value;
            int sum2 = 0;
            foreach (ref readonly var b in reader.Bids) sum2 += (int)b.Price.Value;

            Assert.Equal(3, sum1);
            Assert.Equal(sum1, sum2);
        }

        [Fact]
        public void Foreach_EmptyGroups_YieldNothing()
        {
            var buffer = Encode(99, Array.Empty<OB.BidsData>(), Array.Empty<OB.AsksData>());

            Assert.True(OB.TryParse(buffer, out var reader));

            int bidCount = 0;
            foreach (ref readonly var _ in reader.Bids) bidCount++;
            int askCount = 0;
            foreach (ref readonly var _ in reader.Asks) askCount++;

            Assert.Equal(0, bidCount);
            Assert.Equal(0, askCount);
        }

        [Fact]
        public void Foreach_EarlyBreak_DoesNotCorruptOtherGroup()
        {
            var bids = new[]
            {
                new OB.BidsData { Price = 1, Quantity = 1 },
                new OB.BidsData { Price = 2, Quantity = 2 },
                new OB.BidsData { Price = 3, Quantity = 3 },
            };
            var asks = new[] { new OB.AsksData { Price = 999, Quantity = 9 } };
            var buffer = Encode(0, bids, asks);

            Assert.True(OB.TryParse(buffer, out var reader));

            // Break early on bids
            foreach (ref readonly var b in reader.Bids)
            {
                if (b.Price.Value == 1) break;
            }

            // Asks should still be intact
            int askCount = 0;
            foreach (ref readonly var a in reader.Asks)
            {
                askCount++;
                Assert.Equal(999L, a.Price.Value);
            }
            Assert.Equal(1, askCount);
        }

        [Fact]
        public void Foreach_MutatesEnumerableLocally_ReaderRemainsUsable()
        {
            // BytesConsumed is set to BlockLength initially and only updated by ReadGroups.
            // The foreach API must NOT mutate reader state.
            var bids = new[] { new OB.BidsData { Price = 1, Quantity = 1 } };
            var buffer = Encode(0, bids, Array.Empty<OB.AsksData>());

            Assert.True(OB.TryParse(buffer, out var reader));
            int bytesBefore = reader.BytesConsumed;

            foreach (ref readonly var _ in reader.Bids) { }
            foreach (ref readonly var _ in reader.Asks) { }

            Assert.Equal(bytesBefore, reader.BytesConsumed);

            // ReadGroups still works after foreach (idempotent)
            int bidCallbacks = 0;
            reader.ReadGroups(
                (in OB.BidsData _) => bidCallbacks++,
                (in OB.AsksData _) => { });
            Assert.Equal(1, bidCallbacks);
        }
    }
}
