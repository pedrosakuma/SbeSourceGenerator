using System.Text;

namespace SbeSourceGenerator.Generators.Types
{
    public record LocalMktDateSemanticTypeDefinition(string Namespace, string Name, bool IsNullable) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendLine($"namespace {Namespace};", tabs);
            sb.AppendLine($"public partial struct {Name}", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendSummary("Date", tabs, nameof(LocalMktDateSemanticTypeDefinition));
            if (IsNullable)
            {
                sb.AppendLine($"public DateOnly{(IsNullable ? "?" : "")} Date => Value == null ? null : DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds((long)TimeSpan.FromDays((double)Value).TotalSeconds).UtcDateTime);", tabs);
            }
            else
            {
                sb.AppendLine($"public DateOnly{(IsNullable ? "?" : "")} Date => DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds((long)TimeSpan.FromDays((double)Value).TotalSeconds).UtcDateTime);", tabs);
            }
            sb.AppendLine("}", --tabs);
        }
    }
}