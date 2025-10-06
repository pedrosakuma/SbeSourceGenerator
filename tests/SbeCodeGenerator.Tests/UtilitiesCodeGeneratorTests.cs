using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using System.Xml;
using Microsoft.CodeAnalysis;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    public class UtilitiesCodeGeneratorTests
    {
        [Fact]
        public void Generate_ProducesNumberExtensions()
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
            Assert.Equal(2, resultList.Count);
            Assert.Contains(resultList, r => r.name.Contains("NumberExtensions"));
            Assert.Contains(resultList, r => r.content.Contains("NumberExtensions"));
        }

        [Fact]
        public void Generate_ProducesEndianHelpers()
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
            Assert.Equal(2, resultList.Count);
            Assert.Contains(resultList, r => r.name.Contains("EndianHelpers"));
            Assert.Contains(resultList, r => r.content.Contains("EndianHelpers"));
        }

        [Fact]
        public void Generate_UsesProvidedNamespace()
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
            Assert.Equal(2, resultList.Count);
            Assert.All(resultList, r => Assert.Contains("MyCustomNamespace", r.content));
        }
    }
}
