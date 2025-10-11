using System.Text;

namespace SbeSourceGenerator
{
    public record NullableValueFieldDefinition(string Name, string Description, string PrimitiveType, int Length, string? NullValue)
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            string nullValue = NullValue ?? TypesCatalog.NullValueByType[PrimitiveType];
            sb.AppendSummary(Description, tabs, nameof(NullableValueFieldDefinition));
            sb.AppendLine("#pragma warning disable CS0649", tabs);
            sb.AppendLine($"private {PrimitiveType} {Name.FirstCharToLower()};", tabs);
            sb.AppendLine($"public {PrimitiveType}? {Name} => {Name.FirstCharToLower()} == {nullValue} ? null : {Name.FirstCharToLower()};", tabs);
            sb.AppendLine("#pragma warning restore CS0649", tabs);
        }
    }
}
