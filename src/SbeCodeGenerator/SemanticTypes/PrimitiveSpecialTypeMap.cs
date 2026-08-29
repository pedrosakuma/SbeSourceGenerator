using Microsoft.CodeAnalysis;

namespace SbeSourceGenerator.SemanticTypes
{
    /// <summary>
    /// Maps SBE primitive type names (and their C# translated counterparts) to Roslyn
    /// <see cref="SpecialType"/> values, used to validate that a semantic converter's
    /// declared <c>TWire</c> matches the field's actual primitive on the wire.
    /// </summary>
    internal static class PrimitiveSpecialTypeMap
    {
        public static SpecialType FromSbePrimitive(string primitive)
        {
            if (string.IsNullOrEmpty(primitive)) return SpecialType.None;
            switch (primitive)
            {
                case "int8":   return SpecialType.System_SByte;
                case "uint8":  return SpecialType.System_Byte;
                case "char":   return SpecialType.System_Byte;
                case "int16":  return SpecialType.System_Int16;
                case "uint16": return SpecialType.System_UInt16;
                case "int32":  return SpecialType.System_Int32;
                case "uint32": return SpecialType.System_UInt32;
                case "int64":  return SpecialType.System_Int64;
                case "uint64": return SpecialType.System_UInt64;
                case "float":  return SpecialType.System_Single;
                case "double": return SpecialType.System_Double;
                default:       return SpecialType.None;
            }
        }

        public static SpecialType FromCSharpPrimitive(string csharpType)
        {
            if (string.IsNullOrEmpty(csharpType)) return SpecialType.None;
            switch (csharpType)
            {
                case "sbyte":  return SpecialType.System_SByte;
                case "byte":   return SpecialType.System_Byte;
                case "short":  return SpecialType.System_Int16;
                case "ushort": return SpecialType.System_UInt16;
                case "int":    return SpecialType.System_Int32;
                case "uint":   return SpecialType.System_UInt32;
                case "long":   return SpecialType.System_Int64;
                case "ulong":  return SpecialType.System_UInt64;
                case "float":  return SpecialType.System_Single;
                case "double": return SpecialType.System_Double;
                default:       return SpecialType.None;
            }
        }

        public static string ToCSharpKeyword(SpecialType st) => st switch
        {
            SpecialType.System_SByte => "sbyte",
            SpecialType.System_Byte => "byte",
            SpecialType.System_Int16 => "short",
            SpecialType.System_UInt16 => "ushort",
            SpecialType.System_Int32 => "int",
            SpecialType.System_UInt32 => "uint",
            SpecialType.System_Int64 => "long",
            SpecialType.System_UInt64 => "ulong",
            SpecialType.System_Single => "float",
            SpecialType.System_Double => "double",
            _ => "<unknown>",
        };
    }
}
