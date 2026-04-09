using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using System.Linq;
using SbeSourceGenerator.Schema;
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
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='TestMessage' id='1' description='A test message'>
                        <field name='field1' id='1' type='uint32'/>
                        <field name='field2' id='2' type='uint64'/>
                    </sbe:message>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

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
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='TestMessage' id='1' description='A test message'>
                        <field name='field1' id='1' type='uint32'/>
                        <field name='messageType' id='2' type='uint8' presence='constant' valueRef='TestMessage'/>
                    </sbe:message>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

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
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='Message1' id='1' description='First message'>
                        <field name='field1' id='1' type='uint32'/>
                    </sbe:message>
                    <sbe:message name='Message2' id='2' description='Second message'>
                        <field name='field2' id='1' type='uint64'/>
                    </sbe:message>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

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
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='TestMessage' id='1' description='A test message'>
                        <field name='activeField' id='1' type='uint32' description='Active field'/>
                        <field name='deprecatedField' id='2' type='uint64' deprecated='1' description='Deprecated field'/>
                    </sbe:message>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

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
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='TestMessage' id='1' description='A test message'>
                        <field name='activeField' id='1' type='uint32' description='Active field'/>
                        <field name='legacyField' id='2' type='uint64' sinceVersion='1' deprecated='2' description='Legacy field'/>
                        <field name='newField' id='3' type='uint64' sinceVersion='2' description='New field'/>
                    </sbe:message>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            
            // Check V1 version which should have the deprecated field
            var v1Result = resultList.FirstOrDefault(r => r.name.Contains("V1") && r.name.Contains("TestMessage"));
            Assert.NotEqual(default, v1Result);
            
            // Check that deprecated field has [Obsolete] with version info
            Assert.Contains("[Obsolete(", v1Result.content);
            Assert.Contains("version", v1Result.content.ToLower());
        }

        [Fact]
        public void Generate_WithGroupUsingUint16NumInGroup_ProducesUshortCast()
        {
            // Arrange - Schema with uint16 numInGroup (standard SBE default)
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                                   package='test' id='1' version='0'>
                    <types>
                        <composite name='MessageHeader'>
                            <type name='blockLength' primitiveType='uint16'/>
                            <type name='templateId' primitiveType='uint16'/>
                            <type name='schemaId' primitiveType='uint16'/>
                            <type name='version' primitiveType='uint16'/>
                        </composite>
                        <composite name='GroupSizeEncoding'>
                            <type name='blockLength' primitiveType='uint16'/>
                            <type name='numInGroup' primitiveType='uint16'/>
                        </composite>
                    </types>
                    <sbe:message name='TestMessage' id='1' description='Test'>
                        <field name='id' id='1' type='uint64'/>
                        <group name='entries' id='2' dimensionType='GroupSizeEncoding'>
                            <field name='price' id='3' type='int64'/>
                            <field name='quantity' id='4' type='int64'/>
                        </group>
                    </sbe:message>
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");

            // Act - Run TypesCodeGenerator first to populate CompositeFieldTypes
            var typesGenerator = new TypesCodeGenerator();
            _ = typesGenerator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            var messagesGenerator = new MessagesCodeGenerator();
            var results = messagesGenerator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            // Assert
            Assert.NotEmpty(results);
            var messageResult = results.First();
            // Should cast to ushort (uint16), NOT uint (uint32)
            Assert.Contains("(ushort)", messageResult.content);
            Assert.DoesNotContain("(uint)entries", messageResult.content);
        }

        [Fact]
        public void Generate_WithGroupUsingUint32NumInGroup_ProducesUintCast()
        {
            // Arrange - Schema with uint32 numInGroup (like B3 market data)
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                                   package='test' id='1' version='0'>
                    <types>
                        <composite name='MessageHeader'>
                            <type name='blockLength' primitiveType='uint16'/>
                            <type name='templateId' primitiveType='uint16'/>
                            <type name='schemaId' primitiveType='uint16'/>
                            <type name='version' primitiveType='uint16'/>
                        </composite>
                        <composite name='GroupSizeEncoding'>
                            <type name='blockLength' primitiveType='uint16'/>
                            <type name='numInGroup' primitiveType='uint32'/>
                        </composite>
                    </types>
                    <sbe:message name='TestMessage' id='1' description='Test'>
                        <field name='id' id='1' type='uint64'/>
                        <group name='entries' id='2' dimensionType='GroupSizeEncoding'>
                            <field name='price' id='3' type='int64'/>
                            <field name='quantity' id='4' type='int64'/>
                        </group>
                    </sbe:message>
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");

            // Act - Run TypesCodeGenerator first to populate CompositeFieldTypes
            var typesGenerator = new TypesCodeGenerator();
            _ = typesGenerator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            var messagesGenerator = new MessagesCodeGenerator();
            var results = messagesGenerator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            // Assert
            Assert.NotEmpty(results);
            var messageResult = results.First();
            // Should cast to uint (uint32)
            Assert.Contains("(uint)", messageResult.content);
        }

        [Fact]
        public void Generate_WithDataSinceVersion_ExcludesDataFromEarlierVersions()
        {
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                                   package='test' id='1' version='0'>
                    <types>
                        <composite name='MessageHeader'>
                            <type name='blockLength' primitiveType='uint16'/>
                            <type name='templateId' primitiveType='uint16'/>
                            <type name='schemaId' primitiveType='uint16'/>
                            <type name='version' primitiveType='uint16'/>
                        </composite>
                        <composite name='VarString8'>
                            <type name='length' primitiveType='uint8'/>
                            <type name='varData' length='0' primitiveType='uint8'/>
                        </composite>
                    </types>
                    <sbe:message name='Order' id='1' description='Order message'>
                        <field name='orderId' id='1' type='uint64'/>
                        <data name='symbol' id='2' type='VarString8'/>
                        <data name='memo' id='3' type='VarString8' sinceVersion='1'/>
                    </sbe:message>
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");
            var typesGenerator = new TypesCodeGenerator();
            _ = typesGenerator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            var generator = new MessagesCodeGenerator();
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            // V0 should have symbol but NOT memo
            var v0Result = results.FirstOrDefault(r => r.name.Contains("Order") && !r.name.Contains("V1"));
            Assert.NotEqual(default, v0Result);
            Assert.Contains("Symbol", v0Result.content);
            Assert.DoesNotContain("Memo", v0Result.content);

            // V1 should have both symbol and memo
            var v1Result = results.FirstOrDefault(r => r.name.Contains("V1") && r.name.Contains("Order"));
            Assert.NotEqual(default, v1Result);
            Assert.Contains("Symbol", v1Result.content);
            Assert.Contains("Memo", v1Result.content);
        }

        [Fact]
        public void Generate_WithExplicitBlockLength_UsesSchemaBlockLength()
        {
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                                   package='test' id='1' version='0'>
                    <sbe:message name='PaddedMessage' id='1' blockLength='32' description='Message with padding'>
                        <field name='orderId' id='1' type='uint64'/>
                        <field name='price' id='2' type='int64'/>
                    </sbe:message>
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");
            var generator = new MessagesCodeGenerator();
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            var msgResult = results.FirstOrDefault(r => r.name.Contains("PaddedMessage"));
            Assert.NotEqual(default, msgResult);
            // MESSAGE_SIZE should be 16 (8+8), but BLOCK_LENGTH should be 32
            Assert.Contains("BLOCK_LENGTH = 32", msgResult.content);
            Assert.Contains("MESSAGE_SIZE = 16", msgResult.content);
        }

        [Fact]
        public void Generate_WithoutExplicitBlockLength_BlockLengthEqualsMessageSize()
        {
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                                   package='test' id='1' version='0'>
                    <sbe:message name='NormalMessage' id='1' description='Normal message'>
                        <field name='orderId' id='1' type='uint64'/>
                        <field name='price' id='2' type='int64'/>
                    </sbe:message>
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");
            var generator = new MessagesCodeGenerator();
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            var msgResult = results.FirstOrDefault(r => r.name.Contains("NormalMessage"));
            Assert.NotEqual(default, msgResult);
            // BLOCK_LENGTH should equal MESSAGE_SIZE when not explicitly set
            Assert.Contains("BLOCK_LENGTH = MESSAGE_SIZE", msgResult.content);
        }

        [Fact]
        public void Generate_WithVarString16_UsesUshortLengthPrefix()
        {
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                                   package='test' id='1' version='0'>
                    <types>
                        <composite name='MessageHeader'>
                            <type name='blockLength' primitiveType='uint16'/>
                            <type name='templateId' primitiveType='uint16'/>
                            <type name='schemaId' primitiveType='uint16'/>
                            <type name='version' primitiveType='uint16'/>
                        </composite>
                        <composite name='VarString16'>
                            <type name='length' primitiveType='uint16'/>
                            <type name='varData' length='0' primitiveType='uint8'/>
                        </composite>
                    </types>
                    <sbe:message name='LargeMessage' id='1' description='Message with large varData'>
                        <field name='id' id='1' type='uint64'/>
                        <data name='payload' id='2' type='VarString16'/>
                    </sbe:message>
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");
            var typesGenerator = new TypesCodeGenerator();
            _ = typesGenerator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            var generator = new MessagesCodeGenerator();
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            var msgResult = results.FirstOrDefault(r => r.name.Contains("LargeMessage"));
            Assert.NotEqual(default, msgResult);
            // Should use ushort cast and 65535 max, NOT byte/255
            Assert.Contains("(ushort)data.Length", msgResult.content);
            Assert.Contains("65535", msgResult.content);
            Assert.DoesNotContain("(byte)data.Length", msgResult.content);
        }

        [Fact]
        public void Generate_WithDataInsideGroup_GeneratesGroupDataCallbacks()
        {
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                                   package='test' id='1' version='0'>
                    <types>
                        <composite name='MessageHeader'>
                            <type name='blockLength' primitiveType='uint16'/>
                            <type name='templateId' primitiveType='uint16'/>
                            <type name='schemaId' primitiveType='uint16'/>
                            <type name='version' primitiveType='uint16'/>
                        </composite>
                        <composite name='GroupSizeEncoding'>
                            <type name='blockLength' primitiveType='uint16'/>
                            <type name='numInGroup' primitiveType='uint16'/>
                        </composite>
                        <composite name='VarString8'>
                            <type name='length' primitiveType='uint8'/>
                            <type name='varData' length='0' primitiveType='uint8'/>
                        </composite>
                    </types>
                    <sbe:message name='OrderMessage' id='1' description='Order with group data'>
                        <field name='id' id='1' type='uint64'/>
                        <group name='orders' id='2' dimensionType='GroupSizeEncoding'>
                            <field name='orderId' id='3' type='uint64'/>
                            <data name='notes' id='4' type='VarString8'/>
                        </group>
                    </sbe:message>
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");
            var typesGenerator = new TypesCodeGenerator();
            _ = typesGenerator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            var generator = new MessagesCodeGenerator();
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            var msgResult = results.FirstOrDefault(r => r.name.Contains("OrderMessage"));
            Assert.NotEqual(default, msgResult);
            // Group data callback should be in ConsumeVariableLengthSegments signature
            Assert.Contains("callbackOrdersNotes", msgResult.content);
            // Group data should be read inside the group loop (after reading group entry)
            Assert.Contains("VarString8.Create", msgResult.content);
        }

    }
}
