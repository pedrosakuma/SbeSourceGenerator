using System.Text;

namespace SbeSourceGenerator
{
    public record ValueFieldDefinition(string Name, string Description, string PrimitiveType, int Length,
        EndianConversion EndianConversion = EndianConversion.None)
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendSummary(Description, tabs, nameof(ValueFieldDefinition));
            EndianFieldHelper.AppendField(sb, tabs, PrimitiveType, Name, EndianConversion);
        }
    }
}
