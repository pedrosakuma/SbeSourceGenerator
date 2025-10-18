using System.Text;

namespace SbeSourceGenerator
{
    public record ConstantMessageFieldDefinition(string Name, string Id, string Type, string Description, string ValueRef) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendSummary(Description, tabs, nameof(ConstantMessageFieldDefinition));
            // Use transformed name to avoid collisions with type names
            sb.AppendLine($"public const {Type} {Name.ToKebabCase()} = {ValueRef};", tabs);
        }
    }
}
