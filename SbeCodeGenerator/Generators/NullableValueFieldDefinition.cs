namespace SbeSourceGenerator
{
    public record NullableValueFieldDefinition(string Name, string Description, string PrimitiveType, int Length)
        : IFileContentGenerator, IBlittable
    {
        public string GenerateFileContent()
        {
            return $$"""
                {{SummaryGenerator.Generate(Description, 1, nameof(NullableValueFieldDefinition))}}
                    private {{PrimitiveType}} {{Name.FirstCharToLower()}};
                    public {{PrimitiveType}}? {{Name}} => {{Name.FirstCharToLower()}} == {{TypesCatalog.NullValueByType[PrimitiveType]}} ? null : {{Name.FirstCharToLower()}};
                """;
        }
    }
}
