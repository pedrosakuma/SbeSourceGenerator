using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.CodeAnalysis;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    /// <summary>
    /// Tests for custom encoding/decoding hooks functionality.
    /// </summary>
    public class EncodingHooksTests
    {
        [Fact]
        public void EncodingHooksGenerator_GeneratesEncodingHooksClass()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<messageSchema></messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var encodingHooksResult = resultList.FirstOrDefault(r => r.name.Contains("EncodingHooks"));
            Assert.NotEqual(default, encodingHooksResult);
            Assert.Contains("EncodingHooks<TMessage>", encodingHooksResult.content);
            Assert.Contains("MessagePreEncodingHook", encodingHooksResult.content);
            Assert.Contains("MessagePostEncodingHook", encodingHooksResult.content);
            Assert.Contains("MessagePreDecodingHook", encodingHooksResult.content);
            Assert.Contains("MessagePostDecodingHook", encodingHooksResult.content);
            Assert.Contains("EncodingHooksHelper", encodingHooksResult.content);
        }

        [Fact]
        public void EncodingHooksGenerator_GeneratesFieldHookDelegates()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<messageSchema></messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var encodingHooksResult = resultList.FirstOrDefault(r => r.name.Contains("EncodingHooks"));
            Assert.NotEqual(default, encodingHooksResult);
            Assert.Contains("FieldEncodingHook", encodingHooksResult.content);
            Assert.Contains("FieldDecodingHook", encodingHooksResult.content);
        }

        [Fact]
        public void EncodingHooksGenerator_GeneratesHelperMethods()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<messageSchema></messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var encodingHooksResult = resultList.FirstOrDefault(r => r.name.Contains("EncodingHooks"));
            Assert.NotEqual(default, encodingHooksResult);
            Assert.Contains("EncodingHooksHelper", encodingHooksResult.content);
            Assert.Contains("TryEncode", encodingHooksResult.content);
            Assert.Contains("TryDecode", encodingHooksResult.content);
        }

        [Fact]
        public void MessagesCodeGenerator_GeneratesTryParseWithHooks()
        {
            // Arrange
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='TestMessage' id='1' description='A test message'>
                        <field name='field1' id='1' type='uint32'/>
                        <field name='field2' id='2' type='uint64'/>
                    </sbe:message>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var messageResult = resultList.FirstOrDefault(r => r.name.Contains("TestMessage"));
            Assert.NotEqual(default, messageResult);
            Assert.Contains("TryParse(ReadOnlySpan<byte> buffer, int blockLength, out TestMessageData message, out ReadOnlySpan<byte> variableData, EncodingHooks<TestMessageData>? hooks)", messageResult.content);
            Assert.Contains("hooks?.PreDecoding", messageResult.content);
            Assert.Contains("hooks?.PostDecoding", messageResult.content);
        }

        [Fact]
        public void MessagesCodeGenerator_GeneratesTryEncode()
        {
            // Arrange
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='TestMessage' id='1' description='A test message'>
                        <field name='field1' id='1' type='uint32'/>
                    </sbe:message>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var messageResult = resultList.FirstOrDefault(r => r.name.Contains("TestMessage"));
            Assert.NotEqual(default, messageResult);
            Assert.Contains("public static bool TryEncode(ref TestMessageData message, Span<byte> buffer, EncodingHooks<TestMessageData>? hooks = null)", messageResult.content);
            Assert.Contains("EncodingHooksHelper.TryEncode", messageResult.content);
        }

        [Fact]
        public void MessagesCodeGenerator_MaintainsBackwardCompatibility()
        {
            // Arrange
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='TestMessage' id='1' description='A test message'>
                        <field name='field1' id='1' type='uint32'/>
                    </sbe:message>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var messageResult = resultList.FirstOrDefault(r => r.name.Contains("TestMessage"));
            Assert.NotEqual(default, messageResult);
            
            // Verify that all original methods still exist
            Assert.Contains("public static bool TryParse(ReadOnlySpan<byte> buffer, out TestMessageData message, out ReadOnlySpan<byte> variableData)", messageResult.content);
            Assert.Contains("public static bool TryParse(ReadOnlySpan<byte> buffer, int blockLength, out TestMessageData message, out ReadOnlySpan<byte> variableData)", messageResult.content);
            Assert.Contains("public static bool TryParseWithReader(ref SpanReader reader, int blockLength, out TestMessageData message)", messageResult.content);
        }

        [Fact]
        public void EncodingHooksGenerator_UsesCorrectNamespace()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<messageSchema></messageSchema>");

            // Act
            var results = generator.Generate("MyCustomNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var encodingHooksResult = resultList.FirstOrDefault(r => r.name.Contains("EncodingHooks"));
            Assert.NotEqual(default, encodingHooksResult);
            Assert.Contains("namespace MyCustomNamespace.Runtime", encodingHooksResult.content);
        }

        [Fact]
        public void EncodingHooksGenerator_IncludesDocumentation()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<messageSchema></messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var encodingHooksResult = resultList.FirstOrDefault(r => r.name.Contains("EncodingHooks"));
            Assert.NotEqual(default, encodingHooksResult);
            
            // Check for XML documentation comments
            Assert.Contains("/// <summary>", encodingHooksResult.content);
            Assert.Contains("Allows users to intercept", encodingHooksResult.content);
            Assert.Contains("extensibility points", encodingHooksResult.content);
        }
    }
}
