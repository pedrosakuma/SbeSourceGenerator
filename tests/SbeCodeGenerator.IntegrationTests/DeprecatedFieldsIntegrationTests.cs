using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Deprecated.Test;

namespace SbeCodeGenerator.IntegrationTests
{
    /// <summary>
    /// Integration tests for deprecated fields marked with [Obsolete] attribute.
    /// </summary>
    public class DeprecatedFieldsIntegrationTests
    {
        [Fact]
        public void DeprecatedFields_AreMarkedWithObsoleteAttribute()
        {
            // Verify that generated code contains [Obsolete] attributes for deprecated fields
            var projectDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
            var generatedFiles = Directory.GetFiles(
                Path.Combine(projectDir, "Generated"),
                "OrderWithDeprecated.cs",
                SearchOption.AllDirectories
            );
            
            Assert.NotEmpty(generatedFiles);
            
            // Check all versions for deprecated field markers
            var allCode = string.Join("\n", generatedFiles.Select(f => File.ReadAllText(f)));
            
            // Check that deprecated fields have [Obsolete] attribute
            Assert.Contains("[Obsolete(", allCode);
            
            // Check for specific deprecated field markers
            Assert.Contains("OldPrice", allCode);
            Assert.Contains("OptionalField", allCode);
            
            // LegacyQuantity only appears in V1 and V2
            var v1v2Files = generatedFiles.Where(f => f.Contains("V1") || f.Contains("V2"));
            if (v1v2Files.Any())
            {
                var v1v2Code = string.Join("\n", v1v2Files.Select(f => File.ReadAllText(f)));
                Assert.Contains("LegacyQuantity", v1v2Code);
            }
        }

        [Fact]
        public void DeprecatedField_HasCorrectObsoleteMessage()
        {
            // Verify the obsolete message format
            var projectDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
            var generatedFiles = Directory.GetFiles(
                Path.Combine(projectDir, "Generated"),
                "OrderWithDeprecated.cs",
                SearchOption.AllDirectories
            );
            
            Assert.NotEmpty(generatedFiles);
            
            var allCode = string.Join("\n", generatedFiles.Select(f => File.ReadAllText(f)));
            
            // Should contain deprecation message
            Assert.Contains("deprecated", allCode.ToLower());
        }

        [Fact]
        public void DeprecatedFields_StillWorkCorrectly()
        {
            // Verify that deprecated fields can still be used (backward compatibility)
            // Even though they trigger warnings, they should still function
            
            // V0 should have the deprecated fields
            var v0Size = OrderWithDeprecatedData.MESSAGE_SIZE;
            Assert.True(v0Size > 0);
            
            Span<byte> buffer = stackalloc byte[v0Size];
            ref OrderWithDeprecatedData message = ref MemoryMarshal.AsRef<OrderWithDeprecatedData>(buffer);
            
            // Set the active field
            message.OrderId = 123;
            Assert.Equal(123, message.OrderId.Value);
            
            // Set the deprecated field - should still work
#pragma warning disable CS0618 // Type or member is obsolete
            message.OldPrice = 100;
            Assert.Equal(100, message.OldPrice.Value);
#pragma warning restore CS0618 // Type or member is obsolete
            
            // Set the new field
            message.Price = 200;
            Assert.Equal(200, message.Price.Value);
        }

        [Fact]
        public void NonDeprecatedFields_DoNotHaveObsoleteAttribute()
        {
            // Verify that non-deprecated fields don't have [Obsolete] attribute
            var projectDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
            var generatedFiles = Directory.GetFiles(
                Path.Combine(projectDir, "Generated"),
                "OrderWithDeprecated.cs",
                SearchOption.AllDirectories
            );
            
            Assert.NotEmpty(generatedFiles);
            
            foreach (var generatedFile in generatedFiles)
            {
                var generatedCode = File.ReadAllText(generatedFile);
                
                // Find the non-deprecated fields and verify they don't have [Obsolete] before them
                var lines = generatedCode.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("public") && 
                        (lines[i].Contains("OrderId") || 
                         (lines[i].Contains("Price;") && !lines[i].Contains("OldPrice")) ||
                         (lines[i].Contains("Quantity;") && !lines[i].Contains("Legacy"))))
                    {
                        // Look back up to 3 lines for [Obsolete]
                        var hasObsolete = false;
                        for (int j = Math.Max(0, i - 3); j < i; j++)
                        {
                            if (lines[j].Contains("[Obsolete"))
                            {
                                hasObsolete = true;
                                break;
                            }
                        }
                        
                        if (lines[i].Contains("OrderId") || 
                            (lines[i].Contains("Price;") && !lines[i].Contains("OldPrice")) ||
                            (lines[i].Contains("Quantity;") && !lines[i].Contains("Legacy")))
                        {
                            Assert.False(hasObsolete, $"Non-deprecated field should not have [Obsolete]: {lines[i]}");
                        }
                    }
                }
            }
        }

        [Fact]
        public void DeprecatedFieldWithSinceVersion_HasCorrectMessage()
        {
            // Verify that deprecated fields with sinceVersion have proper message
            var projectDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
            var generatedFiles = Directory.GetFiles(
                Path.Combine(projectDir, "Generated"),
                "OrderWithDeprecated.cs",
                SearchOption.AllDirectories
            );
            
            Assert.NotEmpty(generatedFiles);
            
            // Check V1 or V2 versions which should have LegacyQuantity
            foreach (var generatedFile in generatedFiles.Where(f => f.Contains("V1") || f.Contains("V2")))
            {
                var generatedCode = File.ReadAllText(generatedFile);
                
                if (generatedCode.Contains("LegacyQuantity"))
                {
                    // Should have version information in deprecation message
                    Assert.Contains("version", generatedCode.ToLower());
                }
            }
        }
    }
}
