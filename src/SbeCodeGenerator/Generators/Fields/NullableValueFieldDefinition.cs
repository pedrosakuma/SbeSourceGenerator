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
            sb.AppendTabs(tabs).Append("public ").Append(PrimitiveType).Append("? ").Append(Name);
            AppendNullCheck(sb, PrimitiveType, Name.FirstCharToLower(), nullValue);
            sb.AppendLine("#pragma warning restore CS0649", tabs);
        }

        internal static void AppendNullCheck(StringBuilder sb, string primitiveType, string fieldName, string nullValue)
        {
            if (TypesCatalog.IsFloatingPoint(primitiveType))
                sb.Append(" => ").Append(primitiveType).Append(".IsNaN(").Append(fieldName).Append(") ? null : ").Append(fieldName).AppendLine(";");
            else
                sb.Append(" => ").Append(fieldName).Append(" == ").Append(nullValue).Append(" ? null : ").Append(fieldName).AppendLine(";");
        }
    }
}
