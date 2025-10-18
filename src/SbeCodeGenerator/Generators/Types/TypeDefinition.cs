using System.Text;

namespace SbeSourceGenerator
{
    public record TypeDefinition(string Namespace, string Name, string Description, string PrimitiveType, string SemanticType, int Length)
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendLine($"namespace {Namespace};", tabs);
            sb.AppendSummary(Description, tabs, nameof(TypeDefinition));
            sb.AppendLine($"public readonly partial struct {Name}", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine($"public readonly {PrimitiveType} Value;", tabs);
            sb.AppendLine("", tabs);

            // Constructor
            sb.AppendSummary($"Initializes a new instance of {Name} with the specified value.", tabs, nameof(TypeDefinition));
            sb.AppendLine($"public {Name}({PrimitiveType} value)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("Value = value;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("", tabs);

            // Implicit conversion from primitive to wrapper
            sb.AppendSummary($"Implicitly converts a {PrimitiveType} to {Name}.", tabs, nameof(TypeDefinition));
            sb.AppendLine($"public static implicit operator {Name}({PrimitiveType} value) => new {Name}(value);", tabs);
            sb.AppendLine("", tabs);

            // Explicit conversion from wrapper to primitive
            sb.AppendSummary($"Explicitly converts a {Name} to {PrimitiveType}.", tabs, nameof(TypeDefinition));
            sb.AppendLine($"public static explicit operator {PrimitiveType}({Name} value) => value.Value;", tabs);

            sb.AppendLine("}", --tabs);
        }
    }
}
