using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using SbeSourceGenerator.Schema;
using Microsoft.CodeAnalysis;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    public class UtilitiesCodeGeneratorTests
    {
        [Fact]
        public void Generate_ProducesEndianHelpers()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse("<messageSchema></messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.Equal(3, resultList.Count); // EndianHelpers, SpanReader, SpanWriter
            Assert.Contains(resultList, r => r.name.Contains("EndianHelpers"));
            Assert.Contains(resultList, r => r.content.Contains("EndianHelpers"));
        }

        [Fact]
        public void Generate_UsesProvidedNamespace()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse("<messageSchema></messageSchema>");

            // Act
            var results = generator.Generate("MyCustomNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.Equal(3, resultList.Count); // EndianHelpers, SpanReader, SpanWriter
            Assert.All(resultList, r => Assert.Contains("MyCustomNamespace", r.content));
        }

        [Fact]
        public void Generate_ProducesSpanReader()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse("<messageSchema></messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.Equal(3, resultList.Count); // EndianHelpers, SpanReader, SpanWriter
            Assert.Contains(resultList, r => r.name.Contains("SpanReader"));
            Assert.Contains(resultList, r => r.content.Contains("public ref struct SpanReader"));
        }

        [Fact]
        public void Generate_ProducesSpanWriter()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse("<messageSchema></messageSchema>");

            // Act
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            Assert.Equal(3, resultList.Count); // EndianHelpers, SpanReader, SpanWriter
            Assert.Contains(resultList, r => r.name.Contains("SpanWriter"));
            Assert.Contains(resultList, r => r.content.Contains("public ref struct SpanWriter"));
        }
    }
}
