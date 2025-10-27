using System;
using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    /// <summary>
    /// Per-schema context object that maintains primitive lengths, enum mappings, and constant-type tracking.
    /// Eliminates cross-file global mutations that complicate concurrency and testing.
    /// </summary>
    public class SchemaContext
    {
        public SchemaContext(string schemaKey, HashSet<string>? sharedRuntimeNamespaces = null)
        {
            SchemaKey = schemaKey ?? throw new ArgumentNullException(nameof(schemaKey));
            GeneratedRuntimeNamespaces = sharedRuntimeNamespaces ?? new HashSet<string>(StringComparer.Ordinal);
        }

        public string SchemaKey { get; }

        public Dictionary<string, string> EnumPrimitiveTypes { get; } = new Dictionary<string, string>();
        public Dictionary<string, int> CustomTypeLengths { get; } = new Dictionary<string, int>();
        public Dictionary<string, byte> CustomConstantTypes { get; } = new Dictionary<string, byte>();

        /// <summary>
        /// Maps original schema identifiers to the generated identifiers emitted in C# code.
        /// Helps keep references (valueRef, composites, enums) consistent after normalization.
        /// </summary>
        public Dictionary<string, string> GeneratedTypeNames { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Keeps track of runtime namespaces that already emitted SpanReader/SpanWriter helpers.
        /// Prevents duplicate type definitions when multiple schema versions share the same base namespace.
        /// </summary>
        public HashSet<string> GeneratedRuntimeNamespaces { get; }

        /// <summary>
        /// Tracks which types are optional types (have presence="optional").
        /// Maps type name to its underlying primitive type (e.g., "Int64NULL" -> "long").
        /// </summary>
        public Dictionary<string, string> OptionalTypes { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Tracks composite types and their field types.
        /// Maps "CompositeName.FieldName" -> native type (e.g., "GroupSizeEncoding.numInGroup" -> "ushort").
        /// </summary>
        public Dictionary<string, string> CompositeFieldTypes { get; } = new Dictionary<string, string>();

        /// <summary>
        /// The byte order (endianness) specified in the schema.
        /// Defaults to "littleEndian" if not specified.
        /// </summary>
        public string ByteOrder { get; set; } = "littleEndian";

        public string CreateHintName(params string[] segments)
        {
            var builder = new StringBuilder(SchemaKey);
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment))
                    continue;

                builder.Append('\\');
                builder.Append(segment.Replace('/', '\\'));
            }

            return builder.ToString();
        }
    }
}
