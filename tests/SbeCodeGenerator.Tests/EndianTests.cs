using SbeSourceGenerator;
using SbeSourceGenerator.Schema;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    public class EndianTests
    {
        [Fact]
        public void SchemaContext_DefaultsByteOrder_IsLittleEndian()
        {
            // Arrange & Act
            var context = new SchemaContext("test-schema");

            // Assert
            Assert.Equal("littleEndian", context.ByteOrder);
        }

        [Fact]
        public void ParseSchema_WithLittleEndianAttribute_SetsLittleEndian()
        {
            // Arrange
            var schema = SchemaReader.Parse(@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <sbe:messageSchema xmlns:sbe=""http://fixprotocol.io/2016/sbe""
                                   byteOrder=""littleEndian"">
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");

            // Act
            if (!string.IsNullOrEmpty(schema.ByteOrder))
            {
                context.ByteOrder = schema.ByteOrder;
            }

            // Assert
            Assert.Equal("littleEndian", context.ByteOrder);
        }

        [Fact]
        public void ParseSchema_WithBigEndianAttribute_SetsBigEndian()
        {
            // Arrange
            var schema = SchemaReader.Parse(@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <sbe:messageSchema xmlns:sbe=""http://fixprotocol.io/2016/sbe""
                                   byteOrder=""bigEndian"">
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");

            // Act
            if (!string.IsNullOrEmpty(schema.ByteOrder))
            {
                context.ByteOrder = schema.ByteOrder;
            }

            // Assert
            Assert.Equal("bigEndian", context.ByteOrder);
        }

        [Fact]
        public void ParseSchema_WithoutByteOrderAttribute_DefaultsToLittleEndian()
        {
            // Arrange
            var schema = SchemaReader.Parse(@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <sbe:messageSchema xmlns:sbe=""http://fixprotocol.io/2016/sbe"">
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");

            // Act
            if (!string.IsNullOrEmpty(schema.ByteOrder))
            {
                context.ByteOrder = schema.ByteOrder;
            }

            // Assert - should remain default
            Assert.Equal("littleEndian", context.ByteOrder);
        }
    }
}
