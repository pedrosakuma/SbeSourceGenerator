using System.Text;

namespace SbeSourceGenerator
{
    public record ConstantTypeDefinition(string Namespace, string Name, string Description, string PrimitiveType, string SemanticType,
        string Length, string Value) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            var primitiveType = PrimitiveType;
            var value = Value;
            if (primitiveType == "char" && Length != "")
            {
                primitiveType = "string";
                value = $"\"{value}\"";
            }

            sb.AppendLine($"namespace {Namespace};");
            sb.AppendSummary(Description, tabs, nameof(ConstantTypeDefinition));
            sb.AppendLine($"public struct {Name}");
            sb.AppendLine("{", tabs++);
            sb.AppendLine($"public const {primitiveType} Value = {value};", tabs);
            sb.AppendLine("}", --tabs);
        }
    }
}
