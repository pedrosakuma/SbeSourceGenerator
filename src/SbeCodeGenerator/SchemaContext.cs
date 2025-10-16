using System.Collections.Generic;

namespace SbeSourceGenerator
{
    /// <summary>
    /// Per-schema context object that maintains primitive lengths, enum mappings, and constant-type tracking.
    /// Eliminates cross-file global mutations that complicate concurrency and testing.
    /// </summary>
    public class SchemaContext
    {
        public Dictionary<string, string> EnumPrimitiveTypes { get; } = new Dictionary<string, string>();
        public Dictionary<string, int> CustomTypeLengths { get; } = new Dictionary<string, int>();
        public Dictionary<string, byte> CustomConstantTypes { get; } = new Dictionary<string, byte>();
        
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
        
        /// <summary>
        /// The schema ID from the messageSchema element.
        /// Used to identify different schemas in multi-schema environments.
        /// </summary>
        public string SchemaId { get; set; } = "1";
        
        /// <summary>
        /// The schema version from the messageSchema element.
        /// Indicates the current version of the schema.
        /// </summary>
        public string SchemaVersion { get; set; } = "0";
        
        /// <summary>
        /// The semantic version from the messageSchema element.
        /// Provides human-readable version information.
        /// </summary>
        public string SemanticVersion { get; set; } = "1.0";
        
        /// <summary>
        /// The package name from the messageSchema element.
        /// Used as the namespace for generated types.
        /// </summary>
        public string Package { get; set; } = "";
        
        /// <summary>
        /// The description from the messageSchema element.
        /// Provides documentation for the schema.
        /// </summary>
        public string Description { get; set; } = "";
    }
}
