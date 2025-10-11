using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using System.Xml;
using Microsoft.CodeAnalysis;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    public class MessagesCodeGeneratorTests
    {
        [Fact]
        public void Generate_WithSimpleMessage_ProducesMessageCode()
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
            Assert.NotEmpty(resultList);
            var messageResult = resultList.FirstOrDefault(r => r.name.Contains("TestMessage"));
            Assert.NotEqual(default, messageResult);
            Assert.Contains("TestMessage", messageResult.content);
            Assert.Contains("Field1", messageResult.content);
            Assert.Contains("Field2", messageResult.content);
        }

        [Fact]
        public void Generate_WithMessageContainingConstants_ProducesCodeWithConstants()
        {
            // Arrange
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='TestMessage' id='1' description='A test message'>
                        <field name='field1' id='1' type='uint32'/>
                        <field name='messageType' id='2' type='uint8' presence='constant' valueRef='TestMessage'/>
                    </sbe:message>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.NotEmpty(resultList);
            var messageResult = resultList.FirstOrDefault(r => r.name.Contains("TestMessage"));
            Assert.NotEqual(default, messageResult);
            Assert.Contains("TestMessage", messageResult.content);
        }

        [Fact]
        public void Generate_WithMultipleMessages_ProducesMultipleFiles()
        {
            // Arrange
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='Message1' id='1' description='First message'>
                        <field name='field1' id='1' type='uint32'/>
                    </sbe:message>
                    <sbe:message name='Message2' id='2' description='Second message'>
                        <field name='field2' id='1' type='uint64'/>
                    </sbe:message>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.NotEmpty(resultList);
            Assert.Contains(resultList, r => r.name.Contains("Message1"));
            Assert.Contains(resultList, r => r.name.Contains("Message2"));
        }

        [Fact]
        public void Generate_WithMessages_GeneratesParser()
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
            var parserResult = resultList.FirstOrDefault(r => r.name.Contains("MessageParser"));
            Assert.NotEqual(default, parserResult);
            Assert.Contains("MessageParser", parserResult.content);
        }
    }
}
