using System.Text;

namespace SbeSourceGenerator
{
    public class MessageFieldDefinition : IFileContentGenerator, IBlittableMessageField
    {
        public string Name { get; }
        public string Id { get; }
        public string Type { get; }
        public string Description { get; }
        public int? Offset { get; set; }
        public int Length { get; }

        public MessageFieldDefinition(string Name, string Id, string Type, string Description,
            int? Offset, int Length)
        {
            this.Name = Name;
            this.Id = Id;
            this.Type = Type;
            this.Description = Description;
            this.Offset = Offset;
            this.Length = Length;
        }
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendSummary(Description, tabs, nameof(MessageFieldDefinition));
            sb.AppendLine($"[FieldOffset({Offset})]", tabs);
            sb.AppendLine($"public {Type} {Name};", tabs);
        }
    }
}