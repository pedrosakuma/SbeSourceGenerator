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
            sb.AppendLine($"namespace {Namespace};", tabs);
            sb.AppendLine($"public partial struct {Name}", tabs);
            sb.AppendLine("{", tabs);
            if (hasNullable)
            {
                sb.AppendSummary("Date value from offset and unit", tabs + 1, nameof(MonthYearSemanticTypeDefinition));
                sb.AppendLine($"public DateTime? Value => (Year.HasValue && Month.HasValue && Day.HasValue) ? new DateTime(Year.Value, Month.Value, Day.Value) : null;", tabs + 1);
            }
            else
            {
                sb.AppendSummary("Date value from offset and unit", tabs + 1, nameof(MonthYearSemanticTypeDefinition));
                sb.AppendLine($"public DateTime Value => new DateTime(Year, Month, Day);", tabs + 1);
            }
            sb.AppendLine("}", tabs);
        }
    }
}