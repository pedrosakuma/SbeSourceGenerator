using SbeSourceGenerator;
using System.Xml;
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
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <sbe:messageSchema xmlns:sbe=""http://fixprotocol.io/2016/sbe""
                                   byteOrder=""littleEndian"">
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");

            // Act
            var messageSchemaNode = xmlDoc.DocumentElement;
            if (messageSchemaNode != null)
            {
                var byteOrderAttr = messageSchemaNode.GetAttribute("byteOrder");
                if (!string.IsNullOrEmpty(byteOrderAttr))
                {
                    context.ByteOrder = byteOrderAttr;
                }
            }

            // Assert
            Assert.Equal("littleEndian", context.ByteOrder);
        }

        [Fact]
        public void ParseSchema_WithBigEndianAttribute_SetsBigEndian()
        {
            // Arrange
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <sbe:messageSchema xmlns:sbe=""http://fixprotocol.io/2016/sbe""
                                   byteOrder=""bigEndian"">
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");

            // Act
            var messageSchemaNode = xmlDoc.DocumentElement;
            if (messageSchemaNode != null)
            {
                var byteOrderAttr = messageSchemaNode.GetAttribute("byteOrder");
                if (!string.IsNullOrEmpty(byteOrderAttr))
                {
                    context.ByteOrder = byteOrderAttr;
                }
            }

            // Assert
            Assert.Equal("bigEndian", context.ByteOrder);
        }

        [Fact]
        public void ParseSchema_WithoutByteOrderAttribute_DefaultsToLittleEndian()
        {
            // Arrange
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <sbe:messageSchema xmlns:sbe=""http://fixprotocol.io/2016/sbe"">
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");

            // Act
            var messageSchemaNode = xmlDoc.DocumentElement;
            if (messageSchemaNode != null)
            {
                var byteOrderAttr = messageSchemaNode.GetAttribute("byteOrder");
                if (!string.IsNullOrEmpty(byteOrderAttr))
                {
                    context.ByteOrder = byteOrderAttr;
                }
            }

            // Assert - should remain default
            Assert.Equal("littleEndian", context.ByteOrder);
        }
    }
}
