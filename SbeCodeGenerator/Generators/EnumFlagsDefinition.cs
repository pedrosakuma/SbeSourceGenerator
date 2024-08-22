using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    public record EnumFlagsDefinition(string Namespace, string Name, string Description, string EncodingType, int Length, List<EnumFieldDefinition> Fields)
        : IFileContentGenerator, IBlittable
    {
        public string GenerateFileContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine($$"""
                namespace {{Namespace}};
                {{SummaryGenerator.Generate(Description, nameof(EnumFlagsDefinition))}}
                [System.Flags]
                public enum {{Name}} : {{EncodingType}}
                {
                """);
            foreach (var field in Fields)
            {
                if (field.Description != "")
                    sb.AppendLine(SummaryGenerator.Generate(field.Description, 1, nameof(EnumFlagsDefinition)));
                sb.AppendLine($"\t{field.Name} = {1 << int.Parse(field.Value)},");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}