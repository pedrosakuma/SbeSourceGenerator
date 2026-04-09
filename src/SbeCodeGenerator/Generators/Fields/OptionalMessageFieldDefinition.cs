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
        public string SinceVersion { get; }
        public string Deprecated { get; }
        public string? NullValue { get; }

        public OptionalMessageFieldDefinition(string Name, string Id, string Type, string? PrimitiveType, string Description,
            int? Offset, int Length, string SinceVersion = "", string Deprecated = "", string? NullValue = null)
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
            this.NullValue = NullValue;
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
            sb.AppendTabs(tabs).Append("[FieldOffset(").Append(Offset).AppendLine(")]");
            if (PrimitiveType != null)
            {
                var nullValue = NullValue ?? TypesCatalog.GetNullValue(PrimitiveType);
                sb.AppendTabs(tabs).Append("private ").Append(Type).Append(" ").Append(Name.FirstCharToLower()).AppendLine(";");
                if (NullValue == null && TypesCatalog.IsFloatingPoint(PrimitiveType))
                {
                    sb.AppendTabs(tabs).Append("public ").Append(Type).Append("? ").Append(Name).Append(" => ").Append(PrimitiveType).Append(".IsNaN((").Append(PrimitiveType).Append(")").Append(Name.FirstCharToLower()).Append(") ? null : ").Append(Name.FirstCharToLower()).AppendLine(";");
                    sb.AppendTabs(tabs).Append("public void Set").Append(Name).Append("(").Append(Type).Append("? value) => ").Append(Name.FirstCharToLower()).Append(" = value ?? (").Append(Type).Append(")").Append(nullValue).AppendLine(";");
                }
                else
                {
                    sb.AppendTabs(tabs).Append("public ").Append(Type).Append("? ").Append(Name).Append(" => (").Append(PrimitiveType).Append(")").Append(Name.FirstCharToLower()).Append(" == ").Append(nullValue).Append(" ? null : ").Append(Name.FirstCharToLower()).AppendLine(";");
                    sb.AppendTabs(tabs).Append("public void Set").Append(Name).Append("(").Append(Type).Append("? value) => ").Append(Name.FirstCharToLower()).Append(" = value ?? (").Append(Type).Append(")").Append(nullValue).AppendLine(";");
                }
            }
            else
            {
                sb.AppendTabs(tabs).Append("public ").Append(Type).Append(" ").Append(Name).AppendLine(";");
            }
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
            sb.AppendTabs(tabs).Append("/// (").Append(nameof(OptionalMessageFieldDefinition)).AppendLine(")");
            sb.AppendLine("/// </summary>", tabs);
        }
    }
}
