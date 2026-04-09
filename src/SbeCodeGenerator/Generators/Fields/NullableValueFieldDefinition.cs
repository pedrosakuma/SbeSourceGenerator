using System.Text;

namespace SbeSourceGenerator
{
    public record NullableValueFieldDefinition(string Name, string Description, string PrimitiveType, int Length, string? NullValue,
        EndianConversion EndianConversion = EndianConversion.None)
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            string nullValue = NullValue ?? TypesCatalog.GetNullValue(PrimitiveType);
            string fieldName = Name.FirstCharToLower();
            string getExpr = EndianFieldHelper.GetterExpression(PrimitiveType, fieldName, EndianConversion);

            sb.AppendSummary(Description, tabs, nameof(NullableValueFieldDefinition));
            sb.AppendLine("#pragma warning disable CS0649", tabs);
            sb.AppendTabs(tabs).Append("private ").Append(PrimitiveType).Append(" ").Append(fieldName).AppendLine(";");
            sb.AppendTabs(tabs).Append("public ").Append(PrimitiveType).Append("? ").Append(Name);
            AppendNullCheck(sb, PrimitiveType, getExpr, nullValue);
            sb.AppendLine("#pragma warning restore CS0649", tabs);
        }

        internal static void AppendNullCheck(StringBuilder sb, string primitiveType, string valueExpr, string nullValue)
        {
            if (TypesCatalog.IsFloatingPoint(primitiveType))
                sb.Append(" => ").Append(primitiveType).Append(".IsNaN(").Append(valueExpr).Append(") ? null : ").Append(valueExpr).AppendLine(";");
            else
                sb.Append(" => ").Append(valueExpr).Append(" == ").Append(nullValue).Append(" ? null : ").Append(valueExpr).AppendLine(";");
        }
    }
}
