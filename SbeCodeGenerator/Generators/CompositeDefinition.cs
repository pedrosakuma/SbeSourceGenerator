using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    public record CompositeDefinition(string Namespace, string Name, string Description, string SemanticType, List<IFileContentGenerator> Fields) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine($$"""
                namespace {{Namespace}};
                {{SummaryGenerator.Generate(Description, nameof(CompositeDefinition))}}
                public partial struct {{Name}}
                {
                """);
            foreach (var field in Fields)
                sb.AppendLine(field.GenerateFileContent());

            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
