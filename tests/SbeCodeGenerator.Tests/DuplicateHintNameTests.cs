using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SbeSourceGenerator;
using System.Collections.Immutable;
using System.Text;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    /// <summary>
    /// End-to-end driver tests for the duplicate-hintName guard (SBE015).
    /// Exercises the full IIncrementalGenerator pipeline (which is where the
    /// guard lives) rather than individual generators.
    /// </summary>
    public class DuplicateHintNameTests
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

        private static (ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult Result) RunGenerator(
            params (string path, string content)[] additionalTexts)
        {
            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                syntaxTrees: System.Array.Empty<Microsoft.CodeAnalysis.SyntaxTree>(),
                references: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                });

            var generator = new SBESourceGenerator().AsSourceGenerator();
            var driver = CSharpGeneratorDriver.Create(
                generators: new[] { generator },
                additionalTexts: additionalTexts.Select(t => (AdditionalText)new InMemoryAdditionalText(t.path, t.content)).ToImmutableArray());

            var runResult = driver.RunGenerators(compilation).GetRunResult();
            return (runResult.Diagnostics, runResult);
        }

        [Fact]
        public void Pipeline_NormalSchema_DoesNotReportDuplicateHintName()
        {
            var schema = @"<?xml version='1.0'?>
<sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                   package='SmokeTest' id='1' version='0'>
  <types>
    <type name='groupSizeEncoding' primitiveType='uint8'/>
    <composite name='messageHeader'>
      <type name='blockLength' primitiveType='uint16'/>
      <type name='templateId' primitiveType='uint16'/>
      <type name='schemaId' primitiveType='uint16'/>
      <type name='version' primitiveType='uint16'/>
    </composite>
  </types>
</sbe:messageSchema>";

            var (diagnostics, _) = RunGenerator(("smoke.xml", schema));
            Assert.DoesNotContain(diagnostics, d => d.Id == "SBE015");
        }

        [Fact]
        public void Pipeline_DuplicateEnumNamesInSchema_TriggersDuplicateGuard()
        {
            // Schema with two enums of the same name. Both pass through the
            // generator (RegisterGeneratedTypeName tolerates the second with
            // SBE013), producing two items with identical hintName. Without
            // SBE015, AddSource would throw and abort the entire 'types' phase.
            var schema = @"<?xml version='1.0'?>
<sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'
                   package='Dup' id='1' version='0'>
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
      <validValue name='SELL'>2</validValue>
    </enum>
    <enum name='Side' encodingType='uint8'>
      <validValue name='BUY'>1</validValue>
      <validValue name='SELL'>2</validValue>
    </enum>
  </types>
</sbe:messageSchema>";

            var (diagnostics, result) = RunGenerator(("dup.xml", schema));

            Assert.All(result.Results, r => Assert.Null(r.Exception));
            Assert.Contains(diagnostics, d => d.Id == "SBE015");
        }
    }
}
