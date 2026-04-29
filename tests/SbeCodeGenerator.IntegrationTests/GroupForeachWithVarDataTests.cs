using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using NO = Cross.Schema.Orders.V0.NewOrderData;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Issue #156 follow-up: coverage for the mixed scenario — message with
    /// simple top-level groups AND top-level varData. The foreach API must be
    /// emitted for the groups (since they're simple), independent of the varData
    /// that follows them. ReadGroups continues to work for both.
    /// </summary>
    public class GroupForeachWithVarDataTests
    {
        private static byte[] Encode(NO order, NO.LegsData[] legs, string clientOrderId)
        {
            var buffer = new byte[1024];
            var coid = Encoding.UTF8.GetBytes(clientOrderId);
            Assert.True(NO.TryEncode(order, buffer, legs, coid, out int written));
            Array.Resize(ref buffer, written);
            return buffer;
        }

        [Fact]
        public void Foreach_OnSimpleGroup_WhenMessageAlsoHasTopLevelVarData()
        {
            var order = new NO { OrderId = 42, Quantity = 100, Side = Cross.Schema.Orders.V0.Side.Buy };
            var legs = new[]
            {
                new NO.LegsData { LegSymbol = 1, LegRatio = 1, LegSide = Cross.Schema.Orders.V0.Side.Buy },
                new NO.LegsData { LegSymbol = 2, LegRatio = 2, LegSide = Cross.Schema.Orders.V0.Side.Sell },
                new NO.LegsData { LegSymbol = 3, LegRatio = 3, LegSide = Cross.Schema.Orders.V0.Side.Buy },
            };
            var buffer = Encode(order, legs, "CLIENT-ORDER-XYZ");

            Assert.True(NO.TryParse(buffer, out var reader));

            var symbols = new List<uint>();
            foreach (ref readonly var leg in reader.Legs)
                symbols.Add(leg.LegSymbol);

            Assert.Equal(new uint[] { 1, 2, 3 }, symbols);
        }

        [Fact]
        public void Foreach_DoesNotConsumeVarData_ReadGroupsStillWorks()
        {
            // After foreach iterating Legs, the top-level ClientOrderId varData
            // must still be reachable via ReadGroups (which scans from _blockLength
            // through its own SpanReader — independent of the foreach state).
            var order = new NO { OrderId = 7 };
            var legs = new[] { new NO.LegsData { LegSymbol = 99, LegRatio = 1 } };
            var buffer = Encode(order, legs, "PERSISTED-COID");

            Assert.True(NO.TryParse(buffer, out var reader));

            int legsViaForeach = 0;
            foreach (ref readonly var _ in reader.Legs) legsViaForeach++;

            int legsViaReadGroups = 0;
            string? receivedCoid = null;
            reader.ReadGroups(
                (in NO.LegsData _) => legsViaReadGroups++,
                clientOrderId =>
                {
                    receivedCoid = Encoding.UTF8.GetString(clientOrderId.VarData);
                });

            Assert.Equal(1, legsViaForeach);
            Assert.Equal(1, legsViaReadGroups);
            Assert.Equal("PERSISTED-COID", receivedCoid);
        }

        [Fact]
        public void Foreach_EmptyGroup_WithVarDataStillReadable()
        {
            var order = new NO { OrderId = 1 };
            var buffer = Encode(order, Array.Empty<NO.LegsData>(), "EMPTY-LEGS-OK");

            Assert.True(NO.TryParse(buffer, out var reader));

            int count = 0;
            foreach (ref readonly var _ in reader.Legs) count++;
            Assert.Equal(0, count);

            string? coid = null;
            reader.ReadGroups(
                (in NO.LegsData _) => { },
                cid => coid = Encoding.UTF8.GetString(cid.VarData));
            Assert.Equal("EMPTY-LEGS-OK", coid);
        }

        [Fact]
        public void DirectVarDataProperty_AfterSimpleGroups_ReturnsSameBytesAsCallback()
        {
            // Issue #162: zero-alloc direct access to top-level varData via property.
            // Must yield identical bytes as the callback path, regardless of access order.
            var order = new NO { OrderId = 99, Quantity = 50 };
            var legs = new[]
            {
                new NO.LegsData { LegSymbol = 10, LegRatio = 1 },
                new NO.LegsData { LegSymbol = 20, LegRatio = 2 },
            };
            var buffer = Encode(order, legs, "DIRECT-PROP-COID");

            Assert.True(NO.TryParse(buffer, out var reader));

            // Direct property — no closure, no callback, computed from buffer offsets.
            var coidBytes = reader.ClientOrderId.VarData.ToArray();
            Assert.Equal("DIRECT-PROP-COID", Encoding.UTF8.GetString(coidBytes));

            // Property is stateless: accessing again (and even before iterating groups) is safe.
            var coidBytes2 = reader.ClientOrderId.VarData.ToArray();
            Assert.Equal(coidBytes, coidBytes2);

            // Iterate groups afterwards — direct property must still resolve correctly.
            int count = 0;
            foreach (ref readonly var _ in reader.Legs) count++;
            Assert.Equal(2, count);

            var coidBytes3 = reader.ClientOrderId.VarData.ToArray();
            Assert.Equal(coidBytes, coidBytes3);
        }

        [Fact]
        public void DirectVarDataProperty_WithEmptyGroup_ResolvesCorrectly()
        {
            var order = new NO { OrderId = 1 };
            var buffer = Encode(order, Array.Empty<NO.LegsData>(), "EMPTY-DIRECT");

            Assert.True(NO.TryParse(buffer, out var reader));

            Assert.Equal("EMPTY-DIRECT", Encoding.UTF8.GetString(reader.ClientOrderId.VarData));
        }
    }
}
