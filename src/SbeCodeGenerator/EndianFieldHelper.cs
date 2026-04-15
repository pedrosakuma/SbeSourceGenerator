using System.Text;

namespace SbeSourceGenerator
{
    /// <summary>
    /// Generates endian-aware field declarations (private backing field + public property).
    /// Single-byte fields always get passthrough. Multi-byte fields use the configured EndianConversion strategy.
    /// </summary>
    internal static class EndianFieldHelper
    {
        private static readonly System.Collections.Generic.HashSet<string> MultiByteReversibleTypes =
            new System.Collections.Generic.HashSet<string>
            {
                "short", "ushort", "int", "uint", "long", "ulong", "float", "double"
            };

        public static bool NeedsConversion(string primitiveType, EndianConversion conversion)
        {
            if (conversion == EndianConversion.None)
                return false;
            return MultiByteReversibleTypes.Contains(primitiveType);
        }

        /// <summary>
        /// Appends a field declaration with consistent property pattern.
        /// Always emits private backing field + public property with readonly get and set.
        /// </summary>
        public static void AppendField(StringBuilder sb, int tabs, string type, string name,
            EndianConversion conversion)
        {
            string fieldName = name.FirstCharToLower();
            sb.AppendTabs(tabs).Append("private ").Append(type).Append(" ").Append(fieldName).AppendLine(";");

            if (!NeedsConversion(type, conversion))
            {
                sb.AppendTabs(tabs).Append("public ").Append(type).Append(" ").Append(name)
                    .Append(" { readonly get => ").Append(fieldName)
                    .Append("; set => ").Append(fieldName).Append(" = value; }")
                    .AppendLine();
                return;
            }

            AppendPropertyWithConversion(sb, tabs, type, name, fieldName, conversion);
        }

        /// <summary>
        /// Appends a message field declaration with [FieldOffset] and consistent property pattern.
        /// For struct types (composites, InlineArrays), emits ref-returning property to allow sub-field mutation.
        /// For primitive/enum types, emits private backing field + public property with readonly get and set.
        /// </summary>
        public static void AppendMessageField(StringBuilder sb, int tabs, string declaredType, string primitiveType, string name,
            int? offset, EndianConversion conversion, bool isStructType = false)
        {
            sb.AppendTabs(tabs).Append("[FieldOffset(").Append(offset).AppendLine(")]");

            string fieldName = name.FirstCharToLower();
            sb.AppendTabs(tabs).Append("private ").Append(declaredType).Append(" ").Append(fieldName).AppendLine(";");

            if (isStructType)
            {
                sb.AppendTabs(tabs).Append("public ").Append(declaredType).Append(" ").Append(name)
                    .Append(" { readonly get => ").Append(fieldName)
                    .Append("; set => ").Append(fieldName).Append(" = value; }")
                    .AppendLine();
                return;
            }

            if (!NeedsConversion(primitiveType, conversion))
            {
                sb.AppendTabs(tabs).Append("public ").Append(declaredType).Append(" ").Append(name)
                    .Append(" { readonly get => ").Append(fieldName)
                    .Append("; set => ").Append(fieldName).Append(" = value; }")
                    .AppendLine();
                return;
            }

            bool needsCast = declaredType != primitiveType;
            string getExpr = needsCast
                ? CastGetterExpression(declaredType, primitiveType, fieldName, conversion)
                : GetterExpression(primitiveType, fieldName, conversion);
            string setExpr = needsCast
                ? CastSetterExpression(declaredType, primitiveType, "value", conversion)
                : SetterExpression(primitiveType, "value", conversion);

            sb.AppendTabs(tabs).Append("public ").Append(declaredType).Append(" ").Append(name)
                .Append(" { readonly get => ").Append(getExpr)
                .Append("; set => ").Append(fieldName).Append(" = ").Append(setExpr)
                .AppendLine("; }");
        }

        /// <summary>
        /// Appends a getter expression for use in nullable property patterns.
        /// Returns the expression that converts the backing field to host byte order.
        /// </summary>
        public static string GetterExpression(string type, string fieldName, EndianConversion conversion)
        {
            if (!NeedsConversion(type, conversion))
                return fieldName;

            switch (conversion)
            {
                case EndianConversion.AlwaysReverse:
                    return ReverseCall(type, fieldName);
                case EndianConversion.Conditional:
                    return "BitConverter.IsLittleEndian ? " + ReverseCall(type, fieldName) + " : " + fieldName;
                default:
                    return fieldName;
            }
        }

        /// <summary>
        /// Appends a setter expression for use in nullable property patterns.
        /// Returns the expression that converts a host value to wire byte order.
        /// </summary>
        public static string SetterExpression(string type, string valueExpr, EndianConversion conversion)
        {
            if (!NeedsConversion(type, conversion))
                return valueExpr;

            switch (conversion)
            {
                case EndianConversion.AlwaysReverse:
                    return ReverseCall(type, valueExpr);
                case EndianConversion.Conditional:
                    return "BitConverter.IsLittleEndian ? " + ReverseCall(type, valueExpr) + " : " + valueExpr;
                default:
                    return valueExpr;
            }
        }

        private static void AppendPropertyWithConversion(StringBuilder sb, int tabs, string type,
            string name, string fieldName, EndianConversion conversion)
        {
            string getExpr = GetterExpression(type, fieldName, conversion);
            string setExpr = SetterExpression(type, "value", conversion);

            sb.AppendTabs(tabs).Append("public ").Append(type).Append(" ").Append(name)
                .Append(" { readonly get => ").Append(getExpr)
                .Append("; set => ").Append(fieldName).Append(" = ").Append(setExpr)
                .AppendLine("; }");
        }

        private static string ReverseCall(string type, string expr)
        {
            switch (type)
            {
                case "float":
                    return "BitConverter.Int32BitsToSingle(BinaryPrimitives.ReverseEndianness(BitConverter.SingleToInt32Bits(" + expr + ")))";
                case "double":
                    return "BitConverter.Int64BitsToDouble(BinaryPrimitives.ReverseEndianness(BitConverter.DoubleToInt64Bits(" + expr + ")))";
                default:
                    return "BinaryPrimitives.ReverseEndianness(" + expr + ")";
            }
        }

        /// <summary>
        /// Generates a getter expression for enum-typed fields: cast to underlying, reverse, cast back.
        /// E.g.: (MyEnum)BinaryPrimitives.ReverseEndianness((ushort)myField)
        /// </summary>
        internal static string CastGetterExpression(string declaredType, string primitiveType, string fieldName, EndianConversion conversion)
        {
            // Cast backing field to underlying primitive, apply reverse, cast back to declared type
            string castField = "((" + primitiveType + ")" + fieldName + ")";
            string reversed = ReverseCall(primitiveType, castField);

            switch (conversion)
            {
                case EndianConversion.AlwaysReverse:
                    return "(" + declaredType + ")" + reversed;
                case EndianConversion.Conditional:
                    return "BitConverter.IsLittleEndian ? (" + declaredType + ")" + reversed + " : " + fieldName;
                default:
                    return fieldName;
            }
        }

        /// <summary>
        /// Generates a setter expression for enum-typed fields: cast to underlying, reverse, cast back.
        /// E.g.: (MyEnum)BinaryPrimitives.ReverseEndianness((ushort)value)
        /// </summary>
        internal static string CastSetterExpression(string declaredType, string primitiveType, string valueExpr, EndianConversion conversion)
        {
            string castValue = "((" + primitiveType + ")" + valueExpr + ")";
            string reversed = ReverseCall(primitiveType, castValue);

            switch (conversion)
            {
                case EndianConversion.AlwaysReverse:
                    return "(" + declaredType + ")" + reversed;
                case EndianConversion.Conditional:
                    return "BitConverter.IsLittleEndian ? (" + declaredType + ")" + reversed + " : " + valueExpr;
                default:
                    return valueExpr;
            }
        }
    }
}
