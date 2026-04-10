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
        public void Generate_ProducesSpanReaderAndWriter()
        {
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse("<messageSchema></messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            var resultList = results.ToList();
            Assert.Equal(2, resultList.Count); // SpanReader, SpanWriter
            Assert.Contains(resultList, r => r.name.Contains("SpanReader"));
            Assert.Contains(resultList, r => r.name.Contains("SpanWriter"));
        }

        [Fact]
        public void Generate_UsesProvidedNamespace()
        {
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse("<messageSchema></messageSchema>");

            var results = generator.Generate("MyCustomNamespace", schema, context, default(SourceProductionContext));

            var resultList = results.ToList();
            Assert.Equal(2, resultList.Count);
            Assert.All(resultList, r => Assert.Contains("MyCustomNamespace", r.content));
        }

        [Fact]
        public void Generate_ProducesSpanReader()
        {
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse("<messageSchema></messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            var resultList = results.ToList();
            Assert.Contains(resultList, r => r.name.Contains("SpanReader"));
            Assert.Contains(resultList, r => r.content.Contains("public ref struct SpanReader"));
        }

        [Fact]
        public void Generate_ProducesSpanWriter()
        {
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse("<messageSchema></messageSchema>");

            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            var resultList = results.ToList();
            Assert.Contains(resultList, r => r.name.Contains("SpanWriter"));
            Assert.Contains(resultList, r => r.content.Contains("public ref struct SpanWriter"));
        }
    }
}
