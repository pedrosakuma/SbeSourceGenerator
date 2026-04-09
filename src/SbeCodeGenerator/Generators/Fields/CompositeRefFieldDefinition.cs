using System.Text;

namespace SbeSourceGenerator
{
    /// <summary>
    /// Represents a <ref/> element inside a composite, embedding a referenced composite type as a field.
    /// </summary>
    public record CompositeRefFieldDefinition(string Name, string Description, string TypeName, int Length)
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendSummary(Description, tabs, nameof(CompositeRefFieldDefinition));
            sb.AppendTabs(tabs).Append("public ").Append(TypeName).Append(" ").Append(Name).AppendLine(";");
        }
    }
}
