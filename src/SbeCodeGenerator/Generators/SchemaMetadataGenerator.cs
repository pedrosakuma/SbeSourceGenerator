using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Generates schema metadata constants for version tracking and multi-schema support.
    /// </summary>
    public class SchemaMetadataGenerator : ICodeGenerator
    {
        public IEnumerable<(string name, string content)> Generate(string ns, XmlDocument xmlDocument, SchemaContext context, SourceProductionContext sourceContext)
        {
            StringBuilder sb = new StringBuilder();
            AppendSchemaMetadata(sb, ns, context);
            yield return ($"{ns}\\SchemaMetadata", sb.ToString());
        }

        private static void AppendSchemaMetadata(StringBuilder sb, string ns, SchemaContext context)
        {
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Schema metadata for {ns}");
            if (!string.IsNullOrEmpty(context.Description))
            {
                sb.AppendLine($"    /// {context.Description}");
            }
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class SchemaMetadata");
            sb.AppendLine("    {");
            
            // Schema ID
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// The unique identifier for this schema.");
            sb.AppendLine("        /// Used to distinguish between different schemas in multi-schema environments.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public const ushort SCHEMA_ID = {context.SchemaId};");
            sb.AppendLine();
            
            // Schema Version
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// The current version of this schema.");
            sb.AppendLine("        /// Incremented when fields are added or schema evolves.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public const ushort SCHEMA_VERSION = {context.SchemaVersion};");
            sb.AppendLine();
            
            // Semantic Version
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// The semantic version of this schema.");
            sb.AppendLine("        /// Provides human-readable version information.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public const string SEMANTIC_VERSION = \"{context.SemanticVersion}\";");
            sb.AppendLine();
            
            // Package Name
            if (!string.IsNullOrEmpty(context.Package))
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// The package name from the schema definition.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public const string PACKAGE = \"{context.Package}\";");
                sb.AppendLine();
            }
            
            // Description
            if (!string.IsNullOrEmpty(context.Description))
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// Description of this schema.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public const string DESCRIPTION = \"{EscapeString(context.Description)}\";");
                sb.AppendLine();
            }
            
            // Byte Order
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// The byte order (endianness) used by this schema.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public const string BYTE_ORDER = \"{context.ByteOrder}\";");
            sb.AppendLine();
            
            // Helper method to check schema compatibility
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Checks if this schema can read messages from another schema version.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"schemaId\">The schema ID from the message header</param>");
            sb.AppendLine("        /// <param name=\"version\">The schema version from the message header</param>");
            sb.AppendLine("        /// <returns>True if compatible, false otherwise</returns>");
            sb.AppendLine("        public static bool IsCompatible(ushort schemaId, ushort version)");
            sb.AppendLine("        {");
            sb.AppendLine("            // Same schema ID required");
            sb.AppendLine("            if (schemaId != SCHEMA_ID)");
            sb.AppendLine("                return false;");
            sb.AppendLine();
            sb.AppendLine("            // Can read same or older versions (backward compatibility)");
            sb.AppendLine("            return version <= SCHEMA_VERSION;");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // Helper method to get version info
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets a string representation of the schema version information.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static string GetVersionInfo()");
            sb.AppendLine("        {");
            sb.AppendLine($"            return $\"Schema ID: {{SCHEMA_ID}}, Version: {{SCHEMA_VERSION}} ({{SEMANTIC_VERSION}})\";");
            sb.AppendLine("        }");
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }

        private static string EscapeString(string input)
        {
            return input.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
