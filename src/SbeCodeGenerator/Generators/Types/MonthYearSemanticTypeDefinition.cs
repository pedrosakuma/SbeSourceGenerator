using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SbeSourceGenerator
{
    internal record MonthYearSemanticTypeDefinition(string Namespace, string Name, List<IFileContentGenerator> Fields) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            var hasNullable = Fields.Where(f => f is NullableValueFieldDefinition).Any();
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendTabs(tabs).Append("public partial struct ").Append(Name).AppendLine();
            sb.AppendLine("{", tabs++);
            if (hasNullable)
            {
                sb.AppendSummary("Date value from offset and unit", tabs, nameof(MonthYearSemanticTypeDefinition));
                sb.AppendTabs(tabs).AppendLine("public DateTime? Value => (Year.HasValue && Month.HasValue && Day.HasValue) && Year.Value != 9999 ? new DateTime(Year.Value, Month.Value, Day.Value) : null;");
            }
            else
            {
                sb.AppendSummary("Date value from offset and unit", tabs, nameof(MonthYearSemanticTypeDefinition));
                sb.AppendTabs(tabs).AppendLine("public DateTime Value => new DateTime(Year, Month, Day);");
            }
            sb.AppendLine("}", --tabs);
        }
    }
}
