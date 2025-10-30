using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using SbeSourceGenerator.Generators.Types;
using System.Linq;
using System.Xml;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    public class MessageParsingHelpersTests
    {
        [Fact]
        public void MessagesIncludeTryParseHelper()
        {
            // Arrange
            var context = new SchemaContext("test-schema");
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='TestMessage' id='1' description='A test message'>
                        <field name='field1' id='1' type='uint32'/>
                    </sbe:message>
                </sbe:messageSchema>");

            var generator = new MessagesCodeGenerator();

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default);

            // Assert
            var messageResult = results.FirstOrDefault(r => r.name.Contains("TestMessage"));
            Assert.NotEqual(default, messageResult);
            Assert.Contains("public static bool TryParse", messageResult.content);
        }

        [Fact]
        public void CompositesIncludeTryParseHelper()
        {
            // Arrange
            var context = new SchemaContext("test-schema");
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <types>
                        <composite name='MessageHeader' description='Header'>
                            <field name='blockLength' id='1' primitiveType='uint16'/>
                            <field name='templateId' id='2' primitiveType='uint16'/>
                        </composite>
                    </types>
                </sbe:messageSchema>");

            var generator = new TypesCodeGenerator();

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default);

            // Assert
            var compositeResult = results.FirstOrDefault(r => r.name.Contains("MessageHeader"));
            Assert.NotEqual(default, compositeResult);
            Assert.Contains("public static bool TryParse", compositeResult.content);
        }
    }
}
