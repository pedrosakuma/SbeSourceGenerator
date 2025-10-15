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
    /// Integration tests for custom encoding/decoding hooks.
    /// Tests the complete workflow from schema to generated code with hooks.
    /// </summary>
    public class EncodingHooksIntegrationTests
    {
        private const string TestSchema = @"
            <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe' 
                               package='TestSchema' 
                               id='1' 
                               version='1' 
                               byteOrder='littleEndian'>
                <sbe:message name='Order' id='1' description='Order message'>
                    <field name='orderId' id='1' type='int64'/>
                    <field name='price' id='2' type='int64'/>
                    <field name='quantity' id='3' type='int32'/>
                </sbe:message>
            </sbe:messageSchema>";

        [Fact]
        public void GeneratedCode_IncludesAllHookMethods()
        {
            // Arrange
            var messagesGenerator = new MessagesCodeGenerator();
            var utilitiesGenerator = new UtilitiesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(TestSchema);

            // Act - Generate messages
            var messageResults = messagesGenerator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));
            var messageCode = messageResults.FirstOrDefault(r => r.name.Contains("Order")).content;

            // Act - Generate utilities (including hooks)
            var utilityResults = utilitiesGenerator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));
            var hooksCode = utilityResults.FirstOrDefault(r => r.name.Contains("EncodingHooks")).content;

            // Assert - Message includes hook-aware methods
            Assert.NotNull(messageCode);
            Assert.Contains("TryParse(ReadOnlySpan<byte> buffer, int blockLength, out OrderData message, out ReadOnlySpan<byte> variableData, EncodingHooks<OrderData>? hooks)", messageCode);
            Assert.Contains("TryEncode(ref OrderData message, Span<byte> buffer, EncodingHooks<OrderData>? hooks = null)", messageCode);

            // Assert - Hooks infrastructure is generated
            Assert.NotNull(hooksCode);
            Assert.Contains("EncodingHooks<TMessage>", hooksCode);
            Assert.Contains("MessagePreEncodingHook", hooksCode);
            Assert.Contains("MessagePostEncodingHook", hooksCode);
            Assert.Contains("MessagePreDecodingHook", hooksCode);
            Assert.Contains("MessagePostDecodingHook", hooksCode);
            Assert.Contains("EncodingHooksHelper", hooksCode);
        }

        [Fact]
        public void GeneratedCode_SupportsHooksInAllParseOverloads()
        {
            // Arrange
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(TestSchema);

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));
            var orderMessage = results.FirstOrDefault(r => r.name.Contains("Order"));

            // Assert - All expected method signatures exist
            Assert.NotNull(orderMessage);
            var code = orderMessage.content;

            // Original methods (backward compatibility)
            Assert.Contains("public static bool TryParse(ReadOnlySpan<byte> buffer, out OrderData message, out ReadOnlySpan<byte> variableData)", code);
            Assert.Contains("public static bool TryParse(ReadOnlySpan<byte> buffer, int blockLength, out OrderData message, out ReadOnlySpan<byte> variableData)", code);
            Assert.Contains("public static bool TryParseWithReader(ref SpanReader reader, int blockLength, out OrderData message)", code);

            // New methods with hooks
            Assert.Contains("public static bool TryParse(ReadOnlySpan<byte> buffer, int blockLength, out OrderData message, out ReadOnlySpan<byte> variableData, EncodingHooks<OrderData>? hooks)", code);
            Assert.Contains("public static bool TryEncode(ref OrderData message, Span<byte> buffer, EncodingHooks<OrderData>? hooks = null)", code);
        }

        [Fact]
        public void GeneratedHooks_IncludePreDecodingLogic()
        {
            // Arrange
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(TestSchema);

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));
            var orderMessage = results.FirstOrDefault(r => r.name.Contains("Order"));

            // Assert - PreDecoding hook is invoked
            Assert.NotNull(orderMessage);
            Assert.Contains("if (hooks?.PreDecoding != null && !hooks.PreDecoding(buffer))", orderMessage.content);
            Assert.Contains("message = default;", orderMessage.content);
            Assert.Contains("variableData = default;", orderMessage.content);
            Assert.Contains("return false;", orderMessage.content);
        }

        [Fact]
        public void GeneratedHooks_IncludePostDecodingLogic()
        {
            // Arrange
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(TestSchema);

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));
            var orderMessage = results.FirstOrDefault(r => r.name.Contains("Order"));

            // Assert - PostDecoding hook is invoked
            Assert.NotNull(orderMessage);
            Assert.Contains("if (hooks?.PostDecoding != null && !hooks.PostDecoding(ref message))", orderMessage.content);
        }

        [Fact]
        public void EncodingHooksHelper_IncludesProperNamespace()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(TestSchema);

            // Act
            var results = generator.Generate("MyCustomNamespace", xmlDoc, context, default(SourceProductionContext));
            var hooksResult = results.FirstOrDefault(r => r.name.Contains("EncodingHooks"));

            // Assert
            Assert.NotNull(hooksResult);
            Assert.Contains("namespace MyCustomNamespace.Runtime", hooksResult.content);
        }

        [Fact]
        public void GeneratedCode_MaintainsBackwardCompatibility()
        {
            // Arrange
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(TestSchema);

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));
            var orderMessage = results.FirstOrDefault(r => r.name.Contains("Order"));

            // Assert - All original methods still exist unchanged
            Assert.NotNull(orderMessage);
            var code = orderMessage.content;

            // Check that the simple TryParse without hooks still exists and calls the version with blockLength
            Assert.Contains("public static bool TryParse(ReadOnlySpan<byte> buffer, out OrderData message, out ReadOnlySpan<byte> variableData)", code);
            Assert.Contains("return TryParse(buffer, MESSAGE_SIZE, out message, out variableData);", code);

            // Check that TryParseWithReader is unchanged
            Assert.Contains("public static bool TryParseWithReader(ref SpanReader reader, int blockLength, out OrderData message)", code);
        }

        [Fact]
        public void EncodingHooksGenerator_CreatesCompleteAPI()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(TestSchema);

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));
            var hooksResult = results.FirstOrDefault(r => r.name.Contains("EncodingHooks"));

            // Assert - All hook types are present
            Assert.NotNull(hooksResult);
            var code = hooksResult.content;

            // Delegates
            Assert.Contains("public delegate bool FieldEncodingHook<T>", code);
            Assert.Contains("public delegate bool FieldDecodingHook<T>", code);
            Assert.Contains("public delegate bool MessagePreEncodingHook<TMessage>", code);
            Assert.Contains("public delegate void MessagePostEncodingHook<TMessage>", code);
            Assert.Contains("public delegate bool MessagePreDecodingHook", code);
            Assert.Contains("public delegate bool MessagePostDecodingHook<TMessage>", code);

            // Container class
            Assert.Contains("public class EncodingHooks<TMessage>", code);
            Assert.Contains("public MessagePreEncodingHook<TMessage>? PreEncoding", code);
            Assert.Contains("public MessagePostEncodingHook<TMessage>? PostEncoding", code);
            Assert.Contains("public MessagePreDecodingHook? PreDecoding", code);
            Assert.Contains("public MessagePostDecodingHook<TMessage>? PostDecoding", code);

            // Helper class
            Assert.Contains("public static class EncodingHooksHelper", code);
            Assert.Contains("public static bool TryEncode<TMessage>", code);
            Assert.Contains("public static bool TryDecode<TMessage>", code);
        }

        [Fact]
        public void GeneratedMessages_ArePartialStructs()
        {
            // Arrange
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(TestSchema);

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));
            var orderMessage = results.FirstOrDefault(r => r.name.Contains("Order"));

            // Assert - Messages are partial structs for extensibility
            Assert.NotNull(orderMessage);
            Assert.Contains("public partial struct OrderData", orderMessage.content);
        }

        [Fact]
        public void EncodingHooksHelper_ImplementsPreEncodingHook()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(TestSchema);

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));
            var hooksResult = results.FirstOrDefault(r => r.name.Contains("EncodingHooks"));

            // Assert
            Assert.NotNull(hooksResult);
            var code = hooksResult.content;
            Assert.Contains("if (hooks?.PreEncoding != null && !hooks.PreEncoding(ref message))", code);
            Assert.Contains("return false;", code);
        }

        [Fact]
        public void EncodingHooksHelper_ImplementsPostEncodingHook()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(TestSchema);

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));
            var hooksResult = results.FirstOrDefault(r => r.name.Contains("EncodingHooks"));

            // Assert
            Assert.NotNull(hooksResult);
            var code = hooksResult.content;
            Assert.Contains("hooks?.PostEncoding?.Invoke(ref message, buffer);", code);
        }

        [Fact]
        public void EncodingHooksHelper_ImplementsPreDecodingHook()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(TestSchema);

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));
            var hooksResult = results.FirstOrDefault(r => r.name.Contains("EncodingHooks"));

            // Assert
            Assert.NotNull(hooksResult);
            var code = hooksResult.content;
            Assert.Contains("if (hooks?.PreDecoding != null && !hooks.PreDecoding(buffer))", code);
        }

        [Fact]
        public void EncodingHooksHelper_ImplementsPostDecodingHook()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(TestSchema);

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));
            var hooksResult = results.FirstOrDefault(r => r.name.Contains("EncodingHooks"));

            // Assert
            Assert.NotNull(hooksResult);
            var code = hooksResult.content;
            Assert.Contains("if (hooks?.PostDecoding != null && !hooks.PostDecoding(ref message))", code);
        }
    }
}
