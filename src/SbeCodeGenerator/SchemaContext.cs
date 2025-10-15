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
        /// The byte order (endianness) specified in the schema.
        /// Defaults to "littleEndian" if not specified.
        /// </summary>
        public string ByteOrder { get; set; } = "littleEndian";
    }
}
