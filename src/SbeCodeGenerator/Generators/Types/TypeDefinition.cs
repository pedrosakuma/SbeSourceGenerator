using System.Text;

namespace SbeSourceGenerator
{
    public record TypeDefinition(string Namespace, string Name, string Description, string PrimitiveType, string SemanticType, int Length)
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendSummary(Description, tabs, nameof(TypeDefinition));
            sb.AppendTabs(tabs).Append("public readonly partial struct ").Append(Name).AppendLine();
            sb.AppendLine("{", tabs++);
            sb.AppendTabs(tabs).Append("public readonly ").Append(PrimitiveType).AppendLine(" Value;");
            sb.AppendLine("", tabs);

            // Constructor
            sb.AppendSummary($"Initializes a new instance of {Name} with the specified value.", tabs, nameof(TypeDefinition));
            sb.AppendTabs(tabs).Append("public ").Append(Name).Append("(").Append(PrimitiveType).AppendLine(" value)");
            sb.AppendLine("{", tabs++);
            sb.AppendLine("Value = value;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("", tabs);

            // Implicit conversion from primitive to wrapper
            sb.AppendSummary($"Implicitly converts a {PrimitiveType} to {Name}.", tabs, nameof(TypeDefinition));
            sb.AppendTabs(tabs).Append("public static implicit operator ").Append(Name).Append("(").Append(PrimitiveType).Append(" value) => new ").Append(Name).AppendLine("(value);");
            sb.AppendLine("", tabs);

            // Explicit conversion from wrapper to primitive
            sb.AppendSummary($"Explicitly converts a {Name} to {PrimitiveType}.", tabs, nameof(TypeDefinition));
            sb.AppendTabs(tabs).Append("public static explicit operator ").Append(PrimitiveType).Append("(").Append(Name).AppendLine(" value) => value.Value;");

            sb.AppendLine("}", --tabs);
        }
    }
}
