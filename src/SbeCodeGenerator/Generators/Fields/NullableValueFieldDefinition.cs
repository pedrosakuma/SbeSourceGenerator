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
            string constantName = Name + "NullValue";

            sb.AppendSummary(Description, tabs);
            sb.AppendLine("#pragma warning disable CS0649", tabs);
            AppendNullConstant(sb, tabs, PrimitiveType, constantName, nullValue);
            sb.AppendTabs(tabs).Append("private ").Append(PrimitiveType).Append(" ").Append(fieldName).AppendLine(";");
            sb.AppendTabs(tabs).Append("public readonly ").Append(PrimitiveType).Append("? ").Append(Name);
            AppendNullCheck(sb, PrimitiveType, getExpr, nullValue, constantName);
            sb.AppendLine("#pragma warning restore CS0649", tabs);
        }

        internal static void AppendNullConstant(StringBuilder sb, int tabs, string primitiveType, string constantName, string nullValue)
        {
            if (TypesCatalog.IsFloatingPoint(primitiveType))
                return; // Floating-point uses IsNaN, no sentinel constant needed
            sb.AppendTabs(tabs).Append("public const ").Append(primitiveType).Append(" ").Append(constantName).Append(" = ").Append(nullValue).AppendLine(";");
        }

        internal static void AppendNullCheck(StringBuilder sb, string primitiveType, string valueExpr, string nullValue, string? constantName = null)
        {
            if (TypesCatalog.IsFloatingPoint(primitiveType))
                sb.Append(" => ").Append(primitiveType).Append(".IsNaN(").Append(valueExpr).Append(") ? null : ").Append(valueExpr).AppendLine(";");
            else
                sb.Append(" => ").Append(valueExpr).Append(" == ").Append(constantName ?? nullValue).Append(" ? null : ").Append(valueExpr).AppendLine(";");
        }
    }
}
