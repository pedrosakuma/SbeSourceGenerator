using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using SbeSourceGenerator.Schema;
using Microsoft.CodeAnalysis;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    public class TypesCodeGeneratorTests
    {
        [Fact]
        public void Generate_WithSimpleEnum_ProducesEnumCode()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <enum name='TestEnum' encodingType='uint8'>
                            <validValue name='Value1'>0</validValue>
                            <validValue name='Value2'>1</validValue>
                        </enum>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.NotEmpty(resultList);
            var enumResult = resultList.FirstOrDefault(r => r.name.Contains("TestEnum"));
            Assert.NotEqual(default, enumResult);
            Assert.Contains("TestEnum", enumResult.content);
            Assert.Contains("Value1", enumResult.content);
            Assert.Contains("Value2", enumResult.content);
        }

        [Fact]
        public void Generate_WithSimpleType_ProducesTypeCode()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <type name='CustomType' primitiveType='uint32' description='A custom type'/>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.NotEmpty(resultList);
            var typeResult = resultList.FirstOrDefault(r => r.name.Contains("CustomType"));
            Assert.NotEqual(default, typeResult);
            Assert.Contains("CustomType", typeResult.content);
        }

        [Fact]
        public void Generate_WithComposite_ProducesCompositeCode()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <composite name='TestComposite' description='Test composite'>
                            <type name='field1' primitiveType='uint16'/>
                            <type name='field2' primitiveType='uint32'/>
                        </composite>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.NotEmpty(resultList);
            var compositeResult = resultList.FirstOrDefault(r => r.name.Contains("TestComposite"));
            Assert.NotEqual(default, compositeResult);
            Assert.Contains("TestComposite", compositeResult.content);
        }

        [Fact]
        public void Generate_WithCompositeCharArray_ProducesInlineArrayAndCorrectSize()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <composite name='engine' description='Engine composite'>
                            <type name='capacity' primitiveType='uint16'/>
                            <type name='numCylinders' primitiveType='uint8'/>
                            <type name='manufacturerCode' primitiveType='char' length='3'/>
                        </composite>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));
            var resultList = results.ToList();

            // Assert - InlineArray type generated for manufacturerCode
            var charArrayType = resultList.FirstOrDefault(r => r.name.Contains("ManufacturerCode"));
            Assert.NotEqual(default, charArrayType);
            Assert.Contains("InlineArray(3)", charArrayType.content);

            // Assert - Composite uses the InlineArray type as a field
            var compositeResult = resultList.FirstOrDefault(r => r.name.Contains("Engine"));
            Assert.NotEqual(default, compositeResult);
            Assert.Contains("ManufacturerCode", compositeResult.content);

            // Assert - MESSAGE_SIZE is correct: uint16(2) + uint8(1) + char[3](3) = 6
            Assert.Contains("MESSAGE_SIZE = 6", compositeResult.content);
        }

        [Fact]
        public void Generate_WithSet_NormalizesChoiceNames()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <set name='TestSet' encodingType='uint8'>
                            <choice name='sunRoof'>0</choice>
                            <choice name='sportsPack'>1</choice>
                        </set>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));
            var setResult = results.First(r => r.name.Contains("TestSet"));

            Assert.Contains("SunRoof", setResult.content);
            Assert.Contains("SportsPack", setResult.content);
            Assert.DoesNotContain("sunRoof", setResult.content);
            Assert.DoesNotContain("sportsPack", setResult.content);
        }

        [Fact]
        public void Generate_WithEnum_NormalizesValueNames()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <enum name='TestEnum' encodingType='uint8'>
                            <validValue name='fooBar'>0</validValue>
                            <validValue name='bazQux'>1</validValue>
                        </enum>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));
            var enumResult = results.First(r => r.name.Contains("TestEnum"));

            Assert.Contains("FooBar", enumResult.content);
            Assert.Contains("BazQux", enumResult.content);
            Assert.DoesNotContain("fooBar", enumResult.content);
            Assert.DoesNotContain("bazQux", enumResult.content);
        }

        [Fact]
        public void Generate_WithSet_ProducesSetCode()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <set name='TestSet' encodingType='uint8'>
                            <choice name='Flag1'>0</choice>
                            <choice name='Flag2'>1</choice>
                        </set>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.NotEmpty(resultList);
            var setResult = resultList.FirstOrDefault(r => r.name.Contains("TestSet"));
            Assert.NotEqual(default, setResult);
            Assert.Contains("TestSet", setResult.content);
            Assert.Contains("Flag1", setResult.content);
            Assert.Contains("Flag2", setResult.content);
        }

        [Fact]
        public void Generate_WithSet_ChoiceExceedsBitWidth_ExcludesInvalidChoice()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <set name='BadSet' encodingType='uint8'>
                            <choice name='ValidFlag'>7</choice>
                            <choice name='InvalidFlag'>8</choice>
                        </set>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            var resultList = results.ToList();
            var setResult = resultList.FirstOrDefault(r => r.name.Contains("BadSet"));
            Assert.NotEqual(default, setResult);
            Assert.Contains("ValidFlag", setResult.content);
            Assert.DoesNotContain("InvalidFlag", setResult.content);
        }

        [Fact]
        public void Generate_WithSet_Uint8MaxBitPosition_IsValid()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <set name='FullSet' encodingType='uint8'>
                            <choice name='Bit0'>0</choice>
                            <choice name='Bit1'>1</choice>
                            <choice name='Bit2'>2</choice>
                            <choice name='Bit3'>3</choice>
                            <choice name='Bit4'>4</choice>
                            <choice name='Bit5'>5</choice>
                            <choice name='Bit6'>6</choice>
                            <choice name='Bit7'>7</choice>
                        </set>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            var resultList = results.ToList();
            var setResult = resultList.FirstOrDefault(r => r.name.Contains("FullSet"));
            Assert.NotEqual(default, setResult);
            Assert.Contains("Bit0", setResult.content);
            Assert.Contains("Bit7", setResult.content);
        }

        [Fact]
        public void Generate_WithSet_Uint16_ChoiceExceedsBitWidth_ExcludesInvalidChoice()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <set name='WideSet' encodingType='uint16'>
                            <choice name='ValidFlag'>15</choice>
                            <choice name='InvalidFlag'>16</choice>
                        </set>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            var resultList = results.ToList();
            var setResult = resultList.FirstOrDefault(r => r.name.Contains("WideSet"));
            Assert.NotEqual(default, setResult);
            Assert.Contains("ValidFlag", setResult.content);
            Assert.DoesNotContain("InvalidFlag", setResult.content);
        }

        [Fact]
        public void Generate_WithSet_Uint64_MaxBitPosition63_IsValid()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <set name='BigSet' encodingType='uint64'>
                            <choice name='LowBit'>0</choice>
                            <choice name='HighBit'>63</choice>
                        </set>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            var resultList = results.ToList();
            var setResult = resultList.FirstOrDefault(r => r.name.Contains("BigSet"));
            Assert.NotEqual(default, setResult);
            Assert.Contains("LowBit", setResult.content);
            Assert.Contains("HighBit", setResult.content);
        }

        [Fact]
        public void Generate_WithEnum_DeprecatedValue_GeneratesObsoleteAttribute()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <enum name='OrderType' encodingType='uint8'>
                            <validValue name='Market'>0</validValue>
                            <validValue name='Limit'>1</validValue>
                            <validValue name='StopLoss' deprecated='2'>2</validValue>
                        </enum>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            var resultList = results.ToList();
            var enumResult = resultList.FirstOrDefault(r => r.name.Contains("OrderType"));
            Assert.NotEqual(default, enumResult);
            Assert.Contains("[Obsolete(", enumResult.content);
            Assert.Contains("StopLoss", enumResult.content);
            Assert.DoesNotContain("[Obsolete(", enumResult.content.Split("Market")[0]);
        }

        [Fact]
        public void Generate_WithEnum_SinceVersionOnValue_GeneratesVersionComment()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <enum name='Side' encodingType='uint8'>
                            <validValue name='Buy'>0</validValue>
                            <validValue name='Sell'>1</validValue>
                            <validValue name='SellShort' sinceVersion='2'>2</validValue>
                        </enum>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            var resultList = results.ToList();
            var enumResult = resultList.FirstOrDefault(r => r.name.Contains("Side"));
            Assert.NotEqual(default, enumResult);
            Assert.Contains("Since version 2", enumResult.content);
        }

        [Fact]
        public void Generate_WithEnum_DeprecatedWithSinceVersion_GeneratesDetailedObsolete()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <enum name='TimeInForce' encodingType='uint8'>
                            <validValue name='Day'>0</validValue>
                            <validValue name='GoodTillCancel' sinceVersion='1' deprecated='3'>1</validValue>
                        </enum>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            var resultList = results.ToList();
            var enumResult = resultList.FirstOrDefault(r => r.name.Contains("TimeInForce"));
            Assert.NotEqual(default, enumResult);
            Assert.Contains("[Obsolete(\"This value is deprecated since version 1\")]", enumResult.content);
        }

        [Fact]
        public void Generate_WithSet_DeprecatedChoice_GeneratesObsoleteAttribute()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <set name='Capabilities' encodingType='uint8'>
                            <choice name='Read'>0</choice>
                            <choice name='Write'>1</choice>
                            <choice name='Legacy' deprecated='2'>2</choice>
                        </set>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            var resultList = results.ToList();
            var setResult = resultList.FirstOrDefault(r => r.name.Contains("Capabilities"));
            Assert.NotEqual(default, setResult);
            Assert.Contains("[Obsolete(", setResult.content);
            Assert.Contains("Legacy", setResult.content);
        }

        [Fact]
        public void Generate_FixedSizeChar_WithUtf8Encoding_UsesUtf8()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <type name='Symbol' primitiveType='char' length='8' characterEncoding='UTF-8'/>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));
            var resultList = results.ToList();
            var typeResult = resultList.FirstOrDefault(r => r.name.Contains("Symbol"));
            Assert.NotEqual(default, typeResult);
            Assert.Contains("Encoding.UTF8", typeResult.content);
            Assert.DoesNotContain("Encoding.Latin1", typeResult.content);
        }

        [Fact]
        public void Generate_FixedSizeChar_WithAsciiEncoding_UsesLatin1()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <type name='Exchange' primitiveType='char' length='4' characterEncoding='US-ASCII'/>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));
            var resultList = results.ToList();
            var typeResult = resultList.FirstOrDefault(r => r.name.Contains("Exchange"));
            Assert.NotEqual(default, typeResult);
            Assert.Contains("Encoding.Latin1", typeResult.content);
        }

        [Fact]
        public void Generate_FixedSizeChar_NoEncoding_DefaultsToLatin1()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <type name='Ticker' primitiveType='char' length='6'/>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));
            var resultList = results.ToList();
            var typeResult = resultList.FirstOrDefault(r => r.name.Contains("Ticker"));
            Assert.NotEqual(default, typeResult);
            Assert.Contains("Encoding.Latin1", typeResult.content);
        }

        // ===== Phase 1 Feature Tests =====

        [Fact]
        public void Generate_TypeDefinition_IncludesReadonlyModifier()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <type name='OrderId' primitiveType='uint64' description='Order identifier'/>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var typeResult = resultList.FirstOrDefault(r => r.name.Contains("OrderId"));
            Assert.NotEqual(default, typeResult);
            Assert.Contains("public readonly partial struct OrderId", typeResult.content);
            Assert.Contains("public readonly ulong Value;", typeResult.content);
        }

        [Fact]
        public void Generate_TypeDefinition_IncludesConstructor()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <type name='Price' primitiveType='int64' description='Price value'/>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var typeResult = resultList.FirstOrDefault(r => r.name.Contains("Price"));
            Assert.NotEqual(default, typeResult);
            Assert.Contains("public Price(long value)", typeResult.content);
            Assert.Contains("Value = value;", typeResult.content);
        }

        [Fact]
        public void Generate_TypeDefinition_IncludesImplicitConversion()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <type name='Quantity' primitiveType='uint32' description='Quantity value'/>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var typeResult = resultList.FirstOrDefault(r => r.name.Contains("Quantity"));
            Assert.NotEqual(default, typeResult);
            Assert.Contains("public static implicit operator Quantity(uint value)", typeResult.content);
            Assert.Contains("new Quantity(value)", typeResult.content);
        }

        [Fact]
        public void Generate_TypeDefinition_IncludesExplicitConversion()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <type name='UserId' primitiveType='int32' description='User identifier'/>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var typeResult = resultList.FirstOrDefault(r => r.name.Contains("UserId"));
            Assert.NotEqual(default, typeResult);
            Assert.Contains("public static explicit operator int(UserId value)", typeResult.content);
            Assert.Contains("value.Value", typeResult.content);
        }

        [Fact]
        public void Generate_TypeDefinition_AllPhase1Features_IntegrationTest()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <type name='TradeId' primitiveType='uint64' description='Trade identifier'/>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var typeResult = resultList.FirstOrDefault(r => r.name.Contains("TradeId"));
            Assert.NotEqual(default, typeResult);
            
            // Verify readonly struct
            Assert.Contains("public readonly partial struct TradeId", typeResult.content);
            Assert.Contains("public readonly ulong Value;", typeResult.content);
            
            // Verify constructor
            Assert.Contains("public TradeId(ulong value)", typeResult.content);
            
            // Verify conversions
            Assert.Contains("public static implicit operator TradeId(ulong value)", typeResult.content);
            Assert.Contains("public static explicit operator ulong(TradeId value)", typeResult.content);
        }

        [Fact]
        public void Generate_RefStruct_IncludesReadonlyModifier()
        {
            // Arrange - Phase 3 Option 1: readonly ref structs
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <composite name='VarString8' description='Variable length UTF-8 string'>
                            <type name='length' primitiveType='uint8'/>
                            <type name='varData' length='0' primitiveType='uint8' characterEncoding='UTF-8'/>
                        </composite>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var compositeResult = resultList.FirstOrDefault(r => r.name.Contains("VarString8"));
            Assert.NotEqual(default, compositeResult);
            
            // Verify readonly ref struct declaration
            Assert.Contains("public readonly ref partial struct VarString8", compositeResult.content);
            
            // Verify readonly fields
            Assert.Contains("public readonly byte Length;", compositeResult.content);
            Assert.Contains("public readonly ReadOnlySpan<byte> VarData;", compositeResult.content);
        }

        [Fact]
        public void Generate_RefStruct_IncludesConstructor()
        {
            // Arrange - Phase 3 Option 1: ref struct constructors
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <composite name='VarString8' description='Variable length UTF-8 string'>
                            <type name='length' primitiveType='uint8'/>
                            <type name='varData' length='0' primitiveType='uint8' characterEncoding='UTF-8'/>
                        </composite>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var compositeResult = resultList.FirstOrDefault(r => r.name.Contains("VarString8"));
            Assert.NotEqual(default, compositeResult);
            
            // Verify constructor exists with correct signature
            Assert.Contains("public VarString8(byte length, ReadOnlySpan<byte> varData)", compositeResult.content);
            Assert.Contains("Length = length;", compositeResult.content);
            Assert.Contains("VarData = varData;", compositeResult.content);
        }

        [Fact]
        public void Generate_RefStruct_CreateMethodUsesConstructor()
        {
            // Arrange - Phase 3 Option 1: Create method should use constructor
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <composite name='VarString8' description='Variable length UTF-8 string'>
                            <type name='length' primitiveType='uint8'/>
                            <type name='varData' length='0' primitiveType='uint8' characterEncoding='UTF-8'/>
                        </composite>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var compositeResult = resultList.FirstOrDefault(r => r.name.Contains("VarString8"));
            Assert.NotEqual(default, compositeResult);
            
            // Verify Create method uses constructor instead of object initializer
            Assert.Contains("public static VarString8 Create(ReadOnlySpan<byte> buffer)", compositeResult.content);
            Assert.Contains("new VarString8(length,", compositeResult.content);
            
            // Should NOT use object initializer syntax
            Assert.DoesNotContain("new VarString8 {", compositeResult.content);
        }

        [Fact]
        public void Generate_BlittableComposite_RemainsUnchanged()
        {
            // Arrange - Blittable composites should NOT be readonly ref structs
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <composite name='MessageHeader' description='Message header'>
                            <type name='blockLength' primitiveType='uint16'/>
                            <type name='templateId' primitiveType='uint16'/>
                        </composite>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            var compositeResult = resultList.FirstOrDefault(r => r.name.Contains("MessageHeader"));
            Assert.NotEqual(default, compositeResult);
            
            // Verify it's a regular struct, not a ref struct
            Assert.Contains("public partial struct MessageHeader", compositeResult.content);
            Assert.DoesNotContain("ref struct", compositeResult.content);
            
            // Should have StructLayout attribute for blittable types
            Assert.Contains("[StructLayout(LayoutKind.Sequential, Pack = 1)]", compositeResult.content);
        }

        [Fact]
        public void Generate_WithRefInComposite_EmbedsReferencedType()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <composite name='Booster' description='Turbo booster'>
                            <type name='boostType' primitiveType='char'/>
                            <type name='horsePower' primitiveType='uint16'/>
                        </composite>
                        <composite name='Engine' description='Engine details'>
                            <type name='capacity' primitiveType='uint16'/>
                            <type name='numCylinders' primitiveType='uint8'/>
                            <ref name='booster' type='Booster'/>
                        </composite>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            var engineResult = results.FirstOrDefault(r => r.name.Contains("Engine"));
            Assert.NotEqual(default, engineResult);
            // Should embed Booster as a field
            Assert.Contains("private Booster booster;", engineResult.content);
            Assert.Contains("[System.Diagnostics.CodeAnalysis.UnscopedRef]", engineResult.content);
            Assert.Contains("public ref Booster Booster => ref booster;", engineResult.content);
            // Engine should be blittable (all fields fixed-size)
            Assert.Contains("[StructLayout(LayoutKind.Sequential, Pack = 1)]", engineResult.content);
            // MESSAGE_SIZE should include Booster size (1 char + 2 uint16 = 3) + capacity(2) + numCylinders(1) = 6
            Assert.Contains("MESSAGE_SIZE = 6;", engineResult.content);
        }

        [Fact]
        public void Generate_WithRefInComposite_RegistersCompositeFieldType()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <composite name='Decimal' description='Decimal value'>
                            <type name='mantissa' primitiveType='int64'/>
                            <type name='exponent' primitiveType='int8'/>
                        </composite>
                        <composite name='Quote' description='Quote with ref'>
                            <type name='price' primitiveType='uint64'/>
                            <ref name='decimal' type='Decimal'/>
                        </composite>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            // The ref field should be registered in CompositeFieldTypes
            Assert.True(context.CompositeFieldTypes.ContainsKey("Quote.decimal"));
            Assert.Equal("Decimal", context.CompositeFieldTypes["Quote.decimal"]);
        }

        [Fact]
        public void Generate_WithNestedComposite_GeneratesBothTypes()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <composite name='Outer' description='Outer composite'>
                            <type name='id' primitiveType='uint32'/>
                            <composite name='Inner' description='Inner composite'>
                                <type name='value1' primitiveType='uint16'/>
                                <type name='value2' primitiveType='uint8'/>
                            </composite>
                        </composite>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            // Inner composite should be generated as its own type
            var innerResult = results.FirstOrDefault(r => r.name.Contains("Inner"));
            Assert.NotEqual(default, innerResult);
            Assert.Contains("public partial struct Inner", innerResult.content);
            Assert.Contains("MESSAGE_SIZE = 3;", innerResult.content);

            // Outer composite should embed Inner as a field
            var outerResult = results.FirstOrDefault(r => r.name.Contains("Outer") && !r.name.Contains("Inner"));
            Assert.NotEqual(default, outerResult);
            Assert.Contains("private Inner inner;", outerResult.content);
            Assert.Contains("[System.Diagnostics.CodeAnalysis.UnscopedRef]", outerResult.content);
            Assert.Contains("public ref Inner Inner => ref inner;", outerResult.content);
            // Outer size: uint32(4) + Inner(3) = 7
            Assert.Contains("MESSAGE_SIZE = 7;", outerResult.content);
        }

        [Fact]
        public void Generate_WithNestedEnumInComposite_GeneratesEnum()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <composite name='Tagged' description='Tagged composite'>
                            <type name='value' primitiveType='uint32'/>
                            <enum name='TagType' encodingType='uint8'>
                                <validValue name='A'>0</validValue>
                                <validValue name='B'>1</validValue>
                            </enum>
                        </composite>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            // Nested enum should be generated
            var enumResult = results.FirstOrDefault(r => r.name.Contains("TagType"));
            Assert.NotEqual(default, enumResult);
            Assert.Contains("enum TagType", enumResult.content);
        }
        [Fact]
        public void Generate_WithOptionalFloatType_UsesIsNaN()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <type name='optionalFloat' primitiveType='float' presence='optional'/>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            var typeResult = results.FirstOrDefault(r => r.name.Contains("OptionalFloat"));
            Assert.NotEqual(default, typeResult);
            Assert.Contains("float.IsNaN(value)", typeResult.content);
            Assert.DoesNotContain("value == ", typeResult.content);
        }

        [Fact]
        public void Generate_WithOptionalDoubleType_UsesIsNaN()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <type name='optionalDouble' primitiveType='double' presence='optional'/>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            var typeResult = results.FirstOrDefault(r => r.name.Contains("OptionalDouble"));
            Assert.NotEqual(default, typeResult);
            Assert.Contains("double.IsNaN(value)", typeResult.content);
        }

        [Fact]
        public void Generate_WithOptionalFloatInComposite_UsesIsNaN()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <composite name='PriceComposite' description='Price with optional float'>
                            <type name='mantissa' primitiveType='int64'/>
                            <type name='exponent' primitiveType='float' presence='optional'/>
                        </composite>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            var compositeResult = results.FirstOrDefault(r => r.name.Contains("PriceComposite"));
            Assert.NotEqual(default, compositeResult);
            Assert.Contains("float.IsNaN(exponent)", compositeResult.content);
        }

        [Fact]
        public void Generate_WithMultipleTypesBlocks_MergesAllTypes()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <enum name='Side' encodingType='uint8'>
                            <validValue name='Buy'>1</validValue>
                            <validValue name='Sell'>2</validValue>
                        </enum>
                    </types>
                    <types>
                        <composite name='Decimal' description='Decimal value'>
                            <type name='mantissa' primitiveType='int64'/>
                            <type name='exponent' primitiveType='int8'/>
                        </composite>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            Assert.Contains(results, r => r.name.Contains("Side"));
            Assert.Contains(results, r => r.name.Contains("Decimal"));
        }

        [Fact]
        public void Generate_WithDuplicateEnumName_LastDefinitionWins()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <enum name='Side' encodingType='uint8'>
                            <validValue name='Buy'>1</validValue>
                        </enum>
                        <enum name='Side' encodingType='uint8'>
                            <validValue name='Sell'>2</validValue>
                        </enum>
                    </types>
                </messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();
            // Both should generate (duplicate detection is a warning, not an error)
            var sideResults = results.Where(r => r.name.Contains("Side")).ToList();
            Assert.Equal(2, sideResults.Count);
            // Last definition includes Sell
            Assert.Contains("Sell", sideResults.Last().content);
        }

        [Fact]
        public void Generate_WithBlittableComposite_ProducesToStringOverride()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <types>
                        <composite name='decimal' description='A decimal value'>
                            <type name='mantissa' primitiveType='int64'/>
                            <type name='exponent' primitiveType='int8' presence='constant'>-3</type>
                        </composite>
                    </types>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));
            var decimalResult = results.First(r => r.name.Contains("Decimal"));

            // Assert
            Assert.Contains("public readonly override string ToString()", decimalResult.content);
            Assert.Contains("Decimal {{ Mantissa={Mantissa} }}", decimalResult.content);
        }

        [Fact]
        public void Generate_WithDecimalComposite_ProducesToDecimalMethod()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <types>
                        <composite name='Price' description='Price with 4 decimal places'>
                            <type name='mantissa' primitiveType='int64'/>
                            <type name='exponent' primitiveType='int8' presence='constant'>-4</type>
                        </composite>
                    </types>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();
            var toDecimalResult = results.First(r => r.name.Contains("ToDecimal"));

            // Assert — non-optional mantissa returns decimal (not decimal?)
            Assert.Contains("public partial struct Price", toDecimalResult.content);
            Assert.Contains("public readonly decimal ToDecimal() => Mantissa * 1e-4m;", toDecimalResult.content);
            Assert.DoesNotContain("decimal?", toDecimalResult.content);
        }

        [Fact]
        public void Generate_WithOptionalDecimalComposite_ProducesNullableToDecimalMethod()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <types>
                        <composite name='PriceOptional' description='Optional price'>
                            <type name='mantissa' primitiveType='int64' presence='optional'/>
                            <type name='exponent' primitiveType='int8' presence='constant'>-8</type>
                        </composite>
                    </types>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();
            var toDecimalResult = results.First(r => r.name.Contains("ToDecimal"));

            // Assert — optional mantissa returns decimal?
            Assert.Contains("public partial struct PriceOptional", toDecimalResult.content);
            Assert.Contains("public readonly decimal? ToDecimal() => Mantissa.HasValue ? Mantissa.Value * 1e-8m : null;", toDecimalResult.content);
        }

        [Fact]
        public void Generate_WithNonDecimalComposite_DoesNotProduceToDecimal()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <types>
                        <composite name='messageHeader' description='Message header'>
                            <type name='blockLength' primitiveType='uint16'/>
                            <type name='templateId' primitiveType='uint16'/>
                            <type name='schemaId' primitiveType='uint16'/>
                            <type name='version' primitiveType='uint16'/>
                        </composite>
                    </types>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            // Assert — no ToDecimal file generated for non-decimal composites
            Assert.DoesNotContain(results, r => r.name.Contains("ToDecimal"));
        }

        [Fact]
        public void Generate_WithTimestampNanosComposite_ProducesToDateTimeMethods()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <types>
                        <enum name='TimeUnit' encodingType='uint8'>
                            <validValue name='nanosecond'>0</validValue>
                            <validValue name='second'>5</validValue>
                        </enum>
                        <composite name='UTCTimestampNanos' description='Nanosecond timestamp'>
                            <type name='time' primitiveType='uint64' presence='optional'/>
                            <type name='unit' primitiveType='uint8' presence='constant' valueRef='TimeUnit.nanosecond'>0</type>
                        </composite>
                    </types>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();
            var toDateTimeResult = results.First(r => r.name.Contains("ToDateTime"));

            // Assert — optional time field returns DateTime? and DateTimeOffset?
            Assert.Contains("public partial struct UTCTimestampNanos", toDateTimeResult.content);
            Assert.Contains("public readonly System.DateTime? ToDateTime()", toDateTimeResult.content);
            Assert.Contains("AddTicks((long)(Time.Value / 100))", toDateTimeResult.content);
            Assert.Contains("public readonly System.DateTimeOffset? ToDateTimeOffset()", toDateTimeResult.content);
        }

        [Fact]
        public void Generate_WithTimestampSecondsComposite_ProducesToDateTimeMethods()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <types>
                        <enum name='TimeUnit' encodingType='uint8'>
                            <validValue name='second'>5</validValue>
                        </enum>
                        <composite name='UTCTimestampSeconds' description='Second timestamp'>
                            <type name='time' primitiveType='uint64'/>
                            <type name='unit' primitiveType='uint8' presence='constant' valueRef='TimeUnit.second'>5</type>
                        </composite>
                    </types>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();
            var toDateTimeResult = results.First(r => r.name.Contains("ToDateTime"));

            // Assert — non-optional time returns DateTime (not nullable)
            Assert.Contains("public readonly System.DateTime ToDateTime()", toDateTimeResult.content);
            Assert.Contains("FromUnixTimeSeconds((long)Time)", toDateTimeResult.content);
            Assert.Contains("public readonly System.DateTimeOffset ToDateTimeOffset()", toDateTimeResult.content);
            Assert.DoesNotContain("DateTime?", toDateTimeResult.content);
        }

        [Fact]
        public void Generate_WithTimestampMillisComposite_ProducesToDateTimeMethods()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <types>
                        <enum name='TimeUnit' encodingType='uint8'>
                            <validValue name='millisecond'>3</validValue>
                        </enum>
                        <composite name='UTCTimestampMillis' description='Millisecond timestamp'>
                            <type name='time' primitiveType='uint64'/>
                            <type name='unit' primitiveType='uint8' presence='constant' valueRef='TimeUnit.millisecond'>3</type>
                        </composite>
                    </types>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();
            var toDateTimeResult = results.First(r => r.name.Contains("ToDateTime"));

            // Assert
            Assert.Contains("FromUnixTimeMilliseconds((long)Time)", toDateTimeResult.content);
        }

        [Fact]
        public void Generate_WithNonTimestampComposite_DoesNotProduceToDateTime()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <types>
                        <composite name='Engine' description='Engine data'>
                            <type name='capacity' primitiveType='uint16'/>
                            <type name='numCylinders' primitiveType='uint8'/>
                        </composite>
                    </types>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();

            // Assert
            Assert.DoesNotContain(results, r => r.name.Contains("ToDateTime"));
        }

        [Fact]
        public void Generate_WithLocalMktDateType_ProducesToDateOnlyMethod()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <types>
                        <type name='LocalMktDate32' primitiveType='int32' semanticType='LocalMktDate' description='Days since epoch'/>
                    </types>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();
            var toDateOnlyResult = results.First(r => r.name.Contains("ToDateOnly"));

            // Assert — non-optional returns DateOnly
            Assert.Contains("public partial struct LocalMktDate32", toDateOnlyResult.content);
            Assert.Contains("public readonly System.DateOnly ToDateOnly()", toDateOnlyResult.content);
            Assert.Contains("AddDays(Value)", toDateOnlyResult.content);
            Assert.DoesNotContain("DateOnly?", toDateOnlyResult.content);
        }

        [Fact]
        public void Generate_WithOptionalLocalMktDateType_ProducesNullableToDateOnlyMethod()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <types>
                        <type name='LocalMktDate32Optional' primitiveType='int32' presence='optional' nullValue='0' semanticType='LocalMktDate' description='Optional days since epoch'/>
                    </types>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();
            var toDateOnlyResult = results.First(r => r.name.Contains("ToDateOnly"));

            // Assert — optional returns DateOnly?
            Assert.Contains("public partial struct LocalMktDate32Optional", toDateOnlyResult.content);
            Assert.Contains("public readonly System.DateOnly? ToDateOnly()", toDateOnlyResult.content);
            Assert.Contains("Value.HasValue", toDateOnlyResult.content);
        }

        [Fact]
        public void Generate_WithMonthYearComposite_ProducesToDateOnlyMethod()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <types>
                        <composite name='MaturityMonthYear' semanticType='MonthYear' description='Month-Year with optional day'>
                            <type name='year' primitiveType='uint16' presence='optional'/>
                            <type name='month' primitiveType='uint8' presence='optional'/>
                            <type name='day' primitiveType='uint8' presence='optional'/>
                            <type name='week' primitiveType='uint8' presence='optional'/>
                        </composite>
                    </types>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();
            var toDateOnlyResult = results.First(r => r.name.Contains("ToDateOnly"));

            // Assert
            Assert.Contains("public partial struct MaturityMonthYear", toDateOnlyResult.content);
            Assert.Contains("public readonly System.DateOnly? ToDateOnly()", toDateOnlyResult.content);
            Assert.Contains("Day ?? 1", toDateOnlyResult.content);
        }

        [Fact]
        public void Generate_WithMonthYearNoDayComposite_DefaultsDay()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <types>
                        <composite name='SimpleMonthYear' semanticType='MonthYear' description='Just month and year'>
                            <type name='year' primitiveType='uint16'/>
                            <type name='month' primitiveType='uint8'/>
                        </composite>
                    </types>
                </sbe:messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();
            var toDateOnlyResult = results.First(r => r.name.Contains("ToDateOnly"));

            // Assert — no day field, defaults to 1
            Assert.Contains("public readonly System.DateOnly? ToDateOnly()", toDateOnlyResult.content);
            Assert.DoesNotContain("Day ??", toDateOnlyResult.content);
            // Non-optional year/month are wrapped in nullable for the pattern match
            Assert.Contains("(ushort?)Year is { } y", toDateOnlyResult.content);
            Assert.Contains("(byte?)Month is { } m", toDateOnlyResult.content);
        }
    }
}
