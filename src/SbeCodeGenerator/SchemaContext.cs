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
        /// Stores the resolved primitive type and value for constant types.
        /// Maps type name → (PrimitiveType, Value).
        /// Used when a message constant field references a constant type (e.g., SeqNum1 → ("uint", "1")).
        /// </summary>
        public Dictionary<string, (string PrimitiveType, string Value)> ConstantTypeInfo { get; } = new Dictionary<string, (string, string)>(8);

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
        /// Maps non-optional, non-constant named types to their underlying C# primitive type.
        /// e.g., "SettlType" -> "ushort". Used when such types are referenced in optional message fields.
        /// Composites and char array types are NOT included (they don't have a single underlying primitive).
        /// </summary>
        public Dictionary<string, string> TypePrimitiveMapping { get; } = new Dictionary<string, string>(16);

        /// <summary>
        /// Tracks which types are optional types (have presence="optional").
        /// Maps type name to (underlying primitive type, null value).
        /// e.g., "Int64NULL" -> ("long", ""), "RptSeq" -> ("uint", "0").
        /// </summary>
        public Dictionary<string, (string PrimitiveType, string NullValue)> OptionalTypes { get; } = new Dictionary<string, (string, string)>(8);

        /// <summary>
        /// Issue #166: maps a named type's <c>semanticType</c> attribute to its key, so that
        /// fields referencing the type via <c>type="..."</c> (rather than carrying their own
        /// <c>semanticType</c> attribute) inherit the type's semantic registration.
        /// </summary>
        public Dictionary<string, string> TypeSemanticTypes { get; } = new Dictionary<string, string>(16);

        /// <summary>
        /// Issue #166: types whose generated C# representation already provides a typed
        /// conversion (e.g. <c>LocalMktDate</c> → <c>DateOnly</c> via DateHelper). The
        /// semantic-type registry skips inherited registrations for these so it does not
        /// double-emit a converter call against an already-typed field.
        /// </summary>
        public HashSet<string> TypesWithCustomHelper { get; } = new HashSet<string>(System.StringComparer.Ordinal);

        /// <summary>
        /// Maps user-declared simple type names (from &lt;type&gt; elements) to their
        /// underlying SBE primitive type name (e.g., "uint8EnumEncoding" -&gt; "uint8").
        /// Used to resolve enum/set <c>encodingType</c> attributes that reference a
        /// schema-declared alias rather than a primitive directly.
        /// Per SBE 1.0 spec, <c>encodingType</c> is a <c>symbolicName_t</c> and may
        /// point to either a primitive name or any user-declared &lt;type&gt;.
        /// </summary>
        public Dictionary<string, string> EncodingTypeAliases { get; } = new Dictionary<string, string>(16);

        /// <summary>
        /// Tracks composite types and their field types.
        /// Maps "CompositeName.FieldName" -> native type (e.g., "GroupSizeEncoding.numInGroup" -> "ushort").
        /// </summary>
        public Dictionary<string, string> CompositeFieldTypes { get; } = new Dictionary<string, string>(16);

        /// <summary>
        /// Tracks struct types (composites, InlineArray char types) by their original schema name.
        /// Used to detect fields that need ref-returning properties instead of value properties.
        /// </summary>
        public HashSet<string> StructTypeNames { get; } = new HashSet<string>();

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

        /// <summary>
        /// Issue #166: registry of <c>semanticType</c> → converter bindings used to emit
        /// typed <c>{Field}Value</c> accessors alongside raw wire fields. Built-in registrations
        /// are seeded by <see cref="SemanticTypes.SemanticConverterRegistry.Build"/>; user
        /// registrations from <c>[assembly: SbeSemanticType(...)]</c> override built-ins.
        /// </summary>
        public SemanticTypes.SemanticConverterRegistry SemanticConverters { get; set; } =
            SemanticTypes.SemanticConverterRegistry.Empty;

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
