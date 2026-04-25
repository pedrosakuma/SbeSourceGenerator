using System.Text;

namespace SbeSourceGenerator
{
    public record ConstantTypeFieldDefinition(string Name, string Description, string PrimitiveType, string Value, string ValueRef)
        : IFileContentGenerator, IBlittable
    {
        public int Length => 0;
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendSummary(Description, tabs);
            if (Value == "")
                sb.AppendTabs(tabs).Append("public const ").Append(PrimitiveType).Append(" ").Append(Name).Append(" = (").Append(PrimitiveType).Append(")").Append(ValueRef).AppendLine(";");
            else
                sb.AppendTabs(tabs).Append("public const ").Append(PrimitiveType).Append(" ").Append(Name).Append(" = ").Append(Value).AppendLine(";");
        }
    }
}
