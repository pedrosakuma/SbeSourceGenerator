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
        public EndianConversion EndianConversion { get; }

        public OptionalMessageFieldDefinition(string Name, string Id, string Type, string? PrimitiveType, string Description,
            int? Offset, int Length, string SinceVersion = "", string Deprecated = "", string? NullValue = null,
            EndianConversion EndianConversion = EndianConversion.None)
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
            this.EndianConversion = EndianConversion;
        }
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            bool isDeprecated = !string.IsNullOrEmpty(Deprecated);
            AppendSummaryWithVersion(sb, Description, SinceVersion, tabs);
            if (isDeprecated)
            {
                var deprecatedSince = string.IsNullOrEmpty(SinceVersion)
                    ? "This field is deprecated"
                    : $"This field is deprecated since version {SinceVersion}";
                sb.AppendTabs(tabs).Append("[Obsolete(\"").Append(deprecatedSince).AppendLine("\")]");
            }
            if (PrimitiveType != null)
            {
                var nullValue = NullValue ?? TypesCatalog.GetNullValue(PrimitiveType);
                string fieldName = Name.FirstCharToLower();
                string constantName = Name + "NullValue";

                NullableValueFieldDefinition.AppendNullConstant(sb, tabs, PrimitiveType, constantName, nullValue);
                sb.AppendTabs(tabs).Append("[FieldOffset(").Append(Offset).AppendLine(")]");
                sb.AppendTabs(tabs).Append("private ").Append(Type).Append(" ").Append(fieldName).AppendLine(";");

                if (isDeprecated)
                    sb.AppendLine("#pragma warning disable CS0618", tabs);

                // Getter: convert from wire to host, then check null sentinel
                string getExpr = EndianFieldHelper.GetterExpression(PrimitiveType, fieldName, EndianConversion);
                // Setter: convert from host to wire when storing
                string setNullExpr = EndianFieldHelper.SetterExpression(PrimitiveType, "((" + Type + ")" + nullValue + ")", EndianConversion);

                if (NullValue == null && TypesCatalog.IsFloatingPoint(PrimitiveType))
                {
                    sb.AppendTabs(tabs).Append("public readonly ").Append(Type).Append("? ").Append(Name).Append(" => ").Append(PrimitiveType).Append(".IsNaN((").Append(PrimitiveType).Append(")").Append(getExpr).Append(") ? null : ").Append(getExpr).AppendLine(";");
                }
                else
                {
                    sb.AppendTabs(tabs).Append("public readonly ").Append(Type).Append("? ").Append(Name).Append(" => (").Append(PrimitiveType).Append(")").Append(getExpr).Append(" == ").Append(constantName).Append(" ? null : ").Append(getExpr).AppendLine(";");
                }
                sb.AppendTabs(tabs).Append("public void Set").Append(Name).Append("(").Append(Type).Append("? value) => ").Append(fieldName).Append(" = value.HasValue ? (").Append(Type).Append(")").Append(EndianFieldHelper.SetterExpression(PrimitiveType, "value.Value", EndianConversion)).Append(" : ").Append(setNullExpr).AppendLine(";");

                if (isDeprecated)
                    sb.AppendLine("#pragma warning restore CS0618", tabs);
            }
            else
            {
                sb.AppendTabs(tabs).Append("[FieldOffset(").Append(Offset).AppendLine(")]");
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
