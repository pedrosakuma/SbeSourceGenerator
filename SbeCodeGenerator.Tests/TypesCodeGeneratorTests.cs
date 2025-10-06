using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using System.Xml;
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
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <messageSchema>
                    <types>
                        <enum name='TestEnum' encodingType='uint8'>
                            <validValue name='Value1'>0</validValue>
                            <validValue name='Value2'>1</validValue>
                        </enum>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.NotEmpty(resultList);
            var enumResult = resultList.FirstOrDefault(r => r.name.Contains("TestEnum"));
            Assert.NotNull(enumResult);
            Assert.Contains("TestEnum", enumResult.content);
            Assert.Contains("Value1", enumResult.content);
            Assert.Contains("Value2", enumResult.content);
        }

        [Fact]
        public void Generate_WithSimpleType_ProducesTypeCode()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <messageSchema>
                    <types>
                        <type name='CustomType' primitiveType='uint32' description='A custom type'/>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.NotEmpty(resultList);
            var typeResult = resultList.FirstOrDefault(r => r.name.Contains("CustomType"));
            Assert.NotNull(typeResult);
            Assert.Contains("CustomType", typeResult.content);
        }

        [Fact]
        public void Generate_WithComposite_ProducesCompositeCode()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <messageSchema>
                    <types>
                        <composite name='TestComposite' description='Test composite'>
                            <type name='field1' primitiveType='uint16'/>
                            <type name='field2' primitiveType='uint32'/>
                        </composite>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.NotEmpty(resultList);
            var compositeResult = resultList.FirstOrDefault(r => r.name.Contains("TestComposite"));
            Assert.NotNull(compositeResult);
            Assert.Contains("TestComposite", compositeResult.content);
        }

        [Fact]
        public void Generate_WithSet_ProducesSetCode()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <messageSchema>
                    <types>
                        <set name='TestSet' encodingType='uint8'>
                            <choice name='Flag1'>0</choice>
                            <choice name='Flag2'>1</choice>
                        </set>
                    </types>
                </messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", xmlDoc, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.NotEmpty(resultList);
            var setResult = resultList.FirstOrDefault(r => r.name.Contains("TestSet"));
            Assert.NotNull(setResult);
            Assert.Contains("TestSet", setResult.content);
            Assert.Contains("Flag1", setResult.content);
            Assert.Contains("Flag2", setResult.content);
        }
    }
}
