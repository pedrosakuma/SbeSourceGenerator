using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SbeSourceGenerator
{
    internal record UTCTimestampSemanticTypeDefinition(string Namespace, string Name, List<IFileContentGenerator> Fields) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            var hasNullable = Fields.Where(f => f is NullableValueFieldDefinition).Any();
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendTabs(tabs).Append("public partial struct ").Append(Name).AppendLine();
            sb.AppendLine("{", tabs++);
            sb.AppendSummary("Date value from offset and unit", tabs, nameof(UTCTimestampSemanticTypeDefinition));
            sb.AppendTabs(tabs).Append("public DateTime").Append((hasNullable ? "?" : "")).AppendLine(" Value => Time.ToDateTimeWithUnit(Unit);");
            sb.AppendLine("}", --tabs);
        }
    }
}
