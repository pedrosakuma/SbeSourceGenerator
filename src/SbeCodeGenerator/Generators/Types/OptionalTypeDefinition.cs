using System.Text;

namespace SbeSourceGenerator
{
    public record OptionalTypeDefinition(string Namespace, string Name, string Description, string PrimitiveType, string SemanticType,
        string NullValue, int Length) : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            var nullValue = NullValue;
            if (NullValue == "")
                nullValue = TypesCatalog.GetNullValue(PrimitiveType);
            sb.AppendLine("#pragma warning disable CS0649", tabs);
            sb.AppendLine($"namespace {Namespace};", tabs);
            sb.AppendSummary(Description, tabs, nameof(OptionalTypeDefinition));
            sb.AppendLine($"public partial struct {Name}", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine($"private {PrimitiveType} value;", tabs);
            sb.AppendLine($"public {PrimitiveType}? Value => value == {nullValue} ? null : value;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("#pragma warning restore CS0649", tabs);
        }
    }
}
