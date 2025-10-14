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
    }
}
