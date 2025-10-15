using System;
using System.Runtime.InteropServices;
using SbeSourceGenerator.Runtime;
using Xunit;

namespace SbeCodeGenerator.Tests.Runtime
{
    public class SpanWriterTests
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
            var writer = new SpanWriter(buffer);
            
            // Assert
            Assert.Equal(100, writer.RemainingBytes);
            Assert.Equal(0, writer.BytesWritten);
        }

        [Fact]
        public void BytesWritten_StartsAtZero()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[50];
            var writer = new SpanWriter(buffer);
            
            // Assert
            Assert.Equal(0, writer.BytesWritten);
        }

        [Fact]
        public void RemainingBytes_ReturnsCorrectCount()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[50];
            var writer = new SpanWriter(buffer);
            
            // Assert
            Assert.Equal(50, writer.RemainingBytes);
        }

        [Fact]
        public void CanWrite_ReturnsTrueWhenEnoughSpace()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            
            // Act & Assert
            Assert.True(writer.CanWrite(50));
            Assert.True(writer.CanWrite(100));
        }

        [Fact]
        public void CanWrite_ReturnsFalseWhenNotEnoughSpace()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            
            // Act & Assert
            Assert.False(writer.CanWrite(101));
            Assert.False(writer.CanWrite(200));
        }

        [Fact]
        public void TryWrite_WritesStructAndAdvancesPosition()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            int initialRemaining = writer.RemainingBytes;
            
            var testData = new TestStruct
            {
                Field1 = 42,
                Field2 = 12345678,
                Field3 = 99
            };
            
            // Act
            bool success = writer.TryWrite(testData);
            
            // Assert
            Assert.True(success);
            Assert.Equal(Marshal.SizeOf<TestStruct>(), writer.BytesWritten);
            Assert.Equal(initialRemaining - Marshal.SizeOf<TestStruct>(), writer.RemainingBytes);
            
            // Verify written data
            ref var readBack = ref MemoryMarshal.AsRef<TestStruct>(buffer);
            Assert.Equal(42, readBack.Field1);
            Assert.Equal(12345678, readBack.Field2);
            Assert.Equal(99, readBack.Field3);
        }

        [Fact]
        public void TryWrite_ReturnsFalseWhenNotEnoughSpace()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[5]; // Too small for TestStruct
            var writer = new SpanWriter(buffer);
            
            var testData = new TestStruct { Field1 = 42 };
            
            // Act
            bool success = writer.TryWrite(testData);
            
            // Assert
            Assert.False(success);
            Assert.Equal(0, writer.BytesWritten); // Position unchanged
            Assert.Equal(5, writer.RemainingBytes);
        }

        [Fact]
        public void Write_WritesStructSuccessfully()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            
            var testData = new TestStruct
            {
                Field1 = 42,
                Field2 = 12345678,
                Field3 = 99
            };
            
            // Act
            writer.Write(testData);
            
            // Assert
            Assert.Equal(Marshal.SizeOf<TestStruct>(), writer.BytesWritten);
            
            // Verify written data
            ref var readBack = ref MemoryMarshal.AsRef<TestStruct>(buffer);
            Assert.Equal(42, readBack.Field1);
            Assert.Equal(12345678, readBack.Field2);
            Assert.Equal(99, readBack.Field3);
        }

        [Fact]
        public void Write_ThrowsWhenNotEnoughSpace()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[5]; // Too small
            var writer = new SpanWriter(buffer);
            var testData = new TestStruct { Field1 = 42 };
            
            // Act
            bool exceptionThrown = false;
            try
            {
                writer.Write(testData);
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }
            
            // Assert
            Assert.True(exceptionThrown);
        }

        [Fact]
        public void TryWriteBytes_WritesAndAdvancesPosition()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            ReadOnlySpan<byte> data = stackalloc byte[] { 1, 2, 3, 4, 5 };
            
            // Act
            bool success = writer.TryWriteBytes(data);
            
            // Assert
            Assert.True(success);
            Assert.Equal(5, writer.BytesWritten);
            Assert.Equal(95, writer.RemainingBytes);
            
            // Verify written data
            Assert.Equal(1, buffer[0]);
            Assert.Equal(2, buffer[1]);
            Assert.Equal(3, buffer[2]);
            Assert.Equal(4, buffer[3]);
            Assert.Equal(5, buffer[4]);
        }

        [Fact]
        public void TryWriteBytes_ReturnsFalseWhenNotEnoughSpace()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[5];
            var writer = new SpanWriter(buffer);
            ReadOnlySpan<byte> data = stackalloc byte[10]; // Too large
            
            // Act
            bool success = writer.TryWriteBytes(data);
            
            // Assert
            Assert.False(success);
            Assert.Equal(0, writer.BytesWritten); // Position unchanged
            Assert.Equal(5, writer.RemainingBytes);
        }

        [Fact]
        public void WriteBytes_WritesSuccessfully()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            ReadOnlySpan<byte> data = stackalloc byte[] { 1, 2, 3, 4, 5 };
            
            // Act
            writer.WriteBytes(data);
            
            // Assert
            Assert.Equal(5, writer.BytesWritten);
            
            // Verify written data
            Assert.Equal(1, buffer[0]);
            Assert.Equal(2, buffer[1]);
            Assert.Equal(3, buffer[2]);
            Assert.Equal(4, buffer[3]);
            Assert.Equal(5, buffer[4]);
        }

        [Fact]
        public void WriteBytes_ThrowsWhenNotEnoughSpace()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[5];
            var writer = new SpanWriter(buffer);
            ReadOnlySpan<byte> data = stackalloc byte[10];
            
            // Act
            bool exceptionThrown = false;
            try
            {
                writer.WriteBytes(data);
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }
            
            // Assert
            Assert.True(exceptionThrown);
        }

        [Fact]
        public void TrySkip_SkipsAndAdvancesPosition()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            
            // Act
            bool success = writer.TrySkip(20);
            
            // Assert
            Assert.True(success);
            Assert.Equal(20, writer.BytesWritten);
            Assert.Equal(80, writer.RemainingBytes);
        }

        [Fact]
        public void TrySkip_ClearsByDefault()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            buffer.Fill(0xFF); // Fill with non-zero data
            var writer = new SpanWriter(buffer);
            
            // Act
            bool success = writer.TrySkip(20); // Default clear = true
            
            // Assert
            Assert.True(success);
            
            // Verify bytes were cleared
            for (int i = 0; i < 20; i++)
            {
                Assert.Equal(0, buffer[i]);
            }
            
            // Verify remaining bytes unchanged
            for (int i = 20; i < 100; i++)
            {
                Assert.Equal(0xFF, buffer[i]);
            }
        }

        [Fact]
        public void TrySkip_DoesNotClearWhenClearIsFalse()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            buffer.Fill(0xFF); // Fill with non-zero data
            var writer = new SpanWriter(buffer);
            
            // Act
            bool success = writer.TrySkip(20, clear: false);
            
            // Assert
            Assert.True(success);
            
            // Verify bytes were NOT cleared
            for (int i = 0; i < 20; i++)
            {
                Assert.Equal(0xFF, buffer[i]);
            }
        }

        [Fact]
        public void TrySkip_ReturnsFalseWhenNotEnoughSpace()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[10];
            var writer = new SpanWriter(buffer);
            
            // Act
            bool success = writer.TrySkip(20);
            
            // Assert
            Assert.False(success);
            Assert.Equal(0, writer.BytesWritten); // Position unchanged
            Assert.Equal(10, writer.RemainingBytes);
        }

        [Fact]
        public void Skip_SkipsSuccessfully()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            
            // Act
            writer.Skip(20);
            
            // Assert
            Assert.Equal(20, writer.BytesWritten);
            Assert.Equal(80, writer.RemainingBytes);
        }

        [Fact]
        public void Skip_ThrowsWhenNotEnoughSpace()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[10];
            var writer = new SpanWriter(buffer);
            
            // Act
            bool exceptionThrown = false;
            try
            {
                writer.Skip(20);
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }
            
            // Assert
            Assert.True(exceptionThrown);
        }

        [Fact]
        public void TryGetSlice_ReturnsSliceAtOffset()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            writer.TrySkip(10, clear: false); // Advance position to 10
            
            // Act
            bool success = writer.TryGetSlice(5, 10, out var slice);
            
            // Assert
            Assert.True(success);
            Assert.Equal(10, slice.Length);
            
            // Verify position unchanged
            Assert.Equal(10, writer.BytesWritten);
            
            // Verify we can write to the slice
            slice.Fill(42);
            Assert.Equal(42, buffer[15]); // offset 10 + 5
            Assert.Equal(42, buffer[24]); // offset 10 + 5 + 9
        }

        [Fact]
        public void TryGetSlice_ReturnsFalseWhenInvalidOffset()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            
            // Act
            bool success = writer.TryGetSlice(-1, 10, out var slice);
            
            // Assert
            Assert.False(success);
            Assert.True(slice.IsEmpty);
        }

        [Fact]
        public void TryGetSlice_ReturnsFalseWhenNotEnoughSpace()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            writer.TrySkip(90, clear: false);
            
            // Act
            bool success = writer.TryGetSlice(0, 20, out var slice);
            
            // Assert
            Assert.False(success);
            Assert.True(slice.IsEmpty);
        }

        [Fact]
        public void Reset_ResetsPositionToZero()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            writer.TrySkip(50, clear: false);
            
            // Act
            writer.Reset();
            
            // Assert
            Assert.Equal(0, writer.BytesWritten);
            Assert.Equal(100, writer.RemainingBytes);
        }

        [Fact]
        public void Reset_ResetsToSpecifiedPosition()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            writer.TrySkip(50, clear: false);
            
            // Act
            writer.Reset(30);
            
            // Assert
            Assert.Equal(30, writer.BytesWritten);
            Assert.Equal(70, writer.RemainingBytes);
        }

        [Fact]
        public void Reset_ThrowsWhenPositionIsNegative()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            
            // Act
            bool exceptionThrown = false;
            try
            {
                writer.Reset(-1);
            }
            catch (ArgumentOutOfRangeException)
            {
                exceptionThrown = true;
            }
            
            // Assert
            Assert.True(exceptionThrown);
        }

        [Fact]
        public void Reset_ThrowsWhenPositionIsTooLarge()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            
            // Act
            bool exceptionThrown = false;
            try
            {
                writer.Reset(101);
            }
            catch (ArgumentOutOfRangeException)
            {
                exceptionThrown = true;
            }
            
            // Assert
            Assert.True(exceptionThrown);
        }

        [Fact]
        public void SequentialWrites_WorkCorrectly()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            
            // Act & Assert
            Assert.True(writer.TryWrite(42));
            Assert.Equal(4, writer.BytesWritten);
            Assert.Equal(96, writer.RemainingBytes);
            
            Assert.True(writer.TryWrite(9876543210L));
            Assert.Equal(12, writer.BytesWritten);
            Assert.Equal(88, writer.RemainingBytes);
            
            Assert.True(writer.TryWrite((byte)255));
            Assert.Equal(13, writer.BytesWritten);
            Assert.Equal(87, writer.RemainingBytes);
            
            // Verify written data
            ref int val1 = ref MemoryMarshal.AsRef<int>(buffer);
            Assert.Equal(42, val1);
            
            ref long val2 = ref MemoryMarshal.AsRef<long>(buffer.Slice(4));
            Assert.Equal(9876543210, val2);
            
            ref byte val3 = ref MemoryMarshal.AsRef<byte>(buffer.Slice(12));
            Assert.Equal(255, val3);
        }

        [Fact]
        public void MixedOperations_WorkCorrectly()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            
            // Act & Assert
            Assert.True(writer.TryWrite(123));
            Assert.Equal(4, writer.BytesWritten);
            
            Assert.True(writer.TrySkip(2));
            Assert.Equal(6, writer.BytesWritten);
            
            ReadOnlySpan<byte> data = stackalloc byte[] { 7, 8, 9 };
            Assert.True(writer.TryWriteBytes(data));
            Assert.Equal(9, writer.BytesWritten);
            
            Assert.Equal(91, writer.RemainingBytes);
        }

        [Fact]
        public void Remaining_ReturnsCorrectSpan()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);
            
            // Act
            writer.TryWrite(42);
            writer.TryWrite((byte)99);
            var remaining = writer.Remaining;
            
            // Assert
            Assert.Equal(95, remaining.Length); // 100 - 4 - 1
            
            // Verify we can write to remaining
            remaining[0] = 123;
            Assert.Equal(123, buffer[5]);
        }

        #region Custom Encoder Tests (Schema Evolution & Extensibility)

        private struct VersionedMessage
        {
            public ushort Version;
            public int Value;
            public long ExtendedValue; // Only in version 2+
        }

        [Fact]
        public void TryWriteWith_CustomEncoder_EncodesSuccessfully()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);

            var message = new VersionedMessage
            {
                Version = 1,
                Value = 42
            };

            // Custom encoder that writes version-specific data
            static bool EncodeVersionedMessage(Span<byte> buf, VersionedMessage msg, out int written)
            {
                if (buf.Length < 6)
                {
                    written = 0;
                    return false;
                }

                var tempWriter = new SpanWriter(buf);
                tempWriter.Write(msg.Version);
                tempWriter.Write(msg.Value);
                written = tempWriter.BytesWritten;
                return true;
            }

            // Act
            bool success = writer.TryWriteWith<VersionedMessage>(EncodeVersionedMessage, message);

            // Assert
            Assert.True(success);
            Assert.Equal(6, writer.BytesWritten); // 2 + 4
            Assert.Equal(94, writer.RemainingBytes);
            
            // Verify written data
            Assert.Equal(1, MemoryMarshal.Read<ushort>(buffer));
            Assert.Equal(42, MemoryMarshal.Read<int>(buffer.Slice(2)));
        }

        [Fact]
        public void TryWriteWith_CustomEncoder_HandlesSchemaEvolution()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);

            var messageV2 = new VersionedMessage
            {
                Version = 2,
                Value = 42,
                ExtendedValue = 9999
            };

            // Custom encoder that handles schema evolution
            static bool EncodeVersionedMessageEvolution(Span<byte> buf, VersionedMessage msg, out int written)
            {
                var tempWriter = new SpanWriter(buf);
                
                if (!tempWriter.TryWrite(msg.Version))
                {
                    written = 0;
                    return false;
                }
                
                if (!tempWriter.TryWrite(msg.Value))
                {
                    written = 0;
                    return false;
                }
                
                if (msg.Version >= 2)
                {
                    if (!tempWriter.TryWrite(msg.ExtendedValue))
                    {
                        written = 0;
                        return false;
                    }
                }
                
                written = tempWriter.BytesWritten;
                return true;
            }

            // Act
            bool success = writer.TryWriteWith<VersionedMessage>(EncodeVersionedMessageEvolution, messageV2);

            // Assert
            Assert.True(success);
            Assert.Equal(14, writer.BytesWritten); // 2 + 4 + 8
            Assert.Equal(86, writer.RemainingBytes);
            
            // Verify written data
            Assert.Equal(2, MemoryMarshal.Read<ushort>(buffer));
            Assert.Equal(42, MemoryMarshal.Read<int>(buffer.Slice(2)));
            Assert.Equal(9999, MemoryMarshal.Read<long>(buffer.Slice(6)));
        }

        [Fact]
        public void TryWriteWith_CustomEncoder_FailsWhenInsufficientSpace()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[3]; // Too small
            var writer = new SpanWriter(buffer);

            static bool FailingEncoder(Span<byte> buf, int value, out int written)
            {
                written = 0;
                return false;
            }

            // Act
            bool success = writer.TryWriteWith<int>(FailingEncoder, 42);

            // Assert
            Assert.False(success);
            Assert.Equal(0, writer.BytesWritten); // Position unchanged
        }

        [Fact]
        public void WriteWith_EncodesSuccessfully()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);

            static bool SimpleEncoder(Span<byte> buf, int value, out int written)
            {
                var tempWriter = new SpanWriter(buf);
                tempWriter.Write(value);
                written = tempWriter.BytesWritten;
                return true;
            }

            // Act
            writer.WriteWith(SimpleEncoder, 123);

            // Assert
            Assert.Equal(4, writer.BytesWritten);
            Assert.Equal(123, MemoryMarshal.Read<int>(buffer));
        }

        [Fact]
        public void WriteWith_ThrowsWhenEncodingFails()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[3];
            var writer = new SpanWriter(buffer);

            static bool FailingEncoder(Span<byte> buf, int value, out int written)
            {
                written = 0;
                return false;
            }

            // Act
            bool exceptionThrown = false;
            try
            {
                writer.WriteWith(FailingEncoder, 42);
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }
            
            // Assert
            Assert.True(exceptionThrown);
        }

        #endregion

        #region Round-trip Tests with SpanReader

        [Fact]
        public void RoundTrip_WriteAndRead_PreservesData()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);

            var originalStruct = new TestStruct
            {
                Field1 = 42,
                Field2 = 12345678,
                Field3 = 99
            };

            // Act - Write
            writer.Write(originalStruct);

            // Act - Read
            var reader = new SpanReader(buffer);
            reader.TryRead<TestStruct>(out var readStruct);

            // Assert
            Assert.Equal(originalStruct.Field1, readStruct.Field1);
            Assert.Equal(originalStruct.Field2, readStruct.Field2);
            Assert.Equal(originalStruct.Field3, readStruct.Field3);
        }

        [Fact]
        public void RoundTrip_SequentialWriteAndRead_PreservesData()
        {
            // Arrange
            Span<byte> buffer = stackalloc byte[100];
            var writer = new SpanWriter(buffer);

            // Act - Write
            writer.Write(42);
            writer.Write(9876543210L);
            writer.Write((byte)255);

            // Act - Read
            var reader = new SpanReader(buffer);
            reader.TryRead<int>(out var val1);
            reader.TryRead<long>(out var val2);
            reader.TryRead<byte>(out var val3);

            // Assert
            Assert.Equal(42, val1);
            Assert.Equal(9876543210, val2);
            Assert.Equal(255, val3);
        }

        #endregion
    }
}
