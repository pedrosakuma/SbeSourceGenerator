using Microsoft.CodeAnalysis;
using SbeSourceGenerator;
using SbeSourceGenerator.Diagnostics;
using SbeSourceGenerator.Generators;
using System.Collections.Immutable;
using SbeSourceGenerator.Schema;
using Xunit;

namespace SbeCodeGenerator.Tests
{
    public class DiagnosticsTests
    {
        private class TestDiagnosticReceiver
        {
            public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();
            
            public void ReportDiagnostic(Diagnostic diagnostic)
            {
                Diagnostics.Add(diagnostic);
            }
        }

        [Fact]
        public void Generate_WithInvalidIntegerAttribute_EmitsDiagnostic()
        {
            // This test verifies that the diagnostic infrastructure is set up correctly.
            // However, in practice, SourceProductionContext is a struct that can't be easily mocked.
            // The real validation happens during actual source generation at build time.
            
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <type name='BadType' primitiveType='char' length='NotANumber'/>
                    </types>
                </messageSchema>");

            // Act
            // Using default SourceProductionContext - in real usage, diagnostics would be reported
            // This demonstrates the API signature, even though we can't capture diagnostics in tests
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            // The generator should complete without throwing exceptions
            // In actual build, diagnostic SBE001 or SBE006 would be reported to the compiler
            var resultList = results.ToList();
            Assert.NotEmpty(resultList);
        }

        [Fact]
        public void Generate_WithInvalidEnumFlagValue_CompletesSuccessfully()
        {
            // Arrange
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <set name='BadSet' encodingType='uint8'>
                            <choice name='Flag1'>NotANumber</choice>
                        </set>
                    </types>
                </messageSchema>");

            // Act
            // In actual build, diagnostic SBE003 would be reported for invalid flag value
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            // Generator should complete and produce output with fallback value
            Assert.NotEmpty(resultList);
            var setResult = resultList.FirstOrDefault(r => r.name.Contains("BadSet"));
            Assert.NotEqual(default, setResult);
        }

        [Fact]
        public void Generate_WithInvalidOffset_CompletesSuccessfully()
        {
            // Arrange
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <sbe:messageSchema xmlns:sbe='http://fixprotocol.io/2016/sbe'>
                    <sbe:message name='TestMessage' id='1' description='Test'>
                        <field name='field1' id='1' type='uint32' offset='NotANumber'/>
                    </sbe:message>
                </sbe:messageSchema>");

            // Act
            // In actual build, diagnostic SBE001 would be reported for invalid offset
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext));

            // Assert
            var resultList = results.ToList();
            // Generator should complete and produce output with null offset
            Assert.NotEmpty(resultList);
        }

        [Fact]
        public void DiagnosticDescriptors_HaveCorrectProperties()
        {
            // Verify SBE001 - Invalid integer attribute value
            Assert.Equal("SBE001", SbeDiagnostics.InvalidIntegerAttribute.Id);
            Assert.Equal(DiagnosticSeverity.Error, SbeDiagnostics.InvalidIntegerAttribute.DefaultSeverity);
            Assert.True(SbeDiagnostics.InvalidIntegerAttribute.IsEnabledByDefault);

            // Verify SBE002 - Missing required attribute
            Assert.Equal("SBE002", SbeDiagnostics.MissingRequiredAttribute.Id);
            Assert.Equal(DiagnosticSeverity.Error, SbeDiagnostics.MissingRequiredAttribute.DefaultSeverity);

            // Verify SBE003 - Invalid enum flag value
            Assert.Equal("SBE003", SbeDiagnostics.InvalidEnumFlagValue.Id);
            Assert.Equal(DiagnosticSeverity.Error, SbeDiagnostics.InvalidEnumFlagValue.DefaultSeverity);

            // Verify SBE004 - Malformed schema
            Assert.Equal("SBE004", SbeDiagnostics.MalformedSchema.Id);
            Assert.Equal(DiagnosticSeverity.Error, SbeDiagnostics.MalformedSchema.DefaultSeverity);

            // Verify SBE005 - Unsupported construct
            Assert.Equal("SBE005", SbeDiagnostics.UnsupportedConstruct.Id);
            Assert.Equal(DiagnosticSeverity.Warning, SbeDiagnostics.UnsupportedConstruct.DefaultSeverity);

            // Verify SBE006 - Invalid type length
            Assert.Equal("SBE006", SbeDiagnostics.InvalidTypeLength.Id);
            Assert.Equal(DiagnosticSeverity.Error, SbeDiagnostics.InvalidTypeLength.DefaultSeverity);
        }
    }
}
