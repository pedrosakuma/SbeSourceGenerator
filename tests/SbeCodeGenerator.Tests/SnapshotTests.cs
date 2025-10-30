using SbeSourceGenerator;
using SbeSourceGenerator.Generators;
using System.Xml;
using Microsoft.CodeAnalysis;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    /// <summary>
    /// Snapshot tests that verify generated code output against approved snapshots.
    /// These tests prevent regressions by ensuring generated output remains consistent.
    /// To update snapshots when changes are intentional, delete the .verified.txt files and re-run tests.
    /// </summary>
    public class SnapshotTests
    {
        private readonly string _testSchemaPath;
        private readonly XmlDocument _testSchema;

        public SnapshotTests()
        {
            _testSchemaPath = Path.Combine(AppContext.BaseDirectory, "TestData", "test-schema-simple.xml");
            _testSchema = new XmlDocument();
            _testSchema.Load(_testSchemaPath);
        }

        [Fact]
        public Task TypesCodeGenerator_GeneratesConsistentEnumCode()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");

            // Act
            var results = generator.Generate("TestNamespace", _testSchema, context, default(SourceProductionContext));

            // Find the Side enum
            var enumResult = results.FirstOrDefault(r => r.name.Contains("Side"));

            // Assert
            Assert.NotEqual(default, enumResult);
            return Verifier.Verify(enumResult.content)
                .UseDirectory("Snapshots")
                .UseFileName("TypesCodeGenerator.Enum.Side");
        }

        [Fact]
        public Task TypesCodeGenerator_GeneratesConsistentSetCode()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");

            // Act
            var results = generator.Generate("TestNamespace", _testSchema, context, default(SourceProductionContext));

            // Find the Flags set
            var setResult = results.FirstOrDefault(r => r.name.Contains("Flags"));

            // Assert
            Assert.NotEqual(default, setResult);
            return Verifier.Verify(setResult.content)
                .UseDirectory("Snapshots")
                .UseFileName("TypesCodeGenerator.Set.Flags");
        }

        [Fact]
        public Task TypesCodeGenerator_GeneratesConsistentCompositeCode()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");

            // Act
            var results = generator.Generate("TestNamespace", _testSchema, context, default(SourceProductionContext));

            // Find the MessageHeader composite
            var compositeResult = results.FirstOrDefault(r => r.name.Contains("MessageHeader"));

            // Assert
            Assert.NotEqual(default, compositeResult);
            return Verifier.Verify(compositeResult.content)
                .UseDirectory("Snapshots")
                .UseFileName("TypesCodeGenerator.Composite.MessageHeader");
        }

        [Fact]
        public Task MessagesCodeGenerator_GeneratesConsistentTradeMessage()
        {
            // Arrange
            var context = new SchemaContext("test-schema");
            
            // First run TypesCodeGenerator to populate the context
            var typesGenerator = new TypesCodeGenerator();
            _ = typesGenerator.Generate("TestNamespace", _testSchema, context, default(SourceProductionContext)).ToList();
            
            // Then run MessagesCodeGenerator
            var generator = new MessagesCodeGenerator();

            // Act
            var results = generator.Generate("TestNamespace.V0", _testSchema, context, default(SourceProductionContext));

            // Find the Trade message
            var messageResult = results.FirstOrDefault(r => r.name.Contains("Trade") && !r.name.Contains("Parser"));

            // Assert
            Assert.NotEqual(default, messageResult);
            return Verifier.Verify(messageResult.content)
                .UseDirectory("Snapshots")
                .UseFileName("MessagesCodeGenerator.Message.Trade");
        }

        [Fact]
        public Task MessagesCodeGenerator_GeneratesConsistentQuoteMessage()
        {
            // Arrange
            var context = new SchemaContext("test-schema");
            
            // First run TypesCodeGenerator to populate the context
            var typesGenerator = new TypesCodeGenerator();
            _ = typesGenerator.Generate("TestNamespace", _testSchema, context, default(SourceProductionContext)).ToList();
            
            // Then run MessagesCodeGenerator
            var generator = new MessagesCodeGenerator();

            // Act
            var results = generator.Generate("TestNamespace.V0", _testSchema, context, default(SourceProductionContext));

            // Find the Quote message
            var messageResult = results.FirstOrDefault(r => r.name.Contains("Quote") && !r.name.Contains("Parser"));

            // Assert
            Assert.NotEqual(default, messageResult);
            return Verifier.Verify(messageResult.content)
                .UseDirectory("Snapshots")
                .UseFileName("MessagesCodeGenerator.Message.Quote");
        }

        [Fact]
        public Task UtilitiesCodeGenerator_GeneratesConsistentNumberExtensions()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext("test-schema");

            // Act
            var results = generator.Generate("TestNamespace", _testSchema, context, default(SourceProductionContext));

            // Assert
            var result = results.First(r => r.name.Contains("NumberExtensions"));
            return Verifier.Verify(result.content)
                .UseDirectory("Snapshots")
                .UseFileName("UtilitiesCodeGenerator.NumberExtensions");
        }

        [Fact]
        public Task UtilitiesCodeGenerator_GeneratesConsistentEndianHelpers()
        {
            // Arrange
            var generator = new UtilitiesCodeGenerator();
            var context = new SchemaContext("test-schema");

            // Act
            var results = generator.Generate("TestNamespace", _testSchema, context, default(SourceProductionContext));

            // Assert
            var result = results.First(r => r.name.Contains("EndianHelpers"));
            return Verifier.Verify(result.content)
                .UseDirectory("Snapshots")
                .UseFileName("UtilitiesCodeGenerator.EndianHelpers");
        }
    }
}
