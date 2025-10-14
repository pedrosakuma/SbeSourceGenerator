using System;
using System.Runtime.InteropServices;
using SbeSourceGenerator.Runtime;
using Xunit;

namespace SbeCodeGenerator.Tests.Runtime
{
    public class SpanReaderTests
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TestStruct
        {
            public int Field1;
            public long Field2;
            public byte Field3;
        }

        [Fact]
        public void Constructor_InitializesWithBuffer()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            
            // Act
            var reader = new SpanReader(buffer);
            
            // Assert
            Assert.Equal(100, reader.RemainingBytes);
        }

        [Fact]
        public void RemainingBytes_ReturnsCorrectCount()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[50];
            var reader = new SpanReader(buffer);
            
            // Assert
            Assert.Equal(50, reader.RemainingBytes);
        }

        [Fact]
        public void CanRead_ReturnsTrueWhenEnoughBytes()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var reader = new SpanReader(buffer);
            
            // Act & Assert
            Assert.True(reader.CanRead(50));
            Assert.True(reader.CanRead(100));
        }

        [Fact]
        public void CanRead_ReturnsFalseWhenNotEnoughBytes()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var reader = new SpanReader(buffer);
            
            // Act & Assert
            Assert.False(reader.CanRead(101));
            Assert.False(reader.CanRead(200));
        }

        [Fact]
        public void TryRead_ReadsStructAndAdvancesPosition()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            ref var testData = ref MemoryMarshal.AsRef<TestStruct>(buffer);
            testData.Field1 = 42;
            testData.Field2 = 12345678;
            testData.Field3 = 99;
            
            var reader = new SpanReader(buffer);
            int initialSize = reader.RemainingBytes;
            
            // Act
            bool success = reader.TryRead<TestStruct>(out var result);
            
            // Assert
            Assert.True(success);
            Assert.Equal(42, result.Field1);
            Assert.Equal(12345678, result.Field2);
            Assert.Equal(99, result.Field3);
            Assert.Equal(initialSize - Marshal.SizeOf<TestStruct>(), reader.RemainingBytes);
        }

        [Fact]
        public void TryRead_ReturnsFalseWhenNotEnoughBytes()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[5]; // Too small for TestStruct
            var reader = new SpanReader(buffer);
            
            // Act
            bool success = reader.TryRead<TestStruct>(out var result);
            
            // Assert
            Assert.False(success);
            Assert.Equal(default(TestStruct), result);
            Assert.Equal(5, reader.RemainingBytes); // Position unchanged
        }

        [Fact]
        public void TryReadBytes_ReadsAndAdvancesPosition()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var reader = new SpanReader(buffer);
            
            // Act
            bool success = reader.TryReadBytes(3, out var bytes);
            
            // Assert
            Assert.True(success);
            Assert.Equal(3, bytes.Length);
            Assert.Equal(1, bytes[0]);
            Assert.Equal(2, bytes[1]);
            Assert.Equal(3, bytes[2]);
            Assert.Equal(5, reader.RemainingBytes);
        }

        [Fact]
        public void TryReadBytes_ReturnsFalseWhenNotEnoughBytes()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[5];
            var reader = new SpanReader(buffer);
            
            // Act
            bool success = reader.TryReadBytes(10, out var bytes);
            
            // Assert
            Assert.False(success);
            Assert.True(bytes.IsEmpty);
            Assert.Equal(5, reader.RemainingBytes); // Position unchanged
        }

        [Fact]
        public void TrySkip_SkipsAndAdvancesPosition()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var reader = new SpanReader(buffer);
            
            // Act
            bool success = reader.TrySkip(20);
            
            // Assert
            Assert.True(success);
            Assert.Equal(80, reader.RemainingBytes);
        }

        [Fact]
        public void TrySkip_ReturnsFalseWhenNotEnoughBytes()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[10];
            var reader = new SpanReader(buffer);
            
            // Act
            bool success = reader.TrySkip(20);
            
            // Assert
            Assert.False(success);
            Assert.Equal(10, reader.RemainingBytes); // Position unchanged
        }

        [Fact]
        public void TryPeek_PeeksWithoutAdvancingPosition()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            ref var testData = ref MemoryMarshal.AsRef<TestStruct>(buffer);
            testData.Field1 = 42;
            testData.Field2 = 12345678;
            testData.Field3 = 99;
            
            var reader = new SpanReader(buffer);
            int initialSize = reader.RemainingBytes;
            
            // Act
            bool success = reader.TryPeek<TestStruct>(out var result);
            
            // Assert
            Assert.True(success);
            Assert.Equal(42, result.Field1);
            Assert.Equal(12345678, result.Field2);
            Assert.Equal(99, result.Field3);
            Assert.Equal(initialSize, reader.RemainingBytes); // Position unchanged
        }

        [Fact]
        public void TryPeek_ReturnsFalseWhenNotEnoughBytes()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[5];
            var reader = new SpanReader(buffer);
            
            // Act
            bool success = reader.TryPeek<TestStruct>(out var result);
            
            // Assert
            Assert.False(success);
            Assert.Equal(default(TestStruct), result);
        }

        [Fact]
        public void TryPeekBytes_PeeksWithoutAdvancingPosition()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[] { 1, 2, 3, 4, 5 };
            var reader = new SpanReader(buffer);
            
            // Act
            bool success = reader.TryPeekBytes(3, out var bytes);
            
            // Assert
            Assert.True(success);
            Assert.Equal(3, bytes.Length);
            Assert.Equal(1, bytes[0]);
            Assert.Equal(2, bytes[1]);
            Assert.Equal(3, bytes[2]);
            Assert.Equal(5, reader.RemainingBytes); // Position unchanged
        }

        [Fact]
        public void TryPeekBytes_ReturnsFalseWhenNotEnoughBytes()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[5];
            var reader = new SpanReader(buffer);
            
            // Act
            bool success = reader.TryPeekBytes(10, out var bytes);
            
            // Assert
            Assert.False(success);
            Assert.True(bytes.IsEmpty);
        }

        [Fact]
        public void Reset_ResetsBufferToNewValue()
        {
            // Arrange
            Span<byte> buffer1 = stackalloc byte[100];
            Span<byte> buffer2 = stackalloc byte[50];
            var reader = new SpanReader(buffer1);
            
            // Act
            reader.Reset(buffer2);
            
            // Assert
            Assert.Equal(50, reader.RemainingBytes);
        }

        [Fact]
        public void SequentialReads_WorkCorrectly()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            
            // Write test data
            ref int val1 = ref MemoryMarshal.AsRef<int>(buffer);
            val1 = 42;
            ref long val2 = ref MemoryMarshal.AsRef<long>(buffer.Slice(4));
            val2 = 9876543210;
            ref byte val3 = ref MemoryMarshal.AsRef<byte>(buffer.Slice(12));
            val3 = 255;
            
            var reader = new SpanReader(buffer);
            
            // Act & Assert
            Assert.True(reader.TryRead<int>(out var read1));
            Assert.Equal(42, read1);
            Assert.Equal(96, reader.RemainingBytes);
            
            Assert.True(reader.TryRead<long>(out var read2));
            Assert.Equal(9876543210, read2);
            Assert.Equal(88, reader.RemainingBytes);
            
            Assert.True(reader.TryRead<byte>(out var read3));
            Assert.Equal(255, read3);
            Assert.Equal(87, reader.RemainingBytes);
        }

        [Fact]
        public void MixedOperations_WorkCorrectly()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var reader = new SpanReader(buffer);
            
            // Act & Assert - Peek doesn't advance
            Assert.True(reader.TryPeek<int>(out var peeked));
            Assert.Equal(10, reader.RemainingBytes);
            
            // Read advances
            Assert.True(reader.TryRead<int>(out var read1));
            Assert.Equal(6, reader.RemainingBytes);
            
            // Skip advances
            Assert.True(reader.TrySkip(2));
            Assert.Equal(4, reader.RemainingBytes);
            
            // Read bytes advances
            Assert.True(reader.TryReadBytes(2, out var bytes));
            Assert.Equal(2, reader.RemainingBytes);
        }

        [Fact]
        public void Remaining_ReturnsCorrectSpan()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[] { 1, 2, 3, 4, 5 };
            var reader = new SpanReader(buffer);
            
            // Act
            reader.TryRead<byte>(out _);
            reader.TryRead<byte>(out _);
            var remaining = reader.Remaining;
            
            // Assert
            Assert.Equal(3, remaining.Length);
            Assert.Equal(3, remaining[0]);
            Assert.Equal(4, remaining[1]);
            Assert.Equal(5, remaining[2]);
        }

        #region Custom Parser Tests (Schema Evolution & Extensibility)

        private struct VersionedMessage
        {
            public ushort Version;
            public int Value;
            public long ExtendedValue; // Only in version 2+
        }

        [Fact]
        public void TryReadWith_CustomParser_ParsesSuccessfully()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            ushort version = 1;
            int value = 42;
            MemoryMarshal.Write(buffer, in version);
            MemoryMarshal.Write(buffer.Slice(2), in value);

            var reader = new SpanReader(buffer);

            // Custom parser that reads version-specific data
            static bool ParseVersionedMessage(ReadOnlySpan<byte> buf, out VersionedMessage msg, out int consumed)
            {
                if (buf.Length < 6)
                {
                    msg = default;
                    consumed = 0;
                    return false;
                }

                msg = new VersionedMessage
                {
                    Version = MemoryMarshal.Read<ushort>(buf),
                    Value = MemoryMarshal.Read<int>(buf.Slice(2))
                };
                consumed = 6;
                return true;
            }

            // Act
            bool success = reader.TryReadWith<VersionedMessage>(ParseVersionedMessage, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(1, result.Version);
            Assert.Equal(42, result.Value);
            Assert.Equal(94, reader.RemainingBytes); // 100 - 6
        }

        [Fact]
        public void TryReadWith_CustomParser_HandlesSchemaEvolution()
        {
            // Arrange - Version 2 message with extended field
            Span<byte> buffer = stackalloc byte[100];
            ushort version = 2;
            int value = 42;
            long extendedValue = 9999;
            MemoryMarshal.Write(buffer, in version);
            MemoryMarshal.Write(buffer.Slice(2), in value);
            MemoryMarshal.Write(buffer.Slice(6), in extendedValue);

            var reader = new SpanReader(buffer);

            // Custom parser that handles schema evolution
            static bool ParseVersionedMessageV2(ReadOnlySpan<byte> buf, out VersionedMessage msg, out int consumed)
            {
                if (buf.Length < 2)
                {
                    msg = default;
                    consumed = 0;
                    return false;
                }

                ushort version = MemoryMarshal.Read<ushort>(buf);
                
                if (version == 1)
                {
                    if (buf.Length < 6)
                    {
                        msg = default;
                        consumed = 0;
                        return false;
                    }
                    msg = new VersionedMessage
                    {
                        Version = version,
                        Value = MemoryMarshal.Read<int>(buf.Slice(2))
                    };
                    consumed = 6;
                }
                else // version 2+
                {
                    if (buf.Length < 14)
                    {
                        msg = default;
                        consumed = 0;
                        return false;
                    }
                    msg = new VersionedMessage
                    {
                        Version = version,
                        Value = MemoryMarshal.Read<int>(buf.Slice(2)),
                        ExtendedValue = MemoryMarshal.Read<long>(buf.Slice(6))
                    };
                    consumed = 14;
                }
                
                return true;
            }

            // Act
            bool success = reader.TryReadWith<VersionedMessage>(ParseVersionedMessageV2, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(2, result.Version);
            Assert.Equal(42, result.Value);
            Assert.Equal(9999, result.ExtendedValue);
            Assert.Equal(86, reader.RemainingBytes); // 100 - 14
        }

        [Fact]
        public void TryReadWith_CustomParser_FailsWhenInsufficientData()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[3]; // Too small
            var reader = new SpanReader(buffer);

            static bool FailingParser(ReadOnlySpan<byte> buf, out int value, out int consumed)
            {
                value = 0;
                consumed = 0;
                return false;
            }

            // Act
            bool success = reader.TryReadWith<int>(FailingParser, out var result);

            // Assert
            Assert.False(success);
            Assert.Equal(3, reader.RemainingBytes); // Position unchanged
        }

        #endregion

        #region Alignment Tests

        [Fact]
        public void TryAlignFrom_AlignsToSpecifiedBoundary()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var reader = new SpanReader(buffer);

            // Read 3 bytes (unaligned position)
            reader.TryRead<byte>(out _);
            reader.TryRead<byte>(out _);
            reader.TryRead<byte>(out _);
            
            // Act - Align to 4-byte boundary from position 3
            bool success = reader.TryAlignFrom(4, 3);

            // Assert
            Assert.True(success);
            Assert.Equal(96, reader.RemainingBytes); // Should have skipped 1 byte to align to 4
        }

        [Fact]
        public void TryAlignFrom_AlignTo8ByteBoundary()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var reader = new SpanReader(buffer);

            // Read 5 bytes
            reader.TrySkip(5);
            
            // Act - Align to 8-byte boundary from position 5
            bool success = reader.TryAlignFrom(8, 5);

            // Assert
            Assert.True(success);
            Assert.Equal(92, reader.RemainingBytes); // Should have skipped 3 bytes (5 + 3 = 8)
        }

        [Fact]
        public void TryAlignFrom_NoAlignmentNeeded_WhenAlreadyAligned()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var reader = new SpanReader(buffer);

            // Read 8 bytes (already aligned to 8)
            reader.TrySkip(8);
            
            // Act - Align to 8-byte boundary from position 8
            bool success = reader.TryAlignFrom(8, 8);

            // Assert
            Assert.True(success);
            Assert.Equal(92, reader.RemainingBytes); // No additional bytes skipped
        }

        [Fact]
        public void TryAlignFrom_FailsWhenInsufficientBytes()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[10];
            var reader = new SpanReader(buffer);

            // Read 9 bytes, only 1 remaining
            reader.TrySkip(9);
            
            // Act - Try to align to 8-byte boundary (would need 7 more bytes)
            bool success = reader.TryAlignFrom(8, 9);

            // Assert
            Assert.False(success);
            Assert.Equal(1, reader.RemainingBytes); // Position unchanged
        }

        #endregion

        #region Non-Blittable Type Support Tests

        private struct NonBlittableData
        {
            public int Length;
            public byte Byte1;
            public byte Byte2;
            public byte Byte3;
            public byte Byte4;
            public byte Byte5;
        }

        [Fact]
        public void TryReadWith_SupportsNonBlittableTypes()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            int length = 5;
            MemoryMarshal.Write(buffer, in length); // Length prefix
            buffer[4] = (byte)'H';
            buffer[5] = (byte)'e';
            buffer[6] = (byte)'l';
            buffer[7] = (byte)'l';
            buffer[8] = (byte)'o';

            var reader = new SpanReader(buffer);

            // Custom parser for length-prefixed data
            static bool ParseData(ReadOnlySpan<byte> buf, out NonBlittableData data, out int consumed)
            {
                if (buf.Length < 4)
                {
                    data = default;
                    consumed = 0;
                    return false;
                }

                int length = MemoryMarshal.Read<int>(buf);
                if (buf.Length < 4 + length || length > 5)
                {
                    data = default;
                    consumed = 0;
                    return false;
                }

                data = new NonBlittableData { Length = length };
                if (length > 0) data.Byte1 = buf[4];
                if (length > 1) data.Byte2 = buf[5];
                if (length > 2) data.Byte3 = buf[6];
                if (length > 3) data.Byte4 = buf[7];
                if (length > 4) data.Byte5 = buf[8];
                
                consumed = 4 + length;
                return true;
            }

            // Act
            bool success = reader.TryReadWith<NonBlittableData>(ParseData, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal(5, result.Length);
            Assert.Equal((byte)'H', result.Byte1);
            Assert.Equal((byte)'e', result.Byte2);
            Assert.Equal((byte)'l', result.Byte3);
            Assert.Equal((byte)'l', result.Byte4);
            Assert.Equal((byte)'o', result.Byte5);
            Assert.Equal(91, reader.RemainingBytes); // 100 - 9
        }

        #endregion
    }
}
