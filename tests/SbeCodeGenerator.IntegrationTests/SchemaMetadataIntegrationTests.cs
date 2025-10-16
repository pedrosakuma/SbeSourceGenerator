using System;
using System.IO;
using System.Linq;
using Xunit;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Integration tests for schema metadata generation and multi-schema support.
    /// </summary>
    public class SchemaMetadataIntegrationTests
    {
        [Fact]
        public void SchemaMetadata_IsGeneratedForEachSchema()
        {
            // Verify that SchemaMetadata class is generated for each schema
            var projectDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
            var generatedFiles = Directory.GetFiles(
                Path.Combine(projectDir, "Generated"),
                "SchemaMetadata.cs",
                SearchOption.AllDirectories
            );
            
            // Should have SchemaMetadata for all test schemas
            Assert.True(generatedFiles.Length >= 4, $"Expected at least 4 SchemaMetadata files, found {generatedFiles.Length}");
            Assert.Contains(generatedFiles, f => f.Contains("Versioning.Test") && f.Contains("SchemaMetadata.cs"));
            Assert.Contains(generatedFiles, f => f.Contains("Integration.Test") && f.Contains("SchemaMetadata.cs"));
        }

        [Fact]
        public void VersioningSchema_HasCorrectMetadata()
        {
            // Test the versioning test schema metadata
            var schemaId = Versioning.Test.SchemaMetadata.SCHEMA_ID;
            var schemaVersion = Versioning.Test.SchemaMetadata.SCHEMA_VERSION;
            var semanticVersion = Versioning.Test.SchemaMetadata.SEMANTIC_VERSION;
            var package = Versioning.Test.SchemaMetadata.PACKAGE;
            var description = Versioning.Test.SchemaMetadata.DESCRIPTION;
            var byteOrder = Versioning.Test.SchemaMetadata.BYTE_ORDER;
            
            // Verify values from versioning-test-schema.xml
            Assert.Equal((ushort)3, schemaId);
            Assert.Equal((ushort)2, schemaVersion);
            Assert.Equal("2.0", semanticVersion);
            Assert.Equal("versioning_test", package);
            Assert.Equal("Schema evolution test with sinceVersion", description);
            Assert.Equal("littleEndian", byteOrder);
        }

        [Fact]
        public void IntegrationTestSchema_HasCorrectMetadata()
        {
            // Test the integration test schema metadata
            var schemaId = Integration.Test.SchemaMetadata.SCHEMA_ID;
            var schemaVersion = Integration.Test.SchemaMetadata.SCHEMA_VERSION;
            var semanticVersion = Integration.Test.SchemaMetadata.SEMANTIC_VERSION;
            var byteOrder = Integration.Test.SchemaMetadata.BYTE_ORDER;
            
            // Verify values from integration-test-schema.xml
            Assert.Equal((ushort)2, schemaId);
            Assert.Equal((ushort)0, schemaVersion);
            Assert.Equal("1.0", semanticVersion);
            Assert.Equal("littleEndian", byteOrder);
        }

        [Fact]
        public void IsCompatible_ReturnsTrueForSameSchema()
        {
            // Same schema ID and version
            var compatible = Versioning.Test.SchemaMetadata.IsCompatible(3, 2);
            Assert.True(compatible);
        }

        [Fact]
        public void IsCompatible_ReturnsTrueForOlderVersion()
        {
            // Same schema ID, older version (backward compatibility)
            var compatible = Versioning.Test.SchemaMetadata.IsCompatible(3, 1);
            Assert.True(compatible);
            
            compatible = Versioning.Test.SchemaMetadata.IsCompatible(3, 0);
            Assert.True(compatible);
        }

        [Fact]
        public void IsCompatible_ReturnsFalseForNewerVersion()
        {
            // Same schema ID, newer version (forward compatibility not supported by default)
            var compatible = Versioning.Test.SchemaMetadata.IsCompatible(3, 3);
            Assert.False(compatible);
            
            compatible = Versioning.Test.SchemaMetadata.IsCompatible(3, 10);
            Assert.False(compatible);
        }

        [Fact]
        public void IsCompatible_ReturnsFalseForDifferentSchemaId()
        {
            // Different schema ID
            var compatible = Versioning.Test.SchemaMetadata.IsCompatible(999, 2);
            Assert.False(compatible);
            
            compatible = Versioning.Test.SchemaMetadata.IsCompatible(1, 2);
            Assert.False(compatible);
        }

        [Fact]
        public void GetVersionInfo_ReturnsFormattedString()
        {
            var versionInfo = Versioning.Test.SchemaMetadata.GetVersionInfo();
            
            Assert.Contains("Schema ID: 3", versionInfo);
            Assert.Contains("Version: 2", versionInfo);
            Assert.Contains("2.0", versionInfo);
        }

        [Fact]
        public void MultipleSchemas_CanBeDistinguished()
        {
            // Verify that different schemas have different IDs
            var versioningSchemaId = Versioning.Test.SchemaMetadata.SCHEMA_ID;
            var integrationSchemaId = Integration.Test.SchemaMetadata.SCHEMA_ID;
            
            Assert.NotEqual(versioningSchemaId, integrationSchemaId);
        }

        [Fact]
        public void SchemaMetadata_SupportsMultiSchemaEnvironment()
        {
            // Simulate a multi-schema environment where we need to route messages
            // based on schema ID
            
            ushort incomingSchemaId = 3;
            ushort incomingVersion = 1;
            
            // Check which schema can handle this message
            bool versioningCanHandle = Versioning.Test.SchemaMetadata.IsCompatible(incomingSchemaId, incomingVersion);
            bool integrationCanHandle = Integration.Test.SchemaMetadata.IsCompatible(incomingSchemaId, incomingVersion);
            
            // Only versioning schema should accept this message
            Assert.True(versioningCanHandle);
            Assert.False(integrationCanHandle);
        }

        [Fact]
        public void SchemaMetadata_AllowsVersionNegotiation()
        {
            // Demonstrate version negotiation between client and server
            
            // Client supports versioning schema v2
            ushort clientSchemaId = Versioning.Test.SchemaMetadata.SCHEMA_ID;
            ushort clientVersion = Versioning.Test.SchemaMetadata.SCHEMA_VERSION;
            
            // Server can accept messages from older clients
            Assert.True(Versioning.Test.SchemaMetadata.IsCompatible(clientSchemaId, 0)); // Very old client
            Assert.True(Versioning.Test.SchemaMetadata.IsCompatible(clientSchemaId, 1)); // Old client
            Assert.True(Versioning.Test.SchemaMetadata.IsCompatible(clientSchemaId, 2)); // Current client
            
            // But not from future clients (by default)
            Assert.False(Versioning.Test.SchemaMetadata.IsCompatible(clientSchemaId, 3)); // Future client
        }
    }
}
