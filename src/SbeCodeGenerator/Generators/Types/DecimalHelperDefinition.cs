using System.Text;

namespace SbeSourceGenerator
{
    /// <summary>
    /// Generates a ToDecimal() method on composites that match the SBE decimal pattern
    /// (mantissa field + constant exponent field).
    /// </summary>
    public record DecimalHelperDefinition(string Namespace, string Name, string Description,
        int Exponent, bool IsOptionalMantissa) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendTabs(tabs).Append("public partial struct ").Append(Name).AppendLine();
            sb.AppendLine("{", tabs++);

            var exponentStr = Exponent.ToString();
            var multiplier = $"1e{exponentStr}m";

            if (IsOptionalMantissa)
            {
                sb.AppendLine("/// <summary>", tabs);
                sb.AppendTabs(tabs).Append("/// Converts the mantissa to a decimal using the constant exponent (10^").Append(exponentStr).AppendLine(").");
                sb.AppendLine("/// Returns null if the mantissa is null (optional field).", tabs);
                sb.AppendLine("/// </summary>", tabs);
                sb.AppendTabs(tabs).Append("public readonly decimal? ToDecimal() => Mantissa.HasValue ? Mantissa.Value * ").Append(multiplier).AppendLine(" : null;");
            }
            else
            {
                sb.AppendLine("/// <summary>", tabs);
                sb.AppendTabs(tabs).Append("/// Converts the mantissa to a decimal using the constant exponent (10^").Append(exponentStr).AppendLine(").");
                sb.AppendLine("/// </summary>", tabs);
                sb.AppendTabs(tabs).Append("public readonly decimal ToDecimal() => Mantissa * ").Append(multiplier).AppendLine(";");
            }

            sb.AppendLine("}", --tabs);
        }
    }
}
