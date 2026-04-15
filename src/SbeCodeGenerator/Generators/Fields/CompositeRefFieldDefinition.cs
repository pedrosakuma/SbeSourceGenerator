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
            string fieldName = Name.FirstCharToLower();
            sb.AppendSummary(Description, tabs, nameof(CompositeRefFieldDefinition));
            sb.AppendTabs(tabs).Append("private ").Append(TypeName).Append(" ").Append(fieldName).AppendLine(";");
            sb.AppendTabs(tabs).AppendLine("[System.Diagnostics.CodeAnalysis.UnscopedRef]");
            sb.AppendTabs(tabs).Append("public ref ").Append(TypeName).Append(" ").Append(Name)
                .Append(" => ref ").Append(fieldName).AppendLine(";");
        }
    }
}
