using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Integration tests for byte order (endianness) support.
    /// Tests both little-endian and big-endian encoding/decoding.
    /// </summary>
    public class EndianIntegrationTests
    {
        [Fact]
        public void EndianHelpers_ReverseBytes_WorksCorrectly()
        {
            // Test int16
            short s = 0x1234;
            short reversedS = Endian.Test.EndianHelpers.ReverseBytes(s);
            Assert.Equal(0x3412, reversedS);

            // Test uint16
            ushort us = 0x1234;
            ushort reversedUs = Endian.Test.EndianHelpers.ReverseBytes(us);
            Assert.Equal(0x3412, reversedUs);

            // Test int32
            int i = 0x12345678;
            int reversedI = Endian.Test.EndianHelpers.ReverseBytes(i);
            Assert.Equal(0x78563412, reversedI);

            // Test uint32
            uint ui = 0x12345678U;
            uint reversedUi = Endian.Test.EndianHelpers.ReverseBytes(ui);
            Assert.Equal(0x78563412U, reversedUi);

            // Test int64
            long l = 0x123456789ABCDEF0L;
            long reversedL = Endian.Test.EndianHelpers.ReverseBytes(l);
            Assert.Equal(unchecked((long)0xF0DEBC9A78563412UL), reversedL);

            // Test uint64
            ulong ul = 0x123456789ABCDEF0UL;
            ulong reversedUl = Endian.Test.EndianHelpers.ReverseBytes(ul);
            Assert.Equal(0xF0DEBC9A78563412UL, reversedUl);
        }

        [Fact]
        public void EndianHelpers_ReadLittleEndian_WorksCorrectly()
        {
            Span<byte> buffer = stackalloc byte[8];

            // Write known little-endian bytes
            buffer[0] = 0x12;
            buffer[1] = 0x34;
            buffer[2] = 0x56;
            buffer[3] = 0x78;

            // Test int32
            int value = Endian.Test.EndianHelpers.ReadInt32LittleEndian(buffer);
            Assert.Equal(0x78563412, value);

            // Test uint32
            uint uvalue = Endian.Test.EndianHelpers.ReadUInt32LittleEndian(buffer);
            Assert.Equal(0x78563412U, uvalue);
        }

        [Fact]
        public void EndianHelpers_ReadBigEndian_WorksCorrectly()
        {
            Span<byte> buffer = stackalloc byte[8];

            // Write known big-endian bytes
            buffer[0] = 0x12;
            buffer[1] = 0x34;
            buffer[2] = 0x56;
            buffer[3] = 0x78;

            // Test int32
            int value = Endian.Test.EndianHelpers.ReadInt32BigEndian(buffer);
            Assert.Equal(0x12345678, value);

            // Test uint32
            uint uvalue = Endian.Test.EndianHelpers.ReadUInt32BigEndian(buffer);
            Assert.Equal(0x12345678U, uvalue);
        }

        [Fact]
        public void EndianHelpers_WriteLittleEndian_WorksCorrectly()
        {
            Span<byte> buffer = stackalloc byte[8];

            // Test int32
            Endian.Test.EndianHelpers.WriteInt32LittleEndian(buffer, 0x12345678);
            Assert.Equal(0x78, buffer[0]);
            Assert.Equal(0x56, buffer[1]);
            Assert.Equal(0x34, buffer[2]);
            Assert.Equal(0x12, buffer[3]);

            // Test uint32
            buffer.Clear();
            Endian.Test.EndianHelpers.WriteUInt32LittleEndian(buffer, 0x12345678U);
            Assert.Equal(0x78, buffer[0]);
            Assert.Equal(0x56, buffer[1]);
            Assert.Equal(0x34, buffer[2]);
            Assert.Equal(0x12, buffer[3]);
        }

        [Fact]
        public void EndianHelpers_WriteBigEndian_WorksCorrectly()
        {
            Span<byte> buffer = stackalloc byte[8];

            // Test int32
            Endian.Test.EndianHelpers.WriteInt32BigEndian(buffer, 0x12345678);
            Assert.Equal(0x12, buffer[0]);
            Assert.Equal(0x34, buffer[1]);
            Assert.Equal(0x56, buffer[2]);
            Assert.Equal(0x78, buffer[3]);

            // Test uint32
            buffer.Clear();
            Endian.Test.EndianHelpers.WriteUInt32BigEndian(buffer, 0x12345678U);
            Assert.Equal(0x12, buffer[0]);
            Assert.Equal(0x34, buffer[1]);
            Assert.Equal(0x56, buffer[2]);
            Assert.Equal(0x78, buffer[3]);
        }

        [Fact]
        public void EndianHelpers_IsLittleEndian_ReturnsExpectedValue()
        {
            // Most platforms are little-endian, but this verifies the property works
            bool isLittleEndian = Endian.Test.EndianHelpers.IsLittleEndian;
            Assert.Equal(BitConverter.IsLittleEndian, isLittleEndian);
        }

        [Fact]
        public void EndianHelpers_ReadWrite_RoundTrip_LittleEndian()
        {
            Span<byte> buffer = stackalloc byte[8];

            // Test int64 round trip
            long originalLong = 0x123456789ABCDEF0L;
            Endian.Test.EndianHelpers.WriteInt64LittleEndian(buffer, originalLong);
            long readLong = Endian.Test.EndianHelpers.ReadInt64LittleEndian(buffer);
            Assert.Equal(originalLong, readLong);

            // Test uint64 round trip
            ulong originalULong = 0x123456789ABCDEF0UL;
            Endian.Test.EndianHelpers.WriteUInt64LittleEndian(buffer, originalULong);
            ulong readULong = Endian.Test.EndianHelpers.ReadUInt64LittleEndian(buffer);
            Assert.Equal(originalULong, readULong);
        }

        [Fact]
        public void EndianHelpers_ReadWrite_RoundTrip_BigEndian()
        {
            Span<byte> buffer = stackalloc byte[8];

            // Test int64 round trip
            long originalLong = 0x123456789ABCDEF0L;
            Endian.Test.EndianHelpers.WriteInt64BigEndian(buffer, originalLong);
            long readLong = Endian.Test.EndianHelpers.ReadInt64BigEndian(buffer);
            Assert.Equal(originalLong, readLong);

            // Test uint64 round trip
            ulong originalULong = 0x123456789ABCDEF0UL;
            Endian.Test.EndianHelpers.WriteUInt64BigEndian(buffer, originalULong);
            ulong readULong = Endian.Test.EndianHelpers.ReadUInt64BigEndian(buffer);
            Assert.Equal(originalULong, readULong);
        }
    }
}
