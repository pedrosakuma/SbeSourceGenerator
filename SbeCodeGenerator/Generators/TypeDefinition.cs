namespace SbeSourceGenerator
{
    public record TypeDefinition(string Namespace, string Name, string Description, string PrimitiveType, string SemanticType) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            return $$"""
                namespace {{Namespace}};
                /// <summary>
                /// {{Description}}
                /// </summary>
                public struct {{Name}}
                {
                    public {{PrimitiveType}} Value;
                }
                """;
        }
    }
}
