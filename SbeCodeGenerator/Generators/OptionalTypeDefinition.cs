namespace SbeSourceGenerator
{
    public record OptionalTypeDefinition(string Namespace, string Name, string Description, string PrimitiveType, string SemanticType,
        string NullValue, int Length) : IFileContentGenerator, IBlittable
    {
        public string GenerateFileContent()
        {
            var nullValue = NullValue;
            if (NullValue == "")
                nullValue = TypesCatalog.NullValueByType[PrimitiveType];
            return $$"""
                namespace {{Namespace}};
                {{SummaryGenerator.Generate(Description, nameof(OptionalTypeDefinition))}}
                public struct {{Name}}
                {
                    private {{PrimitiveType}} value;
                    public {{PrimitiveType}}? Value => value == {{nullValue}} ? null : value;
                }
                """;
        }
    }
}
