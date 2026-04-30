using System;
using System.Runtime.InteropServices;
using SbeSourceGenerator.Runtime;
using Semantic.Types.Test.V0;
using Xunit;

// Issue #166: register a user-defined converter for a semanticType not in the built-in set.
[assembly: SbeSemanticType("MyCustomStatus", typeof(SbeCodeGenerator.IntegrationTests.MyCustomStatusConverter))]

namespace SbeCodeGenerator.IntegrationTests
{
    public enum MyStatus : byte
    {
        Unknown = 0,
        Active = 1,
        Suspended = 2,
        Closed = 3,
    }

    public sealed class MyCustomStatusConverter : ISbeSemanticConverter<byte, MyStatus>
    {
        public static MyStatus FromWire(byte wire) => (MyStatus)wire;
        public static byte ToWire(MyStatus semantic) => (byte)semantic;
    }

    /// <summary>
    /// Issue #166: end-to-end coverage for the semantic-type registry. Verifies built-in
    /// converters generate <c>{Field}Value</c> accessors with correct conversions, optional
    /// fields produce nullable accessors, fields without a registered semanticType get no
    /// extra accessor, and user-registered converters are honoured.
    /// </summary>
    public class SemanticTypeRegistryTests
    {
        [Fact]
        public void UTCTimestampNanos_BuiltIn_ProducesDateTimeAccessor()
        {
            Span<byte> buffer = stackalloc byte[TimedEventData.MESSAGE_SIZE];
            ref var msg = ref MemoryMarshal.AsRef<TimedEventData>(buffer);

            // 2024-01-15T12:34:56.789Z, in nanoseconds since UNIX epoch.
            var expected = new DateTime(2024, 1, 15, 12, 34, 56, 789, DateTimeKind.Utc);
            ulong wireNanos = (ulong)((expected - DateTime.UnixEpoch).Ticks * 100L);
            msg.TransactTime = wireNanos;

            // Built-in UtcTimestampNanosConverter truncates to 100ns ticks (DateTime resolution).
            Assert.Equal(expected, msg.TransactTimeValue);
        }

        [Fact]
        public void UTCTimestampMicros_BuiltIn_ProducesDateTimeAccessor()
        {
            Span<byte> buffer = stackalloc byte[TimedEventData.MESSAGE_SIZE];
            ref var msg = ref MemoryMarshal.AsRef<TimedEventData>(buffer);

            var expected = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            msg.ExchangeTime = (ulong)(expected - DateTime.UnixEpoch).TotalMicroseconds;

            Assert.Equal(expected, msg.ExchangeTimeValue);
        }

        [Fact]
        public void UTCDateOnly_BuiltIn_ProducesDateOnlyAccessor()
        {
            Span<byte> buffer = stackalloc byte[TimedEventData.MESSAGE_SIZE];
            ref var msg = ref MemoryMarshal.AsRef<TimedEventData>(buffer);

            var expected = new DateOnly(2024, 1, 15);
            msg.BookingDate = (ushort)(expected.DayNumber - new DateOnly(1970, 1, 1).DayNumber);

            Assert.Equal(expected, msg.BookingDateValue);
        }

        [Fact]
        public void LocalMktDate_BuiltIn_ProducesDateOnlyAccessor()
        {
            Span<byte> buffer = stackalloc byte[TimedEventData.MESSAGE_SIZE];
            ref var msg = ref MemoryMarshal.AsRef<TimedEventData>(buffer);

            var expected = new DateOnly(2030, 12, 31);
            msg.SettlementDate = (ushort)(expected.DayNumber - new DateOnly(1970, 1, 1).DayNumber);

            Assert.Equal(expected, msg.SettlementDateValue);
        }

        [Fact]
        public void MonthYear_BuiltIn_ProducesTupleAccessor()
        {
            Span<byte> buffer = stackalloc byte[TimedEventData.MESSAGE_SIZE];
            ref var msg = ref MemoryMarshal.AsRef<TimedEventData>(buffer);

            msg.ContractMonth = 202407u;
            var (year, month) = msg.ContractMonthValue;
            Assert.Equal(2024, year);
            Assert.Equal(7, month);
        }

        [Fact]
        public void OptionalSemantic_NullSentinel_ReturnsNull()
        {
            Span<byte> buffer = stackalloc byte[TimedEventData.MESSAGE_SIZE];
            ref var msg = ref MemoryMarshal.AsRef<TimedEventData>(buffer);

            msg.SetOptionalNanos(null);
            Assert.Null(msg.OptionalNanosValue);

            var when = new DateTime(2024, 6, 15, 10, 11, 12, DateTimeKind.Utc);
            msg.SetOptionalNanos((ulong)((when - DateTime.UnixEpoch).Ticks * 100L));
            Assert.NotNull(msg.OptionalNanosValue);
            Assert.Equal(when, msg.OptionalNanosValue!.Value);
        }

        [Fact]
        public void UserRegisteredConverter_ProducesTypedAccessor()
        {
            Span<byte> buffer = stackalloc byte[TimedEventData.MESSAGE_SIZE];
            ref var msg = ref MemoryMarshal.AsRef<TimedEventData>(buffer);

            msg.CustomStatus = (byte)MyStatus.Suspended;
            Assert.Equal(MyStatus.Suspended, msg.CustomStatusValue);
        }

        [Fact]
        public void RawAccessor_StillAvailable_ForAllSemanticFields()
        {
            // The registry NEVER replaces the raw wire accessor; it only adds a sibling.
            Span<byte> buffer = stackalloc byte[TimedEventData.MESSAGE_SIZE];
            ref var msg = ref MemoryMarshal.AsRef<TimedEventData>(buffer);

            msg.TransactTime = 12345UL;
            Assert.Equal(12345UL, msg.TransactTime);
            msg.ContractMonth = 202401u;
            Assert.Equal(202401u, msg.ContractMonth);
        }
    }
}
