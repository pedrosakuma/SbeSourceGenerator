using System.Globalization;
using System.Text;

namespace SbeSourceGenerator
{
    public record TypeDefinition(string Namespace, string Name, string Description, string PrimitiveType, string SemanticType, int Length,
        string MinValue = "", string MaxValue = "")
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendSummary(Description, tabs);
            sb.AppendTabs(tabs).Append("public readonly partial struct ").Append(Name).AppendLine();
            sb.AppendLine("{", tabs++);
            sb.AppendTabs(tabs).Append("public readonly ").Append(PrimitiveType).AppendLine(" Value;");
            sb.AppendLine("", tabs);

            // Issue #145: emit derived numeric range constants when schema defines them.
            AppendRangeConstants(sb, tabs);

            // Constructor
            sb.AppendSummary($"Initializes a new instance of {Name} with the specified value.", tabs);
            sb.AppendTabs(tabs).Append("public ").Append(Name).Append("(").Append(PrimitiveType).AppendLine(" value)");
            sb.AppendLine("{", tabs++);
            sb.AppendLine("Value = value;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("", tabs);

            // Implicit conversion from primitive to wrapper
            sb.AppendSummary($"Implicitly converts a {PrimitiveType} to {Name}.", tabs);
            sb.AppendTabs(tabs).Append("public static implicit operator ").Append(Name).Append("(").Append(PrimitiveType).Append(" value) => new ").Append(Name).AppendLine("(value);");
            sb.AppendLine("", tabs);

            // Explicit conversion from wrapper to primitive
            sb.AppendSummary($"Explicitly converts a {Name} to {PrimitiveType}.", tabs);
            sb.AppendTabs(tabs).Append("public static explicit operator ").Append(PrimitiveType).Append("(").Append(Name).AppendLine(" value) => value.Value;");

            sb.AppendLine("}", --tabs);
        }

        private void AppendRangeConstants(StringBuilder sb, int tabs)
        {
            // Floating-point types can't be represented as compile-time const literals reliably.
            if (PrimitiveType == "float" || PrimitiveType == "double")
                return;

            string? minLiteral = FormatRangeLiteral(MinValue, PrimitiveType);
            string? maxLiteral = FormatRangeLiteral(MaxValue, PrimitiveType);

            if (minLiteral == null && maxLiteral == null)
                return;

            if (minLiteral != null)
            {
                sb.AppendTabs(tabs).AppendLine("/// <summary>Minimum permitted value (from schema).</summary>");
                sb.AppendTabs(tabs).Append("public const ").Append(PrimitiveType).Append(" MinValue = ").Append(minLiteral).AppendLine(";");
            }
            if (maxLiteral != null)
            {
                sb.AppendTabs(tabs).AppendLine("/// <summary>Maximum permitted value (from schema).</summary>");
                sb.AppendTabs(tabs).Append("public const ").Append(PrimitiveType).Append(" MaxValue = ").Append(maxLiteral).AppendLine(";");
            }
            sb.AppendLine("", tabs);
        }

        private static string? FormatRangeLiteral(string raw, string primitiveType)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            // Validate it parses as an integer of the right kind. We don't reformat — emit verbatim with a suffix when needed.
            string trimmed = raw.Trim();
            switch (primitiveType)
            {
                case "long":
                    return long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ? trimmed + "L" : null;
                case "ulong":
                    return ulong.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ? trimmed + "UL" : null;
                case "uint":
                    return uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ? trimmed + "U" : null;
                case "int":
                    return int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ? trimmed : null;
                case "short":
                    return short.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ? "(short)" + trimmed : null;
                case "ushort":
                    return ushort.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ? "(ushort)" + trimmed : null;
                case "sbyte":
                    return sbyte.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ? "(sbyte)" + trimmed : null;
                case "byte":
                    return byte.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ? "(byte)" + trimmed : null;
                default:
                    return null;
            }
        }
    }
}

