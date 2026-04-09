using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    public record EnumDefinition(string Namespace, string Name, string Description, string EncodingType, string SemanticType, int Length, List<EnumFieldDefinition> Fields)
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendSummary(Description, tabs, nameof(EnumDefinition));
            sb.AppendTabs(tabs).Append("public enum ").Append(Name).Append(" : ").Append(ChangeTypeIfNeeded(EncodingType)).AppendLine();
            sb.AppendLine("{", tabs++);
            foreach (var field in Fields)
            {
                if (field.Description != "")
                    sb.AppendSummary(Description, tabs, nameof(EnumDefinition));
                sb.AppendTabs(tabs).Append(field.Name).Append(" = ").Append(IncludeQuotationAndCastIfNeeded(field.Value, EncodingType)).AppendLine(",");
            }
            sb.AppendLine("}", --tabs);
        }

        private string IncludeQuotationAndCastIfNeeded(string value, string encodingType)
        {
            return encodingType switch
            {
                "char" => $"(byte)'{value}'",
                _ => value
            };
        }

        private string ChangeTypeIfNeeded(string encodingType)
        {
            return encodingType switch
            {
                "char" => "byte",
                _ => encodingType
            };
        }
    }
}
