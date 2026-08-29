using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SbeSourceGenerator;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    public class DiagnosticLocationTests
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

        private static ImmutableArray<Diagnostic> RunGenerator(string path, string content)
        {
            var compilation = CSharpCompilation.Create(
                "DiagnosticLocationTests",
                references: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                });

            var driver = CSharpGeneratorDriver.Create(
                generators: new[] { new SBESourceGenerator().AsSourceGenerator() },
                additionalTexts: new AdditionalText[] { new InMemoryAdditionalText(path, content) }.ToImmutableArray());

            var result = driver.RunGenerators(compilation).GetRunResult();
            return result.Diagnostics
                .AddRange(result.Results.SelectMany(r => r.Diagnostics));
        }

        [Fact]
        public void Generator_MissingRequiredAttribute_UsesSchemaLocation()
        {
            var schema = @"<?xml version='1.0'?>
<messageSchema version='0'>
  <types>
    <composite name='messageHeader'>
      <type name='blockLength' primitiveType='uint16'/>
      <type name='templateId' primitiveType='uint16'/>
      <type name='schemaId' primitiveType='uint16'/>
      <type name='version' primitiveType='uint16'/>
    </composite>
  </types>
  <message id='1'>
    <field name='Price' id='1' type='uint32'/>
  </message>
</messageSchema>";

            var diagnostic = RunGenerator("/workspace/missing-name.xml", schema).First(d => d.Id == "SBE002");

            Assert.NotEqual(Location.None, diagnostic.Location);
            Assert.Equal("/workspace/missing-name.xml", diagnostic.Location.GetLineSpan().Path);
            Assert.Equal(10, diagnostic.Location.GetLineSpan().StartLinePosition.Line);
        }

        [Fact]
        public void Generator_InvalidIntegerAttribute_UsesAttributeLocation()
        {
            var schema = @"<?xml version='1.0'?>
<messageSchema version='0'>
  <types>
    <type name='BadType' primitiveType='char' length='NotANumber'/>
  </types>
</messageSchema>";

            var diagnostic = RunGenerator("/workspace/invalid-length.xml", schema).First(d => d.Id == "SBE001");

            Assert.NotEqual(Location.None, diagnostic.Location);
            Assert.Equal("/workspace/invalid-length.xml", diagnostic.Location.GetLineSpan().Path);
            Assert.Equal(3, diagnostic.Location.GetLineSpan().StartLinePosition.Line);
        }

        [Fact]
        public void Generator_SinceVersionExceedsSchemaVersion_UsesAttributeLocation()
        {
            var schema = @"<?xml version='1.0'?>
<messageSchema version='1'>
  <types>
    <composite name='messageHeader'>
      <type name='blockLength' primitiveType='uint16'/>
      <type name='templateId' primitiveType='uint16'/>
      <type name='schemaId' primitiveType='uint16'/>
      <type name='version' primitiveType='uint16'/>
    </composite>
  </types>
  <message name='Trade' id='1'>
    <field name='Price' id='1' type='uint32' sinceVersion='5'/>
  </message>
</messageSchema>";

            var diagnostic = RunGenerator("/workspace/since-version.xml", schema).First(d => d.Id == "SBE014");

            Assert.NotEqual(Location.None, diagnostic.Location);
            Assert.Equal("/workspace/since-version.xml", diagnostic.Location.GetLineSpan().Path);
            Assert.Equal(11, diagnostic.Location.GetLineSpan().StartLinePosition.Line);
        }
    }
}
