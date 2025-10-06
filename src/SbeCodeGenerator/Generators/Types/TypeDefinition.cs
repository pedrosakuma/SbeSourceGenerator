using System.Text;

namespace SbeSourceGenerator
{
    public record TypeDefinition(string Namespace, string Name, string Description, string PrimitiveType, string SemanticType, int Length)
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendLine($"namespace {Namespace};", tabs);
            sb.AppendSummary(Description, tabs, nameof(TypeDefinition));
            sb.AppendLine($"public partial struct {Name}", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine($"public {PrimitiveType} Value;", tabs);
            sb.AppendLine("}", --tabs);
        }
    }
}
