using System.Text;

namespace SbeSourceGenerator.Generators.Types
{
    public record LocalMktDateSemanticTypeDefinition(string Namespace, string Name, bool IsNullable) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendTabs(tabs).Append("public partial struct ").Append(Name).AppendLine();
            sb.AppendLine("{", tabs++);
            sb.AppendSummary("Date", tabs, nameof(LocalMktDateSemanticTypeDefinition));
            if (IsNullable)
            {
                sb.AppendTabs(tabs).Append("public DateOnly").Append((IsNullable ? "?" : "")).AppendLine(" Date => Value == null ? null : DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds((long)TimeSpan.FromDays((double)Value).TotalSeconds).UtcDateTime);");
            }
            else
            {
                sb.AppendTabs(tabs).Append("public DateOnly").Append((IsNullable ? "?" : "")).AppendLine(" Date => DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds((long)TimeSpan.FromDays((double)Value).TotalSeconds).UtcDateTime);");
            }
            sb.AppendLine("}", --tabs);
        }
    }
}
