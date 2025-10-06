using System.Text;

namespace SbeSourceGenerator
{
    public record ValueFieldDefinition(string Name, string Description, string PrimitiveType, int Length)
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendSummary(Description, tabs, nameof(ValueFieldDefinition));
            sb.AppendLine($"public {PrimitiveType} {Name};", tabs);
        }
    }
}
