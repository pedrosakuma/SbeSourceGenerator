using System;

namespace SbeSourceGenerator
{
    public record FixedSizeCharTypeDefinition(string Namespace, string Name, string Description,
        int Length) : IFileContentGenerator, IBlittable
    {
        public string GenerateFileContent()
        {
            if (Length == 0)
                return $$"""
                namespace {{Namespace}};
                {{SummaryGenerator.Generate(Description, nameof(FixedSizeCharTypeDefinition))}}
                public struct {{Name}}
                {
                    public byte Value;
                }
                """;
            else
                return $$"""
                namespace {{Namespace}};
                {{SummaryGenerator.Generate(Description, nameof(FixedSizeCharTypeDefinition))}}
                [System.Runtime.CompilerServices.InlineArray({{Length}})]
                public unsafe struct {{Name}}
                {
                    private byte value;
                    public override string ToString() 
                    {
                        return System.Text.Encoding.ASCII.GetString(this);
                    }
                }
                """;
        }
    }
}
