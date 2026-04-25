using System.Text;

namespace SbeSourceGenerator
{
    public record FixedSizeCharTypeDefinition(string Namespace, string Name, string Description,
        int Length, string CharacterEncoding = "") : IFileContentGenerator, IBlittable
    {
        private string ResolvedEncoding => CharacterEncoding.ToUpperInvariant() switch
        {
            "UTF-8" or "UTF8" => "Encoding.UTF8",
            _ => "Encoding.Latin1"
        };

        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            if (Length == 0)
            {
                sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
                sb.AppendSummary(Description, tabs);
                sb.AppendTabs(tabs).Append("public struct ").Append(Name).AppendLine();
                sb.AppendLine("{", tabs++);
                sb.AppendTabs(tabs).AppendLine("public byte Value;");
                sb.AppendLine("}", --tabs);
            }
            else
            {
                sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
                sb.AppendSummary(Description, tabs);
                sb.AppendTabs(tabs).Append("[System.Runtime.CompilerServices.InlineArray(").Append(Length).AppendLine(")]");
                sb.AppendTabs(tabs).Append("public struct ").Append(Name).Append(" : ISpanFormattable").AppendLine();
                sb.AppendLine("{", tabs++);
                sb.AppendLine("private byte value;", tabs);

                // AsSpan() — zero-allocation trimmed content access
                sb.AppendLine("/// <summary>Returns the trimmed content as a ReadOnlySpan&lt;byte&gt; (zero-allocation). Excludes null-termination padding.</summary>", tabs);
                sb.AppendLine("[System.Diagnostics.CodeAnalysis.UnscopedRef]", tabs);
                sb.AppendLine("public readonly ReadOnlySpan<byte> AsSpan()", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("ReadOnlySpan<byte> span = this;", tabs);
                sb.AppendTabs(tabs).AppendLine("var index = span.IndexOf((byte)0);");
                sb.AppendTabs(tabs).Append("return span.Slice(0, index == -1 ? ").Append(Length).AppendLine(" : index);");
                sb.AppendLine("}", --tabs);

                // Issue #155: AsTrimmedSpan() — additionally trims trailing space padding (FIX convention).
                sb.AppendLine("/// <summary>", tabs);
                sb.AppendLine("/// Like <see cref=\"AsSpan\"/> but additionally trims trailing space (0x20) padding —", tabs);
                sb.AppendLine("/// matches the behavior of <c>ToString().Trim()</c> without allocating.", tabs);
                sb.AppendLine("/// </summary>", tabs);
                sb.AppendLine("[System.Diagnostics.CodeAnalysis.UnscopedRef]", tabs);
                sb.AppendLine("public readonly ReadOnlySpan<byte> AsTrimmedSpan()", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("var span = AsSpan();", tabs);
                sb.AppendLine("while (span.Length > 0 && span[span.Length - 1] == (byte)' ') span = span.Slice(0, span.Length - 1);", tabs);
                sb.AppendLine("return span;", tabs);
                sb.AppendLine("}", --tabs);

                // Equals(ReadOnlySpan<byte>) — zero-allocation comparison
                sb.AppendLine("/// <summary>Compares the content to a byte span without allocation. Useful with UTF-8 string literals: symbol.Equals(\"PETR4\"u8)</summary>", tabs);
                sb.AppendLine("public readonly bool Equals(ReadOnlySpan<byte> other) => AsSpan().SequenceEqual(other);", tabs);

                // Issue #153: ISpanFormattable — allocation-free formatting for hot-path logging.
                sb.AppendLine("/// <summary>Allocation-free formatting into a destination span. Returns the trimmed character content.</summary>", tabs);
                sb.AppendLine("public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("var src = AsTrimmedSpan();", tabs);
                sb.AppendTabs(tabs).Append("int needed = System.Text.").Append(ResolvedEncoding).AppendLine(".GetCharCount(src);");
                sb.AppendLine("if (destination.Length < needed) { charsWritten = 0; return false; }", tabs);
                sb.AppendTabs(tabs).Append("charsWritten = System.Text.").Append(ResolvedEncoding).AppendLine(".GetChars(src, destination);");
                sb.AppendLine("return true;", tabs);
                sb.AppendLine("}", --tabs);

                sb.AppendLine("/// <summary>IFormattable implementation; delegates to <see cref=\"ToString()\"/>. Format and provider are ignored.</summary>", tabs);
                sb.AppendLine("public readonly string ToString(string? format, IFormatProvider? formatProvider) => ToString();", tabs);

                // ToString() — delegates to AsSpan()
                sb.AppendTabs(tabs).Append("public readonly override string ToString() => System.Text.").Append(ResolvedEncoding).AppendLine(".GetString(AsSpan());");
                sb.AppendLine("}", --tabs);
            }
        }
    }
}
