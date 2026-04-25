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

            sb.Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendSummary(Description, tabs);
            sb.Append("public readonly struct ").AppendLine(Name);
            sb.AppendLine("{", tabs++);
            sb.AppendTabs(tabs).Append("public const ").Append(primitiveType).Append(" Value = ").Append(value).AppendLine(";");
            sb.AppendLine("}", --tabs);
        }
    }
}
