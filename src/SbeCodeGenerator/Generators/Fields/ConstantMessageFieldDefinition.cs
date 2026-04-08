using System.Text;

namespace SbeSourceGenerator
{
    public record ConstantMessageFieldDefinition(string Name, string Id, string Type, string Description, string ValueRef) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendSummary(Description, tabs, nameof(ConstantMessageFieldDefinition));
            // Use transformed name to avoid collisions with type names
            sb.AppendTabs(tabs).Append("public const ").Append(Type).Append(" ").Append(Name.ToKebabCase()).Append(" = ").Append(ValueRef).AppendLine(";");
        }
    }
}
