namespace SbeSourceGenerator
{
    public record ArrayFieldDefinition(string Name, string Description, string PrimitiveType) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            return $$"""
                {{SummaryGenerator.Generate(Description, 1, nameof(ArrayFieldDefinition))}}
                    public {{PrimitiveType}}[] {{Name}};
                """;
        }
    }
}
