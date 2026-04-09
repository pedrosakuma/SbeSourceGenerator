using System.Text;

namespace SbeSourceGenerator.Generators.Fields
{
    public record DataFieldDefinition(string Name, string Id, string Type, string Description, string LengthPrefixType = "byte") : IFileContentGenerator
    {
        public int MaxLength => LengthPrefixType switch
        {
            "ushort" => 65535,
            "uint" => int.MaxValue, // practical limit
            _ => 255
        };

        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
        }
    }
}
