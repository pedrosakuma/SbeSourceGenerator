namespace SbeSourceGenerator
{
    public record OptionalTypeDefinition(string Namespace, string Name, string Description, string PrimitiveType, string SemanticType,
        string NullValue) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            var nullValue = NullValue;
            if (NullValue == "")
                nullValue = PrimitiveTypes.NullValueByType[PrimitiveType];
            return $$"""
                namespace {{Namespace}};
                /// <summary>
                /// {{Description}}
                /// </summary>
                public struct {{Name}}
                {
                    private {{PrimitiveType}} value;
                    public {{PrimitiveType}}? Value => value == {{nullValue}} ? null : value;
                }
                """;
        }
    }
}
