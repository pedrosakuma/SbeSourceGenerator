using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SbeSourceGenerator
{
    internal record DecimalSemanticTypeDefinition(string Namespace, string Name, List<IFileContentGenerator> Fields) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            var hasNullable = Fields.Where(f => f is NullableValueFieldDefinition).Any();
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendTabs(tabs).Append("public partial struct ").Append(Name).AppendLine();
            sb.AppendLine("{", tabs++);
            sb.AppendSummary("Decimal value from mantissa and exponent", tabs, nameof(DecimalSemanticTypeDefinition));
            sb.AppendTabs(tabs).Append("public decimal").Append((hasNullable ? "?" : "")).AppendLine(" Value => Mantissa.ToDecimalWithPrecision(-Exponent);");
            sb.AppendLine("}", --tabs);
        }
    }
}
