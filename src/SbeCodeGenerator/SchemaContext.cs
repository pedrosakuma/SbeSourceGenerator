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

        public Dictionary<string, string> EnumPrimitiveTypes { get; } = new Dictionary<string, string>(16);
        public Dictionary<string, int> CustomTypeLengths { get; } = new Dictionary<string, int>(32);
        public Dictionary<string, byte> CustomConstantTypes { get; } = new Dictionary<string, byte>(8);

        /// <summary>
        /// Maps original schema identifiers to the generated identifiers emitted in C# code.
        /// Helps keep references (valueRef, composites, enums) consistent after normalization.
        /// </summary>
        public Dictionary<string, string> GeneratedTypeNames { get; } = new Dictionary<string, string>(32);

        /// <summary>
        /// Keeps track of runtime namespaces that already emitted SpanReader/SpanWriter helpers.
        /// Prevents duplicate type definitions when multiple schema versions share the same base namespace.
        /// </summary>
        public HashSet<string> GeneratedRuntimeNamespaces { get; }

        /// <summary>
        /// Tracks which types are optional types (have presence="optional").
        /// Maps type name to its underlying primitive type (e.g., "Int64NULL" -> "long").
        /// </summary>
        public Dictionary<string, string> OptionalTypes { get; } = new Dictionary<string, string>(8);

        /// <summary>
        /// Tracks composite types and their field types.
        /// Maps "CompositeName.FieldName" -> native type (e.g., "GroupSizeEncoding.numInGroup" -> "ushort").
        /// </summary>
        public Dictionary<string, string> CompositeFieldTypes { get; } = new Dictionary<string, string>(16);

        /// <summary>
        /// The byte order (endianness) specified in the schema.
        /// Defaults to "littleEndian" if not specified.
        /// </summary>
        public string ByteOrder { get; set; } = "littleEndian";

        /// <summary>
        /// The composite type name used for the message header.
        /// Defaults to "messageHeader" per SBE spec.
        /// </summary>
        public string HeaderType { get; set; } = "messageHeader";

        /// <summary>
        /// Endian conversion strategy for multi-byte fields.
        /// Computed from schema byteOrder and optional SbeAssumeHostEndianness hint.
        /// </summary>
        public EndianConversion EndianConversion { get; set; } = EndianConversion.None;

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
