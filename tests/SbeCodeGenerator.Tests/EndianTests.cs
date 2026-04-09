using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using SbeSourceGenerator.Schema;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Text;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    public class EndianTests
    {
        [Fact]
        public void SchemaContext_DefaultsByteOrder_IsLittleEndian()
        {
            var context = new SchemaContext("test-schema");
            Assert.Equal("littleEndian", context.ByteOrder);
        }

        [Fact]
        public void SchemaContext_DefaultEndianConversion_IsNone()
        {
            var context = new SchemaContext("test-schema");
            Assert.Equal(EndianConversion.None, context.EndianConversion);
        }

        [Fact]
        public void ParseSchema_WithLittleEndianAttribute_SetsLittleEndian()
        {
            var schema = SchemaReader.Parse(@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <sbe:messageSchema xmlns:sbe=""http://fixprotocol.io/2016/sbe""
                                   byteOrder=""littleEndian"">
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");
            if (!string.IsNullOrEmpty(schema.ByteOrder))
                context.ByteOrder = schema.ByteOrder;

            Assert.Equal("littleEndian", context.ByteOrder);
        }

        [Fact]
        public void ParseSchema_WithBigEndianAttribute_SetsBigEndian()
        {
            var schema = SchemaReader.Parse(@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <sbe:messageSchema xmlns:sbe=""http://fixprotocol.io/2016/sbe""
                                   byteOrder=""bigEndian"">
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");
            if (!string.IsNullOrEmpty(schema.ByteOrder))
                context.ByteOrder = schema.ByteOrder;

            Assert.Equal("bigEndian", context.ByteOrder);
        }

        [Fact]
        public void ParseSchema_WithoutByteOrderAttribute_DefaultsToLittleEndian()
        {
            var schema = SchemaReader.Parse(@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <sbe:messageSchema xmlns:sbe=""http://fixprotocol.io/2016/sbe"">
                </sbe:messageSchema>");

            var context = new SchemaContext("test-schema");
            if (!string.IsNullOrEmpty(schema.ByteOrder))
                context.ByteOrder = schema.ByteOrder;

            Assert.Equal("littleEndian", context.ByteOrder);
        }

        // --- EndianFieldHelper unit tests ---

        [Theory]
        [InlineData("byte", EndianConversion.AlwaysReverse, false)]
        [InlineData("sbyte", EndianConversion.AlwaysReverse, false)]
        [InlineData("char", EndianConversion.AlwaysReverse, false)]
        [InlineData("int", EndianConversion.None, false)]
        [InlineData("int", EndianConversion.AlwaysReverse, true)]
        [InlineData("ushort", EndianConversion.Conditional, true)]
        [InlineData("long", EndianConversion.AlwaysReverse, true)]
        [InlineData("float", EndianConversion.AlwaysReverse, true)]
        [InlineData("double", EndianConversion.Conditional, true)]
        public void NeedsConversion_ReturnsExpected(string type, EndianConversion conversion, bool expected)
        {
            Assert.Equal(expected, EndianFieldHelper.NeedsConversion(type, conversion));
        }

        [Fact]
        public void AppendField_None_EmitsPublicField()
        {
            var sb = new StringBuilder();
            EndianFieldHelper.AppendField(sb, 0, "int", "Price", EndianConversion.None);
            Assert.Contains("public int Price;", sb.ToString());
            Assert.DoesNotContain("private", sb.ToString());
        }

        [Fact]
        public void AppendField_AlwaysReverse_EmitsPrivateFieldAndProperty()
        {
            var sb = new StringBuilder();
            EndianFieldHelper.AppendField(sb, 0, "int", "Price", EndianConversion.AlwaysReverse);
            var result = sb.ToString();
            Assert.Contains("private int price;", result);
            Assert.Contains("public int Price", result);
            Assert.Contains("BinaryPrimitives.ReverseEndianness(price)", result);
        }

        [Fact]
        public void AppendField_Conditional_EmitsConditionalProperty()
        {
            var sb = new StringBuilder();
            EndianFieldHelper.AppendField(sb, 0, "int", "Price", EndianConversion.Conditional);
            var result = sb.ToString();
            Assert.Contains("private int price;", result);
            Assert.Contains("BitConverter.IsLittleEndian", result);
            Assert.Contains("BinaryPrimitives.ReverseEndianness(price)", result);
        }

        [Fact]
        public void AppendField_SingleByte_AlwaysPassthrough()
        {
            var sb = new StringBuilder();
            EndianFieldHelper.AppendField(sb, 0, "byte", "Tag", EndianConversion.AlwaysReverse);
            Assert.Contains("public byte Tag;", sb.ToString());
            Assert.DoesNotContain("private", sb.ToString());
        }

        [Fact]
        public void AppendMessageField_AlwaysReverse_EmitsFieldOffsetAndProperty()
        {
            var sb = new StringBuilder();
            EndianFieldHelper.AppendMessageField(sb, 0, "uint", "uint", "Price", 4, EndianConversion.AlwaysReverse);
            var result = sb.ToString();
            Assert.Contains("[FieldOffset(4)]", result);
            Assert.Contains("private uint price;", result);
            Assert.Contains("BinaryPrimitives.ReverseEndianness(price)", result);
        }

        [Fact]
        public void AppendMessageField_None_EmitsPublicField()
        {
            var sb = new StringBuilder();
            EndianFieldHelper.AppendMessageField(sb, 0, "uint", "uint", "Price", 4, EndianConversion.None);
            var result = sb.ToString();
            Assert.Contains("[FieldOffset(4)]", result);
            Assert.Contains("public uint Price;", result);
        }

        [Fact]
        public void GetterExpression_Float_UsesInt32BitsConversion()
        {
            var expr = EndianFieldHelper.GetterExpression("float", "price", EndianConversion.AlwaysReverse);
            Assert.Contains("BitConverter.Int32BitsToSingle", expr);
            Assert.Contains("SingleToInt32Bits", expr);
        }

        [Fact]
        public void GetterExpression_Double_UsesInt64BitsConversion()
        {
            var expr = EndianFieldHelper.GetterExpression("double", "price", EndianConversion.AlwaysReverse);
            Assert.Contains("BitConverter.Int64BitsToDouble", expr);
            Assert.Contains("DoubleToInt64Bits", expr);
        }

        // --- Enum cast pattern ---

        [Fact]
        public void CastGetterExpression_Enum_AlwaysReverse_EmitsCastAndReverse()
        {
            var expr = EndianFieldHelper.CastGetterExpression("MyEnum", "ushort", "myField", EndianConversion.AlwaysReverse);
            Assert.Contains("(MyEnum)", expr);
            Assert.Contains("BinaryPrimitives.ReverseEndianness", expr);
            Assert.Contains("(ushort)myField", expr);
        }

        [Fact]
        public void CastGetterExpression_Enum_Conditional_EmitsConditionalCast()
        {
            var expr = EndianFieldHelper.CastGetterExpression("MyEnum", "ushort", "myField", EndianConversion.Conditional);
            Assert.Contains("BitConverter.IsLittleEndian", expr);
            Assert.Contains("(MyEnum)", expr);
            Assert.Contains("myField", expr);
        }

        [Fact]
        public void CastSetterExpression_Enum_AlwaysReverse_EmitsCastAndReverse()
        {
            var expr = EndianFieldHelper.CastSetterExpression("MyEnum", "ushort", "value", EndianConversion.AlwaysReverse);
            Assert.Contains("(MyEnum)", expr);
            Assert.Contains("BinaryPrimitives.ReverseEndianness", expr);
            Assert.Contains("(ushort)value", expr);
        }

        // --- Integration with generators ---

        [Fact]
        public void Generate_BigEndianSchema_MessageFields_HaveReverseEndianness()
        {
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext("test-schema");
            context.ByteOrder = "bigEndian";
            context.EndianConversion = EndianConversion.AlwaysReverse;

            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe' byteOrder='bigEndian'>
                    <sbe:message name='TestMessage' id='1' description='Test'>
                        <field name='price' id='1' type='uint32'/>
                        <field name='quantity' id='2' type='uint64'/>
                    </sbe:message>
                </sbe:messageSchema>");

            var results = generator.Generate("TestNs", schema, context, default(SourceProductionContext)).ToList();
            Assert.NotEmpty(results);

            var content = results[0].content;
            Assert.Contains("BinaryPrimitives.ReverseEndianness", content);
            Assert.Contains("private uint", content);
            Assert.Contains("private ulong", content);
            Assert.Contains("public uint Price", content);
            Assert.Contains("public ulong Quantity", content);
            Assert.Contains("using System.Buffers.Binary;", content);
        }

        [Fact]
        public void Generate_LittleEndianSchema_MessageFields_ArePublicFields()
        {
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext("test-schema");

            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='TestMessage' id='1' description='Test'>
                        <field name='price' id='1' type='uint32'/>
                        <field name='quantity' id='2' type='uint64'/>
                    </sbe:message>
                </sbe:messageSchema>");

            var results = generator.Generate("TestNs", schema, context, default(SourceProductionContext)).ToList();
            var content = results[0].content;
            Assert.Contains("public uint Price;", content);
            Assert.Contains("public ulong Quantity;", content);
            Assert.DoesNotContain("BinaryPrimitives.ReverseEndianness", content);
        }

        [Fact]
        public void Generate_BigEndianSchema_SingleByteFields_ArePassthrough()
        {
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext("test-schema");
            context.ByteOrder = "bigEndian";
            context.EndianConversion = EndianConversion.AlwaysReverse;

            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe' byteOrder='bigEndian'>
                    <sbe:message name='TestMessage' id='1' description='Test'>
                        <field name='tag' id='1' type='uint8'/>
                        <field name='price' id='2' type='uint32'/>
                    </sbe:message>
                </sbe:messageSchema>");

            var results = generator.Generate("TestNs", schema, context, default(SourceProductionContext)).ToList();
            var content = results[0].content;
            // Single byte field should be a public field
            Assert.Contains("public byte Tag;", content);
            // Multi-byte field should use endian conversion
            Assert.Contains("private uint", content);
        }

        [Fact]
        public void Generate_BigEndianSchema_Conditional_EmitsBitConverterCheck()
        {
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext("test-schema");
            context.ByteOrder = "bigEndian";
            context.EndianConversion = EndianConversion.Conditional;

            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe' byteOrder='bigEndian'>
                    <sbe:message name='TestMessage' id='1' description='Test'>
                        <field name='price' id='1' type='uint32'/>
                    </sbe:message>
                </sbe:messageSchema>");

            var results = generator.Generate("TestNs", schema, context, default(SourceProductionContext)).ToList();
            var content = results[0].content;
            Assert.Contains("BitConverter.IsLittleEndian", content);
        }

        [Fact]
        public void Generate_BigEndianSchema_CompositeFields_HaveReverseEndianness()
        {
            var typesGen = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            context.ByteOrder = "bigEndian";
            context.EndianConversion = EndianConversion.AlwaysReverse;

            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe' byteOrder='bigEndian'>
                    <types>
                        <composite name='decimal' description='Fixed-point decimal'>
                            <type name='mantissa' primitiveType='int64'/>
                            <type name='exponent' primitiveType='int8'/>
                        </composite>
                    </types>
                </sbe:messageSchema>");

            var results = typesGen.Generate("TestNs", schema, context, default(SourceProductionContext)).ToList();
            var decimalResult = results.FirstOrDefault(r => r.name.Contains("Decimal"));
            Assert.NotEqual(default, decimalResult);

            // int64 (long) should have endian conversion, int8 (sbyte) should not
            Assert.Contains("BinaryPrimitives.ReverseEndianness", decimalResult.content);
            Assert.Contains("using System.Buffers.Binary;", decimalResult.content);
        }

        [Fact]
        public void Generate_BigEndianSchema_EnumField_HasCastAndReverse()
        {
            var typesGen = new TypesCodeGenerator();
            var messagesGen = new MessagesCodeGenerator();
            var context = new SchemaContext("test-schema");
            context.ByteOrder = "bigEndian";
            context.EndianConversion = EndianConversion.AlwaysReverse;

            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe' byteOrder='bigEndian'>
                    <types>
                        <enum name='Side' encodingType='uint16'>
                            <validValue name='Buy'>1</validValue>
                            <validValue name='Sell'>2</validValue>
                        </enum>
                    </types>
                    <sbe:message name='Order' id='1' description='An order'>
                        <field name='side' id='1' type='Side'/>
                    </sbe:message>
                </sbe:messageSchema>");

            // Run types first to register enum
            typesGen.Generate("TestNs", schema, context, default(SourceProductionContext)).ToList();

            var results = messagesGen.Generate("TestNs", schema, context, default(SourceProductionContext)).ToList();
            var content = results[0].content;

            // Enum with ushort underlying should have cast + reverse pattern
            Assert.Contains("BinaryPrimitives.ReverseEndianness", content);
            Assert.Contains("(ushort)", content);
            Assert.Contains("(Side)", content);
        }

        [Fact]
        public void Generate_BigEndianSchema_ByteEnum_IsPassthrough()
        {
            var typesGen = new TypesCodeGenerator();
            var messagesGen = new MessagesCodeGenerator();
            var context = new SchemaContext("test-schema");
            context.ByteOrder = "bigEndian";
            context.EndianConversion = EndianConversion.AlwaysReverse;

            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe' byteOrder='bigEndian'>
                    <types>
                        <enum name='Side' encodingType='uint8'>
                            <validValue name='Buy'>1</validValue>
                            <validValue name='Sell'>2</validValue>
                        </enum>
                    </types>
                    <sbe:message name='Order' id='1' description='An order'>
                        <field name='side' id='1' type='Side'/>
                    </sbe:message>
                </sbe:messageSchema>");

            typesGen.Generate("TestNs", schema, context, default(SourceProductionContext)).ToList();
            var results = messagesGen.Generate("TestNs", schema, context, default(SourceProductionContext)).ToList();
            var content = results[0].content;

            // byte-backed enum should NOT have endian conversion
            Assert.Contains("public Side Side;", content);
            Assert.DoesNotContain("BinaryPrimitives.ReverseEndianness", content);
        }

        [Fact]
        public void Generate_BigEndianSchema_OptionalField_HasEndianConversion()
        {
            var messagesGen = new MessagesCodeGenerator();
            var context = new SchemaContext("test-schema");
            context.ByteOrder = "bigEndian";
            context.EndianConversion = EndianConversion.AlwaysReverse;

            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe' byteOrder='bigEndian'>
                    <sbe:message name='TestMessage' id='1' description='Test'>
                        <field name='price' id='1' type='uint32' presence='optional'/>
                    </sbe:message>
                </sbe:messageSchema>");

            var results = messagesGen.Generate("TestNs", schema, context, default(SourceProductionContext)).ToList();
            var content = results[0].content;

            Assert.Contains("BinaryPrimitives.ReverseEndianness", content);
            Assert.Contains("private uint", content);
        }
    }
}
