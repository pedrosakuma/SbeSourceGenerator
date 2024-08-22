namespace SbeSourceGenerator
{
    public record TypeDefinition(string Namespace, string Name, string Description, string PrimitiveType, string SemanticType, int Length)
        : IFileContentGenerator, IBlittable
    {
        public string GenerateFileContent()
        {
            return $$"""
                namespace {{Namespace}};
                {{SummaryGenerator.Generate(Description, nameof(TypeDefinition))}}
                public struct {{Name}}
                {
                    public {{PrimitiveType}} Value;
                }
                """;
        }
    }
}
