using System.Text;

namespace SbeSourceGenerator
{
    public record ConstantTypeFieldDefinition(string Name, string Description, string PrimitiveType, string Value, string ValueRef)
        : IFileContentGenerator, IBlittable
    {
        public int Length => 0;
        public string GenerateFileContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine(SummaryGenerator.Generate(Description, 1, nameof(DecimalSemanticTypeDefinition)));
            if (Value == "")
                sb.AppendLine($"\tpublic const {PrimitiveType} {Name} = ({PrimitiveType}){ValueRef};");
            else
                sb.AppendLine($"\tpublic const {PrimitiveType} {Name} = {Value};");
            return sb.ToString();
        }
    }
}
