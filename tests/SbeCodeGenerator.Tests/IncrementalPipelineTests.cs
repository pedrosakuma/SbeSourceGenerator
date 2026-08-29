using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SbeSourceGenerator;
using SbeSourceGenerator.SemanticTypes;
using System.Collections.Immutable;
using System.Text;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    public class IncrementalPipelineTests
    {
        private sealed class InMemoryAdditionalText : AdditionalText
        {
            private readonly SourceText _text;

            public InMemoryAdditionalText(string path, string content)
            {
                Path = path;
                _text = SourceText.From(content, Encoding.UTF8);
            }

            public override string Path { get; }

            public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default) => _text;
        }

        [Fact]
        public void Initialize_WhenOneOfTwoSchemasChanges_CachesTheUnchangedSchema()
        {
            var firstSchema = new InMemoryAdditionalText("first-schema.xml", CreateSchema("First.Schema", "Order", withExtraEnumValue: false));
            var secondSchema = new InMemoryAdditionalText("second-schema.xml", CreateSchema("Second.Schema", "Trade", withExtraEnumValue: false));
            var updatedFirstSchema = new InMemoryAdditionalText("first-schema.xml", CreateSchema("First.Schema", "Order", withExtraEnumValue: true));

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                syntaxTrees: [],
                references:
                [
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                ]);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: [new SBESourceGenerator().AsSourceGenerator()],
                additionalTexts: [firstSchema, secondSchema],
                driverOptions: new GeneratorDriverOptions(
                    IncrementalGeneratorOutputKind.None,
                    trackIncrementalGeneratorSteps: true));

            driver = driver.RunGenerators(compilation);
            driver = driver.ReplaceAdditionalText(firstSchema, updatedFirstSchema);
            driver = driver.RunGenerators(compilation);

            GeneratorRunResult generatorResult = Assert.Single(driver.GetRunResult().Results);
            Assert.Null(generatorResult.Exception);

            Assert.True(
                generatorResult.TrackedSteps.TryGetValue("PerSchemaGeneration", out var steps),
                "Expected the per-schema generation step to be tracked.");

            var reasonsByPath = steps
                .SelectMany(static step => step.Outputs)
                .ToDictionary(
                    static output => ExtractPath(output.Value),
                    static output => output.Reason,
                    System.StringComparer.Ordinal);

            Assert.True(
                reasonsByPath.TryGetValue("first-schema.xml", out var changedReason),
                "Expected the changed schema to be present in the tracked outputs.");
            Assert.True(
                changedReason is IncrementalStepRunReason.Modified or IncrementalStepRunReason.New,
                $"Expected first-schema.xml to be Modified or New, but was {changedReason}.");

            Assert.True(
                reasonsByPath.TryGetValue("second-schema.xml", out var unchangedReason),
                "Expected the unchanged schema to be present in the tracked outputs.");
            Assert.True(
                unchangedReason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
                $"Expected second-schema.xml to be Cached or Unchanged, but was {unchangedReason}.");
        }

        private static string ExtractPath(object value)
        {
            if (value is ValueTuple<string, AdditionalText, AnalyzerConfigOptionsProvider, SemanticConverterRegistry> tracked)
                return tracked.Item1;

            throw new Xunit.Sdk.XunitException($"Unexpected tracked output value type: {value.GetType().FullName}");
        }

        private static string CreateSchema(string package, string messageName, bool withExtraEnumValue)
        {
            string extraEnumValue = withExtraEnumValue ? Environment.NewLine + "      <validValue name='HOLD'>3</validValue>" : string.Empty;

            return $@"<?xml version='1.0'?>
<sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                   package='{package}' id='1' version='0'>
  <types>
    <type name='groupSizeEncoding' primitiveType='uint8'/>
    <composite name='messageHeader'>
      <type name='blockLength' primitiveType='uint16'/>
      <type name='templateId' primitiveType='uint16'/>
      <type name='schemaId' primitiveType='uint16'/>
      <type name='version' primitiveType='uint16'/>
    </composite>
    <enum name='Side' encodingType='uint8'>
      <validValue name='BUY'>1</validValue>
      <validValue name='SELL'>2</validValue>{extraEnumValue}
    </enum>
  </types>
  <sbe:message name='{messageName}' id='1'>
    <field name='side' id='1' type='Side'/>
  </sbe:message>
</sbe:messageSchema>";
        }
    }
}
