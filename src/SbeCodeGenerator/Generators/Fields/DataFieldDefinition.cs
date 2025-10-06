using System.Text;

namespace SbeSourceGenerator.Generators.Fields
{
    public record DataFieldDefinition(string Name, string Id, string Type, string Description) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
        }
    }
}
