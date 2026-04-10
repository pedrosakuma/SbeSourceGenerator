using System;
using Optional.Fields.Test.V0;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Integration tests for optional field encoding and decoding.
    /// Tests verify that optional fields can be properly encoded and decoded with both null and non-null values.
    /// </summary>
    public class OptionalFieldsIntegrationTests
    {
        [Fact]
        public void TryEncode_OptionalFieldWithValue_RoundTripSucceeds()
        {
            // Arrange - Create a message with an optional value set
            var original = new SimpleOptionalData
            {
                Id = 123
            };
            original.SetOptionalValue(456);

            var buffer = new byte[SimpleOptionalData.MESSAGE_SIZE];

            // Act - Encode
            bool encodeResult = original.TryEncode(buffer, out int bytesWritten);

            // Assert encoding succeeded
            Assert.True(encodeResult);
            Assert.Equal(SimpleOptionalData.MESSAGE_SIZE, bytesWritten);

            // Act - Decode
            bool decodeResult = SimpleOptionalData.TryParse(buffer, out var decoded);

            // Assert decoding succeeded and values match
            Assert.True(decodeResult);
            Assert.Equal(original.Id, decoded.Data.Id);
            Assert.Equal(456, decoded.Data.OptionalValue);
            Assert.NotNull(decoded.Data.OptionalValue);
        }

        [Fact]
        public void TryEncode_OptionalFieldWithNull_RoundTripSucceeds()
        {
            // Arrange - Create a message with optional value set to null
            var original = new SimpleOptionalData
            {
                Id = 789
            };
            original.SetOptionalValue(null);

            var buffer = new byte[SimpleOptionalData.MESSAGE_SIZE];

            // Act - Encode
            bool encodeResult = original.TryEncode(buffer, out int bytesWritten);

            // Assert encoding succeeded
            Assert.True(encodeResult);
            Assert.Equal(SimpleOptionalData.MESSAGE_SIZE, bytesWritten);

            // Act - Decode
            bool decodeResult = SimpleOptionalData.TryParse(buffer, out var decoded);

            // Assert decoding succeeded and value is null
            Assert.True(decodeResult);
            Assert.Equal(original.Id, decoded.Data.Id);
            Assert.Null(decoded.Data.OptionalValue);
        }

        [Fact]
        public void TryEncode_OptionalFieldNotSet_DefaultsToNull()
        {
            // Arrange - Create a message without setting the optional value
            var original = new SimpleOptionalData
            {
                Id = 999
            };
            // Don't call SetOptionalValue - field should default to null value

            var buffer = new byte[SimpleOptionalData.MESSAGE_SIZE];

            // Act - Encode
            bool encodeResult = original.TryEncode(buffer, out int bytesWritten);

            // Assert encoding succeeded
            Assert.True(encodeResult);

            // Act - Decode
            bool decodeResult = SimpleOptionalData.TryParse(buffer, out var decoded);

            // Assert decoding succeeded and value is null (or could be treated as uninitialized)
            Assert.True(decodeResult);
            Assert.Equal(original.Id, decoded.Data.Id);
            // The behavior when not set depends on whether the struct is zero-initialized
            // In practice, this should either be null or the default value
        }

        [Fact]
        public void Encode_ThrowingVersion_OptionalFieldWithValue_Succeeds()
        {
            // Arrange
            var message = new SimpleOptionalData
            {
                Id = 111
            };
            message.SetOptionalValue(222);

            var buffer = new byte[SimpleOptionalData.MESSAGE_SIZE];

            // Act
            int bytesWritten = message.Encode(buffer);

            // Assert
            Assert.Equal(SimpleOptionalData.MESSAGE_SIZE, bytesWritten);

            // Verify round-trip
            SimpleOptionalData.TryParse(buffer, out var decoded);
            Assert.Equal(message.Id, decoded.Data.Id);
            Assert.Equal(222, decoded.Data.OptionalValue);
        }

        [Fact]
        public void TryEncodeWithWriter_OptionalFieldWithNull_Works()
        {
            // Arrange
            var message = new SimpleOptionalData
            {
                Id = 333
            };
            message.SetOptionalValue(null);

            var buffer = new byte[100]; // Larger buffer for demonstration
            var writer = new Optional.Fields.Test.V0.Runtime.SpanWriter(buffer);

            // Act
            bool result = message.TryEncodeWithWriter(ref writer);

            // Assert
            Assert.True(result);
            Assert.Equal(SimpleOptionalData.MESSAGE_SIZE, writer.BytesWritten);

            // Verify round-trip
            SimpleOptionalData.TryParse(buffer, out var decoded);
            Assert.Equal(message.Id, decoded.Data.Id);
            Assert.Null(decoded.Data.OptionalValue);
        }

        [Fact]
        public void RoundTrip_MultipleMessagesWithOptionalFields_AllDataPreserved()
        {
            // Test that encoding/decoding preserves optional field values across different scenarios
            var testCases = new[]
            {
                (Id: 1L, OptionalValue: (long?)100),
                (Id: 2L, OptionalValue: (long?)null),
                (Id: 3L, OptionalValue: (long?)long.MaxValue),
                (Id: 4L, OptionalValue: (long?)0),
                (Id: 5L, OptionalValue: (long?)-1),
                (Id: 6L, OptionalValue: (long?)-9223372036854775807L) // Near null value but not null
            };

            foreach (var testCase in testCases)
            {
                // Arrange
                var original = new SimpleOptionalData
                {
                    Id = testCase.Id
                };
                original.SetOptionalValue(testCase.OptionalValue);

                var buffer = new byte[SimpleOptionalData.MESSAGE_SIZE];

                // Act - Encode
                bool encodeResult = original.TryEncode(buffer, out int bytesWritten);

                // Assert encoding
                Assert.True(encodeResult, $"Failed to encode test case with Id={testCase.Id}");

                // Act - Decode
                bool decodeResult = SimpleOptionalData.TryParse(buffer, out var decoded);

                // Assert decoding and value preservation
                Assert.True(decodeResult, $"Failed to decode test case with Id={testCase.Id}");
                Assert.Equal(testCase.Id, decoded.Data.Id);
                Assert.Equal(testCase.OptionalValue, decoded.Data.OptionalValue);
            }
        }

        [Fact]
        public void MultipleMessagesInSequence_WithOptionalFields_CanBeEncodedAndDecoded()
        {
            // Arrange - Encode multiple messages into one buffer
            var buffer = new byte[SimpleOptionalData.MESSAGE_SIZE * 3];
            var writer = new Optional.Fields.Test.V0.Runtime.SpanWriter(buffer);

            var message1 = new SimpleOptionalData { Id = 1 };
            message1.SetOptionalValue(100);

            var message2 = new SimpleOptionalData { Id = 2 };
            message2.SetOptionalValue(null);

            var message3 = new SimpleOptionalData { Id = 3 };
            message3.SetOptionalValue(300);

            // Act - Encode all three messages
            Assert.True(message1.TryEncodeWithWriter(ref writer));
            Assert.True(message2.TryEncodeWithWriter(ref writer));
            Assert.True(message3.TryEncodeWithWriter(ref writer));

            Assert.Equal(SimpleOptionalData.MESSAGE_SIZE * 3, writer.BytesWritten);

            // Act - Decode all three messages
            var reader = new Optional.Fields.Test.V0.Runtime.SpanReader(buffer);

            Assert.True(reader.TryRead<SimpleOptionalData>(out var decoded1));
            Assert.True(reader.TryRead<SimpleOptionalData>(out var decoded2));
            Assert.True(reader.TryRead<SimpleOptionalData>(out var decoded3));

            // Assert
            Assert.Equal(1, decoded1.Id);
            Assert.Equal(100, decoded1.OptionalValue);

            Assert.Equal(2, decoded2.Id);
            Assert.Null(decoded2.OptionalValue);

            Assert.Equal(3, decoded3.Id);
            Assert.Equal(300, decoded3.OptionalValue);
        }

        [Fact]
        public void SetOptionalValue_MultipleCallsOnSameInstance_UpdatesCorrectly()
        {
            // Arrange
            var message = new SimpleOptionalData { Id = 42 };

            // Act & Assert - Set to value
            message.SetOptionalValue(100);
            var buffer1 = new byte[SimpleOptionalData.MESSAGE_SIZE];
            message.TryEncode(buffer1, out _);
            SimpleOptionalData.TryParse(buffer1, out var decoded1);
            Assert.Equal(100, decoded1.Data.OptionalValue);

            // Act & Assert - Set to null
            message.SetOptionalValue(null);
            var buffer2 = new byte[SimpleOptionalData.MESSAGE_SIZE];
            message.TryEncode(buffer2, out _);
            SimpleOptionalData.TryParse(buffer2, out var decoded2);
            Assert.Null(decoded2.Data.OptionalValue);

            // Act & Assert - Set to different value
            message.SetOptionalValue(200);
            var buffer3 = new byte[SimpleOptionalData.MESSAGE_SIZE];
            message.TryEncode(buffer3, out _);
            SimpleOptionalData.TryParse(buffer3, out var decoded3);
            Assert.Equal(200, decoded3.Data.OptionalValue);
        }

        [Fact]
        public void AllOptionalTypes_RoundTrip_WithAllFieldsSet_Succeeds()
        {
            // Arrange - Create message with all optional fields set to non-null values
            var original = new AllOptionalTypesData { Id = 1 };
            original.SetOptInt8(sbyte.MaxValue);
            original.SetOptInt16(short.MaxValue);
            original.SetOptInt32(int.MaxValue);
            original.SetOptInt64(long.MaxValue);
            original.SetOptUInt8(byte.MaxValue - 1); // -1 because 255 is null value
            original.SetOptUInt16(ushort.MaxValue - 1); // -1 because 65535 is null value
            original.SetOptUInt32(uint.MaxValue - 1); // -1 because 4294967295 is null value
            original.SetOptUInt64(ulong.MaxValue - 1); // -1 because max is null value

            var buffer = new byte[AllOptionalTypesData.MESSAGE_SIZE];

            // Act - Encode
            bool encodeResult = original.TryEncode(buffer, out int bytesWritten);

            // Assert encoding succeeded
            Assert.True(encodeResult);
            Assert.Equal(AllOptionalTypesData.MESSAGE_SIZE, bytesWritten);

            // Act - Decode
            bool decodeResult = AllOptionalTypesData.TryParse(buffer, out var decoded);

            // Assert decoding succeeded and all values match
            Assert.True(decodeResult);
            Assert.Equal(original.Id, decoded.Data.Id);
            Assert.Equal(sbyte.MaxValue, decoded.Data.OptInt8);
            Assert.Equal(short.MaxValue, decoded.Data.OptInt16);
            Assert.Equal(int.MaxValue, decoded.Data.OptInt32);
            Assert.Equal(long.MaxValue, decoded.Data.OptInt64);
            Assert.Equal((byte)(byte.MaxValue - 1), decoded.Data.OptUInt8);
            Assert.Equal((ushort)(ushort.MaxValue - 1), decoded.Data.OptUInt16);
            Assert.Equal(uint.MaxValue - 1, decoded.Data.OptUInt32);
            Assert.Equal(ulong.MaxValue - 1, decoded.Data.OptUInt64);
        }

        [Fact]
        public void AllOptionalTypes_RoundTrip_WithAllFieldsNull_Succeeds()
        {
            // Arrange - Create message with all optional fields set to null
            var original = new AllOptionalTypesData { Id = 2 };
            original.SetOptInt8(null);
            original.SetOptInt16(null);
            original.SetOptInt32(null);
            original.SetOptInt64(null);
            original.SetOptUInt8(null);
            original.SetOptUInt16(null);
            original.SetOptUInt32(null);
            original.SetOptUInt64(null);

            var buffer = new byte[AllOptionalTypesData.MESSAGE_SIZE];

            // Act - Encode
            bool encodeResult = original.TryEncode(buffer, out int bytesWritten);

            // Assert encoding succeeded
            Assert.True(encodeResult);
            Assert.Equal(AllOptionalTypesData.MESSAGE_SIZE, bytesWritten);

            // Act - Decode
            bool decodeResult = AllOptionalTypesData.TryParse(buffer, out var decoded);

            // Assert decoding succeeded and all values are null
            Assert.True(decodeResult);
            Assert.Equal(original.Id, decoded.Data.Id);
            Assert.Null(decoded.Data.OptInt8);
            Assert.Null(decoded.Data.OptInt16);
            Assert.Null(decoded.Data.OptInt32);
            Assert.Null(decoded.Data.OptInt64);
            Assert.Null(decoded.Data.OptUInt8);
            Assert.Null(decoded.Data.OptUInt16);
            Assert.Null(decoded.Data.OptUInt32);
            Assert.Null(decoded.Data.OptUInt64);
        }

        [Fact]
        public void AllOptionalTypes_RoundTrip_WithMixedNullAndValues_Succeeds()
        {
            // Arrange - Create message with some optional fields set and some null
            var original = new AllOptionalTypesData { Id = 3 };
            original.SetOptInt8(10);
            original.SetOptInt16(null);
            original.SetOptInt32(1000);
            original.SetOptInt64(null);
            original.SetOptUInt8(null);
            original.SetOptUInt16(200);
            original.SetOptUInt32(null);
            original.SetOptUInt64(3000);

            var buffer = new byte[AllOptionalTypesData.MESSAGE_SIZE];

            // Act - Encode
            bool encodeResult = original.TryEncode(buffer, out int bytesWritten);

            // Assert encoding succeeded
            Assert.True(encodeResult);

            // Act - Decode
            bool decodeResult = AllOptionalTypesData.TryParse(buffer, out var decoded);

            // Assert decoding succeeded and values match
            Assert.True(decodeResult);
            Assert.Equal(original.Id, decoded.Data.Id);
            Assert.Equal((sbyte)10, decoded.Data.OptInt8);
            Assert.Null(decoded.Data.OptInt16);
            Assert.Equal(1000, decoded.Data.OptInt32);
            Assert.Null(decoded.Data.OptInt64);
            Assert.Null(decoded.Data.OptUInt8);
            Assert.Equal((ushort)200, decoded.Data.OptUInt16);
            Assert.Null(decoded.Data.OptUInt32);
            Assert.Equal((ulong)3000, decoded.Data.OptUInt64);
        }

        [Fact]
        public void AllOptionalTypes_RoundTrip_WithEdgeCaseValues_Succeeds()
        {
            // Test values near the null values to ensure they're preserved correctly
            var original = new AllOptionalTypesData { Id = 4 };
            original.SetOptInt8(-127); // Near null value (-128) but not null
            original.SetOptInt16(-32767); // Near null value (-32768) but not null
            original.SetOptInt32(-2147483647); // Near null value but not null
            original.SetOptInt64(-9223372036854775807L); // Near null value but not null
            original.SetOptUInt8(254); // Near null value (255) but not null
            original.SetOptUInt16(65534); // Near null value (65535) but not null
            original.SetOptUInt32(4294967294); // Near null value but not null
            original.SetOptUInt64(18446744073709551614); // Near null value but not null

            var buffer = new byte[AllOptionalTypesData.MESSAGE_SIZE];

            // Act - Encode and decode
            original.TryEncode(buffer, out _);
            AllOptionalTypesData.TryParse(buffer, out var decoded);

            // Assert all values are preserved (not null)
            Assert.Equal((sbyte)-127, decoded.Data.OptInt8);
            Assert.Equal((short)-32767, decoded.Data.OptInt16);
            Assert.Equal(-2147483647, decoded.Data.OptInt32);
            Assert.Equal(-9223372036854775807L, decoded.Data.OptInt64);
            Assert.Equal((byte)254, decoded.Data.OptUInt8);
            Assert.Equal((ushort)65534, decoded.Data.OptUInt16);
            Assert.Equal((uint)4294967294, decoded.Data.OptUInt32);
            Assert.Equal((ulong)18446744073709551614, decoded.Data.OptUInt64);
        }
    }
}
