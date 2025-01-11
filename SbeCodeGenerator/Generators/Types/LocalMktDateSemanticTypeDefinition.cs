using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SbeSourceGenerator.Generators.Types
{
    internal record LocalMktDateSemanticTypeDefinition(string Namespace, string Name, bool IsNullable) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendLine($"namespace {Namespace};", tabs);
            sb.AppendLine($"public partial struct {Name}", tabs);
            sb.AppendLine("{", tabs);
            sb.AppendSummary("Date", tabs + 1, nameof(LocalMktDateSemanticTypeDefinition));
            if (IsNullable)
            {
                sb.AppendLine($"public DateOnly{(IsNullable ? "?" : "")} Date => Value == null ? null : DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(TimeSpan.FromDays((double)Value).Seconds).UtcDateTime);", tabs + 1);
            }
            else
            {
                sb.AppendLine($"public DateOnly{(IsNullable ? "?" : "")} Date => DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(TimeSpan.FromDays((double)Value).Seconds).UtcDateTime);", tabs + 1);
            }
            sb.AppendLine("}", tabs);
        }
    }
}