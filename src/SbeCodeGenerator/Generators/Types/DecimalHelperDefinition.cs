using System.Globalization;
using System.Text;

namespace SbeSourceGenerator
{
    /// <summary>
    /// Generates a ToDecimal() method and derived constants on composites that match
    /// the SBE decimal pattern (mantissa field + constant exponent field).
    /// </summary>
    public record DecimalHelperDefinition(string Namespace, string Name, string Description,
        int Exponent, bool IsOptionalMantissa) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendTabs(tabs).Append("public partial struct ").Append(Name).AppendLine();
            sb.AppendLine("{", tabs++);

            var exponentStr = Exponent.ToString(CultureInfo.InvariantCulture);
            var multiplier = $"1e{exponentStr}m";

            // Issue #145: emit derived constants — single source of truth for decimal places.
            AppendDerivedConstants(sb, tabs);

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

        private void AppendDerivedConstants(StringBuilder sb, int tabs)
        {
            var exponentStr = Exponent.ToString(CultureInfo.InvariantCulture);
            int decimals = Exponent < 0 ? -Exponent : 0;

            // Note: an `Exponent` constant is already emitted by the composite's constant exponent field —
            // we skip re-emitting it to avoid duplicate-member errors. We add only derived helpers.

            sb.AppendTabs(tabs).AppendLine("/// <summary>Number of fractional decimal places (max(0, -Exponent)). Use for rendering / formatting.</summary>");
            sb.AppendTabs(tabs).Append("public const int Decimals = ").Append(decimals.ToString(CultureInfo.InvariantCulture)).AppendLine(";");

            // Multiplier as double — mantissa * Multiplier == real value
            sb.AppendTabs(tabs).AppendLine("/// <summary>Pre-computed scaling factor as <see cref=\"double\"/> (mantissa * Multiplier == real value).</summary>");
            sb.AppendTabs(tabs).Append("public const double Multiplier = 1e").Append(exponentStr).AppendLine(";");

            sb.AppendTabs(tabs).AppendLine("/// <summary>Pre-computed scaling factor as <see cref=\"decimal\"/> (mantissa * MultiplierDecimal == real value).</summary>");
            sb.AppendTabs(tabs).Append("public const decimal MultiplierDecimal = 1e").Append(exponentStr).AppendLine("m;");

            // Divisor as long — only emit when a long literal can represent 10^|Exponent| exactly.
            // long max ~9.22e18, so |Exponent| up to 18.
            if (Exponent <= 0 && Exponent >= -18)
            {
                long divisor = 1;
                for (int i = 0; i < -Exponent; i++) divisor *= 10;
                sb.AppendTabs(tabs).AppendLine("/// <summary>Pre-computed integer divisor (mantissa / Divisor truncated == integer part). Only emitted when Exponent ∈ [-18, 0].</summary>");
                sb.AppendTabs(tabs).Append("public const long Divisor = ").Append(divisor.ToString(CultureInfo.InvariantCulture)).AppendLine("L;");
            }

            sb.AppendLine("", tabs);
        }
    }
}

