namespace SbeSourceGenerator
{
    public record ValueFieldDefinition(string Name, string Description, string PrimitiveType, int Length)
        : IFileContentGenerator, IBlittable
    {
        public string GenerateFileContent()
        {
            return $$"""
                {{SummaryGenerator.Generate(Description, 1, nameof(ValueFieldDefinition))}}
                    public {{PrimitiveType}} {{Name}};
                """;
        }
    }
}
