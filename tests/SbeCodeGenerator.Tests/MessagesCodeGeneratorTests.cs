using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using System.Linq;
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
            var context = new SchemaContext("test-schema");
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
            var context = new SchemaContext("test-schema");
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
            var context = new SchemaContext("test-schema");
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
        public void Generate_WithDeprecatedFields_AddsObsoleteAttribute()
        {
            // Arrange
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='TestMessage' id='1' description='A test message'>
                        <field name='activeField' id='1' type='uint32' description='Active field'/>
                        <field name='deprecatedField' id='2' type='uint64' deprecated='1' description='Deprecated field'/>
                    </sbe:message>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.NotEmpty(resultList);
            var messageResult = resultList.FirstOrDefault(r => r.name.Contains("TestMessage"));
            Assert.NotEqual(default, messageResult);
            
            // Check that deprecated field has [Obsolete] attribute
            Assert.Contains("[Obsolete(", messageResult.content);
            Assert.Contains("deprecated", messageResult.content.ToLower());
            
            // Verify both fields are present
            Assert.Contains("ActiveField", messageResult.content);
            Assert.Contains("DeprecatedField", messageResult.content);
        }

        [Fact]
        public void Generate_WithDeprecatedFieldWithSinceVersion_AddsVersionToObsoleteMessage()
        {
            // Arrange
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='TestMessage' id='1' description='A test message'>
                        <field name='activeField' id='1' type='uint32' description='Active field'/>
                        <field name='legacyField' id='2' type='uint64' sinceVersion='1' deprecated='2' description='Legacy field'/>
                        <field name='newField' id='3' type='uint64' sinceVersion='2' description='New field'/>
                    </sbe:message>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            
            // Check V1 version which should have the deprecated field
            var v1Result = resultList.FirstOrDefault(r => r.name.Contains("V1") && r.name.Contains("TestMessage"));
            Assert.NotEqual(default, v1Result);
            
            // Check that deprecated field has [Obsolete] with version info
            Assert.Contains("[Obsolete(", v1Result.content);
            Assert.Contains("version", v1Result.content.ToLower());
        }

    }
}
