using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    public record EnumFlagsDefinition(string Namespace, string Name, string Description, string EncodingType, int Length, List<EnumFieldDefinition> Fields)
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendLine($"namespace {Namespace};", tabs);
            sb.AppendSummary(Description, tabs, nameof(EnumFlagsDefinition));
            sb.AppendLine($"[System.Flags]", tabs);
            sb.AppendLine($"public enum {Name} : {EncodingType}", tabs);
            sb.AppendLine("{", tabs++);
            foreach (var field in Fields)
            {
                if (field.Description != "")
                    sb.AppendSummary(Description, tabs, nameof(EnumFlagsDefinition));
                sb.AppendLine($"{field.Name} = {1 << int.Parse(field.Value)},", tabs);
            }
            sb.AppendLine("}", --tabs);
        }
    }
}