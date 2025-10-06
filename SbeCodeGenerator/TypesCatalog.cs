using System.Collections.Generic;

namespace SbeSourceGenerator
{
    /// <summary>
    /// Catalog of static, immutable primitive type mappings.
    /// Per-schema mutable state has been moved to SchemaContext.
    /// </summary>
    internal class TypesCatalog
    {
        public static readonly Dictionary<string, string> NullValueByType = new Dictionary<string, string>
        {
            { "sbyte", "-128" },
            { "short", "-32768" },
            { "int", "-2147483648" },
            { "long", "-9223372036854775808L" },

            { "char", "'\\0'" },

            { "byte", "255" },
            { "ushort", "65535" },
            { "uint", "4294967295" },
            { "ulong", "18446744073709551615UL" },
        };

        public static readonly Dictionary<string, int> PrimitiveTypeLengths = new Dictionary<string, int>
        {
            { "sbyte", sizeof(sbyte) },
            { "short", sizeof(short) },
            { "int", sizeof(int) },
            { "long", sizeof(long) },

            { "char", sizeof(byte) },

            { "byte", sizeof(byte) },
            { "ushort", sizeof(ushort) },
            { "uint", sizeof(uint) },
            { "ulong", sizeof(ulong) },
        };
    }
}
