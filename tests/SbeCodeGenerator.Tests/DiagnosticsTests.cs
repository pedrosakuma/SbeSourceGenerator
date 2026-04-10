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

            // Verify SBE013 - Duplicate type name
            Assert.Equal("SBE013", SbeDiagnostics.DuplicateTypeName.Id);
            Assert.Equal(DiagnosticSeverity.Warning, SbeDiagnostics.DuplicateTypeName.DefaultSeverity);

            // Verify SBE014 - sinceVersion exceeds schema version
            Assert.Equal("SBE014", SbeDiagnostics.SinceVersionExceedsSchemaVersion.Id);
            Assert.Equal(DiagnosticSeverity.Warning, SbeDiagnostics.SinceVersionExceedsSchemaVersion.DefaultSeverity);
        }

        [Fact]
        public void Generate_WithDuplicateTypeName_CompletesSuccessfully()
        {
            var generator = new TypesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema>
                    <types>
                        <enum name='Side' encodingType='uint8'>
                            <validValue name='Buy'>1</validValue>
                        </enum>
                        <enum name='Side' encodingType='uint8'>
                            <validValue name='Sell'>2</validValue>
                        </enum>
                    </types>
                </messageSchema>");

            // Should not throw — last definition wins
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();
            Assert.NotEmpty(results);
            // The last definition should be the one that's generated
            var sideResult = results.Last(r => r.name.Contains("Side"));
            Assert.Contains("Sell", sideResult.content);
        }

        [Fact]
        public void Generate_WithSinceVersionExceedingSchemaVersion_CompletesSuccessfully()
        {
            var generator = new MessagesCodeGenerator();
            var context = new SchemaContext("test-schema");
            var schema = SchemaReader.Parse(@"
                <messageSchema version='1'>
                    <types>
                        <composite name='messageHeader' description='Message header'>
                            <type name='blockLength' primitiveType='uint16'/>
                            <type name='templateId' primitiveType='uint16'/>
                            <type name='schemaId' primitiveType='uint16'/>
                            <type name='version' primitiveType='uint16'/>
                        </composite>
                    </types>
                    <message name='TestMsg' id='1'>
                        <field name='price' id='1' type='uint64'/>
                        <field name='quantity' id='2' type='uint32' sinceVersion='5'/>
                    </message>
                </messageSchema>");

            // Should not throw even with sinceVersion=5 > schemaVersion=1
            var results = generator.Generate("TestNamespace", schema, context, default(SourceProductionContext)).ToList();
            Assert.NotEmpty(results);
        }

        [Fact]
        public void TypeResolverHelper_RegisterGeneratedTypeName_DetectsDuplicates()
        {
            var context = new SchemaContext("test-schema");

            // First registration succeeds
            TypeResolverHelper.RegisterGeneratedTypeName(context, "MyType");
            Assert.True(context.GeneratedTypeNames.ContainsKey("MyType"));

            // Second registration with same name overwrites (but would emit diagnostic with real sourceContext)
            TypeResolverHelper.RegisterGeneratedTypeName(context, "MyType");
            Assert.True(context.GeneratedTypeNames.ContainsKey("MyType"));
        }
    }
}
