using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    public record EnumDefinition(string Namespace, string Name, string Description, string EncodingType, string SemanticType, int Length, List<EnumFieldDefinition> Fields)
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendLine($"namespace {Namespace};", tabs);
            sb.AppendSummary(Description, tabs, nameof(EnumDefinition));
            sb.AppendLine($"public enum {Name} : {ChangeTypeIfNeeded(EncodingType)}", tabs);
            sb.AppendLine("{", tabs);
            foreach (var field in Fields)
            {
                if (field.Description != "")
                    sb.AppendSummary(Description, tabs + 1, nameof(EnumDefinition));
                sb.AppendLine($"{field.Name} = {IncludeQuotationAndCastIfNeeded(field.Value, EncodingType)},", tabs + 1);
            }
            sb.AppendLine("}", tabs);
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