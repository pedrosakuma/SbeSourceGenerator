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
            sb.AppendLine($"namespace {Namespace};", tabs);
            sb.AppendLine($"public partial struct {Name}", tabs);
            sb.AppendLine("{", tabs);
            sb.AppendSummary("Date value from offset and unit", tabs + 1, nameof(UTCTimestampSemanticTypeDefinition));
            sb.AppendLine($"public DateTime{(hasNullable ? "?" : "")} Value => Time.ToDateTimeWithUnit(Unit);", tabs + 1);
            sb.AppendLine("}", tabs);
        }
    }
}