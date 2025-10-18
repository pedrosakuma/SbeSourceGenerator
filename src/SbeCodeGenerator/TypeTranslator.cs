using System;
using System.Collections.Generic;

namespace SbeSourceGenerator
{
    internal static class TypeTranslator
    {
        private static readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            {"int8", "sbyte"}, {"int8null", "sbyte"},
            {"int16", "short"}, {"int16null", "short"},
            {"int32", "int"}, {"int32null", "int"},
            {"int64", "long"}, {"int64null", "long"},
            {"uint8", "byte"}, {"uint8null", "byte"},
            {"uint16", "ushort"}, {"uint16null", "ushort"},
            {"uint32", "uint"}, {"uint32null", "uint"},
            {"uint64", "ulong"}, {"uint64null", "ulong"},
            {"char", "char"}, {"charnull", "char"},
            {"float", "float"}, {"double", "double"}
        };

        private static readonly Dictionary<string, string> _nullableNullValues = new(StringComparer.OrdinalIgnoreCase)
        {
            {"int8null", TypesCatalog.NullValueByType["sbyte"]},
            {"int16null", TypesCatalog.NullValueByType["short"]},
            {"int32null", TypesCatalog.NullValueByType["int"]},
            {"int64null", TypesCatalog.NullValueByType["long"]},
            {"uint8null", TypesCatalog.NullValueByType["byte"]},
            {"uint16null", TypesCatalog.NullValueByType["ushort"]},
            {"uint32null", TypesCatalog.NullValueByType["uint"]},
            {"uint64null", TypesCatalog.NullValueByType["ulong"]},
            {"charnull", "'\\0'"}
        };

        private static readonly HashSet<string> _primitiveNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "char","int8","int16","int32","int64","uint8","uint16","uint32","uint64","float","double"
        };

        /// <summary>
        /// Translates a schema type plus presence attribute into a rich translation result.
        /// </summary>
        /// <param name="schemaType">Value of type / primitiveType attribute (may be custom type or built-in)</param>
        /// <param name="presence">presence attribute ("", "required", "optional", "constant")</param>
        /// <param name="explicitNullValue">nullValue attribute if provided (may be empty)</param>
        public static TranslatedType Translate(string schemaType, string presence = "", string explicitNullValue = "")
        {
            if (string.IsNullOrEmpty(schemaType))
                return new TranslatedType(string.Empty, string.Empty, string.Empty, false, false, null, string.Empty);

            string lowered = schemaType.ToLowerInvariant();
            bool isNullableEncoding = lowered.EndsWith("null", StringComparison.Ordinal);
            if (!_aliases.TryGetValue(lowered, out var primitive))
                primitive = schemaType; // preserve custom type name

            bool isOptionalPresence = string.Equals(presence, "optional", StringComparison.Ordinal);

            string? nullValue = null;
            if (!string.IsNullOrEmpty(explicitNullValue))
                nullValue = explicitNullValue;
            else if (isNullableEncoding && _nullableNullValues.TryGetValue(lowered, out var v))
                nullValue = v;
            else if (isOptionalPresence && TypesCatalog.NullValueByType.TryGetValue(primitive, out var nv))
                nullValue = nv;

            return new TranslatedType(schemaType, primitive, primitive, isNullableEncoding, isOptionalPresence, nullValue, schemaType);
        }

        public static bool TryGetNullValue(string schemaType, out string? nullValue)
        {
            var lowered = schemaType.ToLowerInvariant();
            if (_nullableNullValues.TryGetValue(lowered, out var v))
            {
                nullValue = v;
                return true;
            }
            nullValue = null;
            return false;
        }

        public static bool IsPrimitive(string schemaType) => _primitiveNames.Contains(schemaType);

        /// <summary>
        /// Attempts to get a fixed length (in bytes) for a primitive type name (after translation).
        /// Returns true if known length.
        /// </summary>
        public static bool TryGetPrimitiveLength(string schemaTypeOrPrimitive, out int length)
        {
            var lowered = schemaTypeOrPrimitive.ToLowerInvariant();
            if (_aliases.TryGetValue(lowered, out var primitive))
                schemaTypeOrPrimitive = primitive;
            return TypesCatalog.PrimitiveTypeLengths.TryGetValue(schemaTypeOrPrimitive, out length);
        }

        /// <summary>
        /// Normalizes an identifier to PascalCase; keeps interior capitals (id -> Id, messageHeader -> MessageHeader).
        /// </summary>
        public static string NormalizeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            if (char.IsUpper(raw[0])) return raw; // already PascalCase-ish
            return char.ToUpperInvariant(raw[0]) + raw.Substring(1);
        }
    }

    internal sealed record TranslatedType(
        string Original,
        string PrimitiveType,
        string DotNetType,
        bool IsNullableEncoding,
        bool IsOptionalPresence,
        string? NullValue,
        string OriginalName);
}
