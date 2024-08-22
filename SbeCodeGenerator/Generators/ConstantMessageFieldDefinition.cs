namespace SbeSourceGenerator
{
    public record ConstantMessageFieldDefinition(string Name, string Id, string Type, string Description, string ValueRef) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            return $"""
                {SummaryGenerator.Generate(Description, 1, nameof(ConstantMessageFieldDefinition))}
                    public const {Type} {Name.ToKebabCase()} = {ValueRef};
                """;
        }
    }
}
