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
                sb.AppendLine("public override string ToString()", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("ReadOnlySpan<byte> span = this;", tabs);
                sb.AppendTabs(tabs).AppendLine("var index = span.IndexOf((byte)0);");
                sb.AppendTabs(tabs).Append("return System.Text.").Append(ResolvedEncoding).Append(".GetString(span.Slice(0, index == -1 ? ").Append(Length).AppendLine(" : index));");
                sb.AppendLine("}", --tabs);
                sb.AppendLine("}", --tabs);
            }
        }
    }
}
