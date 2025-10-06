using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using System.Xml;
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
            var results = generator.Generate("TestNamespace", xmlDoc, context);

            // Assert
            var resultList = results.ToList();
            Assert.Single(resultList);
            Assert.Contains("NumberExtensions", resultList[0].name);
            Assert.Contains("NumberExtensions", resultList[0].content);
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
            var results = generator.Generate("MyCustomNamespace", xmlDoc, context);

            // Assert
            var resultList = results.ToList();
            Assert.Single(resultList);
            Assert.Contains("MyCustomNamespace", resultList[0].content);
        }
    }
}
