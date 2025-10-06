using System.Collections.Generic;

namespace SbeSourceGenerator
{
    /// <summary>
    /// Per-schema context object that maintains primitive lengths, enum mappings, and constant-type tracking.
    /// Eliminates cross-file global mutations that complicate concurrency and testing.
    /// </summary>
    internal class SchemaContext
    {
        public Dictionary<string, string> EnumPrimitiveTypes { get; } = new Dictionary<string, string>();
        public Dictionary<string, int> CustomTypeLengths { get; } = new Dictionary<string, int>();
        public Dictionary<string, byte> CustomConstantTypes { get; } = new Dictionary<string, byte>();
    }
}
