using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using SbeSourceGenerator.Generators.Types;
using System.Collections.Generic;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    public class ParserCodeGeneratorTests
    {
        [Fact]
        public void GenerateParser_WithMessages_ProducesParserCode()
        {
            // Arrange
            var generator = new ParserCodeGenerator();
            var context = new SchemaContext();
            var messages = new List<MessageDefinition>
            {
                new MessageDefinition(
                    "TestNamespace",
                    "TestMessage",
                    "1",
                    "Test message",
                    "",
                    "",
                    new List<IFileContentGenerator>(),
                    new List<IFileContentGenerator>(),
                    new List<IFileContentGenerator>(),
                    new List<IFileContentGenerator>()
                )
            };

            // Act
            var results = generator.GenerateParser("TestNamespace", messages, context);

            // Assert
            var resultList = results.ToList();
            Assert.Single(resultList);
            Assert.Contains("MessageParser", resultList[0].name);
            Assert.Contains("MessageParser", resultList[0].content);
            Assert.Contains("TestMessage", resultList[0].content);
        }

        [Fact]
        public void GenerateParser_WithMultipleMessages_IncludesAllMessages()
        {
            // Arrange
            var generator = new ParserCodeGenerator();
            var context = new SchemaContext();
            var messages = new List<MessageDefinition>
            {
                new MessageDefinition(
                    "TestNamespace",
                    "Message1",
                    "1",
                    "First message",
                    "",
                    "",
                    new List<IFileContentGenerator>(),
                    new List<IFileContentGenerator>(),
                    new List<IFileContentGenerator>(),
                    new List<IFileContentGenerator>()
                ),
                new MessageDefinition(
                    "TestNamespace",
                    "Message2",
                    "2",
                    "Second message",
                    "",
                    "",
                    new List<IFileContentGenerator>(),
                    new List<IFileContentGenerator>(),
                    new List<IFileContentGenerator>(),
                    new List<IFileContentGenerator>()
                )
            };

            // Act
            var results = generator.GenerateParser("TestNamespace", messages, context);

            // Assert
            var resultList = results.ToList();
            Assert.Single(resultList);
            Assert.Contains("Message1", resultList[0].content);
            Assert.Contains("Message2", resultList[0].content);
        }
    }
}
