using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    public record EnumDefinition(string Namespace, string Name, string Description, string EncodingType, string SemanticType, int Length, List<EnumFieldDefinition> Fields)
        : IFileContentGenerator, IBlittable
    {
        public string GenerateFileContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine($$"""
                namespace {{Namespace}};
                {{SummaryGenerator.Generate(Description, nameof(EnumDefinition))}}
                public enum {{Name}} : {{ChangeTypeIfNeeded(EncodingType)}}
                {
                """);
            foreach (var field in Fields)
            {
                if (field.Description != "")
                    sb.AppendLine(SummaryGenerator.Generate(field.Description, 1, nameof(EnumDefinition)));
                sb.AppendLine($"\t{field.Name} = {IncludeQuotationAndCastIfNeeded(field.Value, EncodingType)},");
            }
            sb.AppendLine("}");
            return sb.ToString();
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