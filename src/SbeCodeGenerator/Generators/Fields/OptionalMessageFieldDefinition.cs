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

        public OptionalMessageFieldDefinition(string Name, string Id, string Type, string? PrimitiveType, string Description,
            int? Offset, int Length, string SinceVersion = "", string Deprecated = "")
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
            if (PrimitiveType != null)
            {
                sb.AppendLine($"private {Type} {Name.FirstCharToLower()};", tabs);
                sb.AppendLine($"public {Type}? {Name} => ({PrimitiveType}){Name.FirstCharToLower()} == {TypesCatalog.NullValueByType[PrimitiveType]} ? null : {Name.FirstCharToLower()};", tabs);
                sb.AppendLine($"public void Set{Name}({Type}? value) => {Name.FirstCharToLower()} = value ?? ({Type}){TypesCatalog.NullValueByType[PrimitiveType]};", tabs);
            }
            else
            {
                sb.AppendLine($"public {Type} {Name};", tabs);
            }
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
            sb.AppendLine($"/// ({nameof(OptionalMessageFieldDefinition)})", tabs);
            sb.AppendLine("/// </summary>", tabs);
        }
    }
}