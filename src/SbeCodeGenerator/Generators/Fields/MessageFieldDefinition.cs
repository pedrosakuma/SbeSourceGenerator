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
        public string SinceVersion { get; }
        public string Deprecated { get; }

        public MessageFieldDefinition(string Name, string Id, string Type, string Description,
            int? Offset, int Length, string SinceVersion = "", string Deprecated = "")
        {
            this.Name = Name;
            this.Id = Id;
            this.Type = Type;
            this.Description = Description;
            this.Offset = Offset;
            this.Length = Length;
            this.SinceVersion = SinceVersion;
            this.Deprecated = Deprecated;
        }
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            AppendSummaryWithVersion(sb, Description, SinceVersion, tabs);
            if (!string.IsNullOrEmpty(Deprecated))
            {
                var deprecatedSince = string.IsNullOrEmpty(SinceVersion)
                    ? "This field is deprecated"
                    : $"This field is deprecated since version {SinceVersion}";
                sb.AppendLine($"[Obsolete(\"{deprecatedSince}\")]", tabs);
            }
            sb.AppendLine($"[FieldOffset({Offset})]", tabs);
            sb.AppendLine($"public {Type} {Name};", tabs);
        }

        private void AppendSummaryWithVersion(StringBuilder sb, string description, string sinceVersion, int tabs)
        {
            sb.AppendLine("/// <summary>", tabs);
            if (!string.IsNullOrEmpty(description))
            {
                sb.AppendLine($"/// {description}", tabs);
            }
            else
            {
                sb.AppendLine($"/// ", tabs);
            }
            if (!string.IsNullOrEmpty(sinceVersion))
            {
                sb.AppendLine($"/// ", tabs);
                sb.AppendLine($"/// Since version {sinceVersion}", tabs);
            }
            sb.AppendLine($"/// ({nameof(MessageFieldDefinition)})", tabs);
            sb.AppendLine("/// </summary>", tabs);
        }
    }
}