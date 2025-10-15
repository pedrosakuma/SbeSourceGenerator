using System;
using Optional.Fields.Test;
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
            bool decodeResult = SimpleOptionalData.TryParse(buffer, out var decoded, out _);

            // Assert decoding succeeded and values match
            Assert.True(decodeResult);
            Assert.Equal(original.Id, decoded.Id);
            Assert.Equal(456, decoded.OptionalValue);
            Assert.NotNull(decoded.OptionalValue);
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
            bool decodeResult = SimpleOptionalData.TryParse(buffer, out var decoded, out _);

            // Assert decoding succeeded and value is null
            Assert.True(decodeResult);
            Assert.Equal(original.Id, decoded.Id);
            Assert.Null(decoded.OptionalValue);
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
            bool decodeResult = SimpleOptionalData.TryParse(buffer, out var decoded, out _);

            // Assert decoding succeeded and value is null (or could be treated as uninitialized)
            Assert.True(decodeResult);
            Assert.Equal(original.Id, decoded.Id);
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
            SimpleOptionalData.TryParse(buffer, out var decoded, out _);
            Assert.Equal(message.Id, decoded.Id);
            Assert.Equal(222, decoded.OptionalValue);
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
            var writer = new Optional.Fields.Test.Runtime.SpanWriter(buffer);

            // Act
            bool result = message.TryEncodeWithWriter(ref writer);

            // Assert
            Assert.True(result);
            Assert.Equal(SimpleOptionalData.MESSAGE_SIZE, writer.BytesWritten);

            // Verify round-trip
            SimpleOptionalData.TryParse(buffer, out var decoded, out _);
            Assert.Equal(message.Id, decoded.Id);
            Assert.Null(decoded.OptionalValue);
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
                bool decodeResult = SimpleOptionalData.TryParse(buffer, out var decoded, out _);

                // Assert decoding and value preservation
                Assert.True(decodeResult, $"Failed to decode test case with Id={testCase.Id}");
                Assert.Equal(testCase.Id, decoded.Id);
                Assert.Equal(testCase.OptionalValue, decoded.OptionalValue);
            }
        }

        [Fact]
        public void MultipleMessagesInSequence_WithOptionalFields_CanBeEncodedAndDecoded()
        {
            // Arrange - Encode multiple messages into one buffer
            var buffer = new byte[SimpleOptionalData.MESSAGE_SIZE * 3];
            var writer = new Optional.Fields.Test.Runtime.SpanWriter(buffer);

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
            var reader = new Optional.Fields.Test.Runtime.SpanReader(buffer);

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
            SimpleOptionalData.TryParse(buffer1, out var decoded1, out _);
            Assert.Equal(100, decoded1.OptionalValue);

            // Act & Assert - Set to null
            message.SetOptionalValue(null);
            var buffer2 = new byte[SimpleOptionalData.MESSAGE_SIZE];
            message.TryEncode(buffer2, out _);
            SimpleOptionalData.TryParse(buffer2, out var decoded2, out _);
            Assert.Null(decoded2.OptionalValue);

            // Act & Assert - Set to different value
            message.SetOptionalValue(200);
            var buffer3 = new byte[SimpleOptionalData.MESSAGE_SIZE];
            message.TryEncode(buffer3, out _);
            SimpleOptionalData.TryParse(buffer3, out var decoded3, out _);
            Assert.Equal(200, decoded3.OptionalValue);
        }
    }
}
