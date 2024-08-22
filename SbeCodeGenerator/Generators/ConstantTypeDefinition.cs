namespace SbeSourceGenerator
{
    public record ConstantTypeDefinition(string Namespace, string Name, string Description, string PrimitiveType, string SemanticType,
        string Length, string Value) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            var primitiveType = PrimitiveType;
            var value = Value;
            if (primitiveType == "char" && Length != "")
            {
                primitiveType = "string";
                value = $"\"{value}\"";
            }

            return $$"""
                namespace {{Namespace}};
                {{SummaryGenerator.Generate(Description, nameof(ConstantTypeDefinition))}}
                public struct {{Name}}
                {
                    public const {{primitiveType}} Value = {{value}};
                }
                """;
        }
    }
}
