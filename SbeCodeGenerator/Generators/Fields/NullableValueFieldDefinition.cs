using System.Text;

namespace SbeSourceGenerator
{
    public record NullableValueFieldDefinition(string Name, string Description, string PrimitiveType, int Length)
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendSummary(Description, tabs, nameof(NullableValueFieldDefinition));
            sb.AppendLine($"private {PrimitiveType} {Name.FirstCharToLower()};", tabs);
            sb.AppendLine($"public {PrimitiveType}? {Name} => {Name.FirstCharToLower()} == {TypesCatalog.NullValueByType[PrimitiveType]} ? null : {Name.FirstCharToLower()};", tabs);
        }
    }
}
