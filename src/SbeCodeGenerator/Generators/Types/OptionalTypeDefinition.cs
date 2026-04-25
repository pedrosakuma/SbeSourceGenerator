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
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendSummary(Description, tabs);
            sb.AppendTabs(tabs).Append("public partial struct ").Append(Name).AppendLine();
            sb.AppendLine("{", tabs++);
            NullableValueFieldDefinition.AppendNullConstant(sb, tabs, PrimitiveType, "ValueNullValue", nullValue);
            sb.AppendTabs(tabs).Append("private ").Append(PrimitiveType).AppendLine(" value;");
            sb.AppendTabs(tabs).Append("public readonly ").Append(PrimitiveType).Append("? Value");
            NullableValueFieldDefinition.AppendNullCheck(sb, PrimitiveType, "value", nullValue, "ValueNullValue");
            sb.AppendLine("}", --tabs);
            sb.AppendLine("#pragma warning restore CS0649", tabs);
        }
    }
}
