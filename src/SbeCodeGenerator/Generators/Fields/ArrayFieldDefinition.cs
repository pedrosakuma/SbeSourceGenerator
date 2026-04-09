using System.Text;

namespace SbeSourceGenerator
{
    public record ArrayFieldDefinition(string Name, string Description, string PrimitiveType) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendSummary(Description, tabs, nameof(ArrayFieldDefinition));
            sb.AppendTabs(tabs).Append("public ReadOnlySpan<").Append(PrimitiveType).Append("> ").Append(Name).AppendLine(";");
        }
    }
}
