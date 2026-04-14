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
                sb.AppendSummary(Description, tabs, nameof(FixedSizeCharTypeDefinition));
                sb.AppendTabs(tabs).Append("public struct ").Append(Name).AppendLine();
                sb.AppendLine("{", tabs++);
                sb.AppendTabs(tabs).AppendLine("public byte Value;");
                sb.AppendLine("}", --tabs);
            }
            else
            {
                sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
                sb.AppendSummary(Description, tabs, nameof(FixedSizeCharTypeDefinition));
                sb.AppendTabs(tabs).Append("[System.Runtime.CompilerServices.InlineArray(").Append(Length).AppendLine(")]");
                sb.AppendTabs(tabs).Append("public struct ").Append(Name).AppendLine();
                sb.AppendLine("{", tabs++);
                sb.AppendLine("private byte value;", tabs);

                // AsSpan() — zero-allocation trimmed content access
                sb.AppendLine("/// <summary>Returns the trimmed content as a ReadOnlySpan&lt;byte&gt; (zero-allocation). Excludes null-termination padding.</summary>", tabs);
                sb.AppendLine("[System.Diagnostics.CodeAnalysis.UnscopedRef]", tabs);
                sb.AppendLine("public ReadOnlySpan<byte> AsSpan()", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("ReadOnlySpan<byte> span = this;", tabs);
                sb.AppendTabs(tabs).AppendLine("var index = span.IndexOf((byte)0);");
                sb.AppendTabs(tabs).Append("return span.Slice(0, index == -1 ? ").Append(Length).AppendLine(" : index);");
                sb.AppendLine("}", --tabs);

                // Equals(ReadOnlySpan<byte>) — zero-allocation comparison
                sb.AppendLine("/// <summary>Compares the content to a byte span without allocation. Useful with UTF-8 string literals: symbol.Equals(\"PETR4\"u8)</summary>", tabs);
                sb.AppendLine("public bool Equals(ReadOnlySpan<byte> other) => AsSpan().SequenceEqual(other);", tabs);

                // ToString() — delegates to AsSpan()
                sb.AppendTabs(tabs).Append("public override string ToString() => System.Text.").Append(ResolvedEncoding).AppendLine(".GetString(AsSpan());");
                sb.AppendLine("}", --tabs);
            }
        }
    }
}
