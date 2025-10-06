using System.Text;

namespace SbeSourceGenerator
{
    public record ConstantMessageFieldDefinition(string Name, string Id, string Type, string Description, string ValueRef) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendSummary(Description, tabs, nameof(ConstantMessageFieldDefinition));
            sb.AppendLine($"public const {Type} {Name.ToKebabCase()} = {ValueRef};", tabs);
        }
    }
}
