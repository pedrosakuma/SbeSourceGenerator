namespace SbeSourceGenerator
{
    public record FixedSizeCharTypeDefinition(string Namespace, string Name, string Description, 
        string Length) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            if (Length == "")
                return $$"""
                namespace {{Namespace}};
                /// <summary>
                /// {{Description}}
                /// </summary>
                public struct {{Name}}
                {
                    public byte Value;
                }
                """;
            else
                return $$"""
                namespace {{Namespace}};
                /// <summary>
                /// {{Description}}
                /// </summary>
                [System.Runtime.CompilerServices.InlineArray({{Length}})]
                public struct {{Name}}
                {
                    private byte value;
                    public Span<byte> Value => System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref value, {{Length}});
                    public override string ToString() 
                    {
                        return System.Text.Encoding.ASCII.GetString(Value);
                    }
                }
                """;
        }
    }
}
