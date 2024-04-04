using System;
using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    internal class PrimitiveTypes
    {
        public static readonly Dictionary<string, string> NullValueByType = new Dictionary<string, string>
        {
            { "sbyte", "-128" },
            { "short", "-32768" },
            { "int", "-2147483648" },
            { "long", "-9223372036854775808L" },

            { "char", "'\\0'" },
            
            { "byte", "255" },
            { "ushort", "65335" },
            { "uint", "4294967295" },
            { "ulong", "18446744073709551615UL" },
        };
    }
}
