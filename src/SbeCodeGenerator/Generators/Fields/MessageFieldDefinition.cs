using System.Text;

namespace SbeSourceGenerator
{
    public class MessageFieldDefinition : IFileContentGenerator, IBlittableMessageField
    {
        public string Name { get; }
        public string Id { get; }
        public string Type { get; }
        public string? PrimitiveType { get; }
        public string Description { get; }
        public int? Offset { get; set; }
        public int Length { get; }
        public string SinceVersion { get; }
        public string Deprecated { get; }
        public EndianConversion EndianConversion { get; }

        public MessageFieldDefinition(string Name, string Id, string Type, string Description,
            int? Offset, int Length, string SinceVersion = "", string Deprecated = "",
            EndianConversion EndianConversion = EndianConversion.None, string? PrimitiveType = null)
        {
            this.Name = Name;
            this.Id = Id;
            this.Type = Type;
            this.PrimitiveType = PrimitiveType;
            this.Description = Description;
            this.Offset = Offset;
            this.Length = Length;
            this.SinceVersion = SinceVersion;
            this.Deprecated = Deprecated;
            this.EndianConversion = EndianConversion;
        }
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            AppendSummaryWithVersion(sb, Description, SinceVersion, tabs);
            if (!string.IsNullOrEmpty(Deprecated))
            {
                var deprecatedSince = string.IsNullOrEmpty(SinceVersion)
                    ? "This field is deprecated"
                    : $"This field is deprecated since version {SinceVersion}";
                sb.AppendTabs(tabs).Append("[Obsolete(\"").Append(deprecatedSince).AppendLine("\")]");
            }
            string effectiveType = PrimitiveType ?? Type;
            EndianFieldHelper.AppendMessageField(sb, tabs, Type, effectiveType, Name, Offset, EndianConversion);
        }

        private void AppendSummaryWithVersion(StringBuilder sb, string description, string sinceVersion, int tabs)
        {
            sb.AppendLine("/// <summary>", tabs);
            if (!string.IsNullOrEmpty(description))
            {
                sb.AppendTabs(tabs).Append("/// ").Append(description).AppendLine();
            }
            else
            {
                sb.AppendTabs(tabs).AppendLine("/// ");
            }
            if (!string.IsNullOrEmpty(sinceVersion))
            {
                sb.AppendTabs(tabs).AppendLine("/// ");
                sb.AppendTabs(tabs).Append("/// Since version ").Append(sinceVersion).AppendLine();
            }
            sb.AppendTabs(tabs).Append("/// (").Append(nameof(MessageFieldDefinition)).AppendLine(")");
            sb.AppendLine("/// </summary>", tabs);
        }
    }
}
