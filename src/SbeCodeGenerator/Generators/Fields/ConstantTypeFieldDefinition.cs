using System.Text;

namespace SbeSourceGenerator
{
    public record ConstantTypeFieldDefinition(string Name, string Description, string PrimitiveType, string Value, string ValueRef)
        : IFileContentGenerator, IBlittable
    {
        public int Length => 0;
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendSummary(Description, tabs, nameof(ConstantTypeFieldDefinition));
            if (Value == "")
                sb.AppendLine($"public const {PrimitiveType} {Name} = ({PrimitiveType}){ValueRef};", tabs);
            else
                sb.AppendLine($"public const {PrimitiveType} {Name} = {Value};", tabs);
        }
    }
}
