using System.Text;

namespace SbeSourceGenerator
{
    public record NullableValueFieldDefinition(string Name, string Description, string PrimitiveType, int Length, string? NullValue)
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            string nullValue = NullValue ?? TypesCatalog.GetNullValue(PrimitiveType);
            sb.AppendSummary(Description, tabs, nameof(NullableValueFieldDefinition));
            sb.AppendLine("#pragma warning disable CS0649", tabs);
            sb.AppendTabs(tabs).Append("private ").Append(PrimitiveType).Append(" ").Append(Name.FirstCharToLower()).AppendLine(";");
            sb.AppendTabs(tabs).Append("public ").Append(PrimitiveType).Append("? ").Append(Name).Append(" => ").Append(Name.FirstCharToLower()).Append(" == ").Append(nullValue).Append(" ? null : ").Append(Name.FirstCharToLower()).AppendLine(";");
            sb.AppendLine("#pragma warning restore CS0649", tabs);
        }
    }
}
