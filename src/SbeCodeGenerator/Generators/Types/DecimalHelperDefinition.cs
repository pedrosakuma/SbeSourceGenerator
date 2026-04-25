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
            sb.AppendTabs(tabs).Append("public partial struct ").Append(Name).Append(" : ISpanFormattable").AppendLine();
            sb.AppendLine("{", tabs++);

            var exponentStr = Exponent.ToString(CultureInfo.InvariantCulture);
            var multiplier = $"1e{exponentStr}m";
            int decimals = Exponent < 0 ? -Exponent : 0;
            // Default format string (e.g. "F4") used when caller passes empty format.
            var defaultFormat = $"\"F{decimals.ToString(CultureInfo.InvariantCulture)}\"";

            // Issue #145: emit derived constants — single source of truth for decimal places.
            AppendDerivedConstants(sb, tabs);

            if (IsOptionalMantissa)
            {
                sb.AppendLine("/// <summary>", tabs);
                sb.AppendTabs(tabs).Append("/// Converts the mantissa to a decimal using the constant exponent (10^").Append(exponentStr).AppendLine(").");
                sb.AppendLine("/// Returns null if the mantissa is null (optional field).", tabs);
                sb.AppendLine("/// </summary>", tabs);
                sb.AppendTabs(tabs).Append("public readonly decimal? ToDecimal() => Mantissa.HasValue ? Mantissa.Value * ").Append(multiplier).AppendLine(" : null;");

                // Issue #153: ISpanFormattable — allocation-free decimal formatting using the schema's known decimal count.
                sb.AppendLine("/// <summary>Allocation-free formatting; defaults to <c>F" + decimals.ToString(CultureInfo.InvariantCulture) + "</c>. Empty output for null mantissa.</summary>", tabs);
                sb.AppendLine("public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("var v = ToDecimal();", tabs);
                sb.AppendLine("if (!v.HasValue) { charsWritten = 0; return true; }", tabs);
                sb.AppendTabs(tabs).Append("return v.Value.TryFormat(destination, out charsWritten, format.IsEmpty ? ").Append(defaultFormat).AppendLine(" : format, provider);");
                sb.AppendLine("}", --tabs);

                sb.AppendLine("/// <summary>IFormattable implementation; uses <c>F" + decimals.ToString(CultureInfo.InvariantCulture) + "</c> when format is null. Returns empty string for null mantissa.</summary>", tabs);
                sb.AppendTabs(tabs).Append("public readonly string ToString(string? format, IFormatProvider? provider) => ToDecimal() is decimal v ? v.ToString(format ?? ").Append(defaultFormat).AppendLine(", provider) : string.Empty;");
            }
            else
            {
                sb.AppendLine("/// <summary>", tabs);
                sb.AppendTabs(tabs).Append("/// Converts the mantissa to a decimal using the constant exponent (10^").Append(exponentStr).AppendLine(").");
                sb.AppendLine("/// </summary>", tabs);
                sb.AppendTabs(tabs).Append("public readonly decimal ToDecimal() => Mantissa * ").Append(multiplier).AppendLine(";");

                // Issue #153
                sb.AppendLine("/// <summary>Allocation-free formatting; defaults to <c>F" + decimals.ToString(CultureInfo.InvariantCulture) + "</c>.</summary>", tabs);
                sb.AppendLine("public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendTabs(tabs).Append("return ToDecimal().TryFormat(destination, out charsWritten, format.IsEmpty ? ").Append(defaultFormat).AppendLine(" : format, provider);");
                sb.AppendLine("}", --tabs);

                sb.AppendLine("/// <summary>IFormattable implementation; uses <c>F" + decimals.ToString(CultureInfo.InvariantCulture) + "</c> when format is null.</summary>", tabs);
                sb.AppendTabs(tabs).Append("public readonly string ToString(string? format, IFormatProvider? provider) => ToDecimal().ToString(format ?? ").Append(defaultFormat).AppendLine(", provider);");
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

