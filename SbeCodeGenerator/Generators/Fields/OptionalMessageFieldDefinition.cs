using System.Text;

namespace SbeSourceGenerator
{
    public class OptionalMessageFieldDefinition : IFileContentGenerator, IBlittableMessageField
    {
        public string Name { get; }
        public string Id { get; }
        public string Type { get; }
        public string? PrimitiveType { get; }
        public string Description { get; }
        public int? Offset { get; set; }
        public int Length { get; }

        public OptionalMessageFieldDefinition(string Name, string Id, string Type, string? PrimitiveType, string Description,
            int? Offset, int Length)
        {
            this.Name = Name;
            this.Id = Id;
            this.Type = Type;
            this.PrimitiveType = PrimitiveType;
            this.Description = Description;
            this.Offset = Offset;
            this.Length = Length;
        }
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendSummary(Description, tabs, nameof(OptionalMessageFieldDefinition));
            sb.AppendLine($"[FieldOffset({Offset})]", tabs);
            sb.AppendLine($"private {Type} {Name.FirstCharToLower()};", tabs);
            if (PrimitiveType != null)
                sb.AppendLine($"public {Type}? {Name} => ({PrimitiveType}){Name.FirstCharToLower()} == {TypesCatalog.NullValueByType[PrimitiveType]} ? null : {Name.FirstCharToLower()};", tabs);
            else
                sb.AppendLine($"public {Type}? {Name} => {Name.FirstCharToLower()};", tabs);
        }
    }
}