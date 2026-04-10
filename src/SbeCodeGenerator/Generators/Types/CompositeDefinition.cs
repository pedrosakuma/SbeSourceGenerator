using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SbeSourceGenerator
{
    public record CompositeDefinition(string Namespace, string Name, string Description, string SemanticType,
        List<IFileContentGenerator> Fields, EndianConversion EndianConversion = EndianConversion.None) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            bool blittable = Fields.All(f => f is IBlittable);
            if (EndianConversion != EndianConversion.None)
                sb.AppendUsings(tabs, "System.Runtime.InteropServices", "System.Buffers.Binary");
            else
                sb.AppendUsings(tabs, "System.Runtime.InteropServices");
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendSummary(Description, tabs, nameof(CompositeDefinition));

            if (blittable)
            {
                sb.AppendLine("[StructLayout(LayoutKind.Sequential, Pack = 1)]", tabs);
                sb.AppendTabs(tabs).Append("public partial struct ").Append(Name).AppendLine();
            }
            else
                sb.AppendTabs(tabs).Append("public readonly ref partial struct ").Append(Name).AppendLine();

            sb.AppendLine("{", tabs++);
            if (blittable)
            {
                sb.AppendTabs(tabs).Append("public const int MESSAGE_SIZE = ").Append(Fields.SumFieldLength()).AppendLine(";");
                sb.AppendTabs(tabs).Append("public static bool TryParse(ReadOnlySpan<byte> buffer, out ").Append(Name).AppendLine(" value, out ReadOnlySpan<byte> remaining)");
                sb.AppendLine("{", tabs++);
                sb.AppendLine("if (buffer.Length < MESSAGE_SIZE)", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("value = default;", tabs);
                sb.AppendLine("remaining = default;", tabs);
                sb.AppendLine("return false;", tabs);
                sb.AppendLine("}", --tabs);
                sb.AppendTabs(tabs).Append("value = MemoryMarshal.AsRef<").Append(Name).AppendLine(">(buffer);");
                sb.AppendLine("remaining = buffer.Slice(MESSAGE_SIZE);", tabs);
                sb.AppendLine("return true;", tabs);
                sb.AppendLine("}", --tabs);
            }

            // Append fields with readonly modifier for ref structs
            foreach (var field in Fields)
            {
                if (!blittable)
                {
                    // For ref structs, manually add readonly fields
                    if (field is ValueFieldDefinition vfd)
                    {
                        sb.AppendSummary(vfd.Description, tabs, nameof(ValueFieldDefinition));
                        sb.AppendTabs(tabs).Append("public readonly ").Append(vfd.PrimitiveType).Append(" ").Append(vfd.Name).AppendLine(";");
                    }
                    else if (field is ArrayFieldDefinition afd)
                    {
                        sb.AppendSummary(afd.Description, tabs, nameof(ArrayFieldDefinition));
                        sb.AppendTabs(tabs).Append("public readonly ReadOnlySpan<").Append(afd.PrimitiveType).Append("> ").Append(afd.Name).AppendLine(";");
                    }
                    else
                    {
                        field.AppendFileContent(sb, tabs);
                    }
                }
                else
                {
                    field.AppendFileContent(sb, tabs);
                }
            }

            if (!blittable)
            {
                var valueField = Fields.OfType<ValueFieldDefinition>().FirstOrDefault();
                var arrayField = Fields.OfType<ArrayFieldDefinition>().FirstOrDefault();

                if (valueField != null && arrayField != null)
                {
                    sb.AppendSummary($"Initializes a new instance of {Name} with the specified values.", tabs, nameof(CompositeDefinition));
                    sb.AppendTabs(tabs).Append("public ").Append(Name).Append("(").Append(valueField.PrimitiveType).Append(" ").Append(valueField.Name.FirstCharToLower()).Append(", ReadOnlySpan<").Append(arrayField.PrimitiveType).Append("> ").Append(arrayField.Name.FirstCharToLower()).AppendLine(")");
                    sb.AppendLine("{", tabs++);
                    sb.AppendTabs(tabs).Append(valueField.Name).Append(" = ").Append(valueField.Name.FirstCharToLower()).AppendLine(";");
                    sb.AppendTabs(tabs).Append(arrayField.Name).Append(" = ").Append(arrayField.Name.FirstCharToLower()).AppendLine(";");
                    sb.AppendLine("}", --tabs);
                    sb.AppendLine("", tabs);

                    sb.AppendSummary("Create instance from buffer", tabs, nameof(CompositeDefinition));
                    sb.AppendTabs(tabs).Append("public static ").Append(Name).Append(" Create(ReadOnlySpan<byte> buffer) => new ").Append(Name).AppendLine("(MemoryMarshal.AsRef<byte>(buffer), buffer.Slice(1));");
                }

                sb.AppendSummary("Callback delegate used on ConsumeVariableLengthSegments", tabs, nameof(CompositeDefinition));
                sb.AppendTabs(tabs).Append("public delegate void Callback(").Append(Name).AppendLine(" data);");
            }
            else
            {
                FileContentGeneratorExtensions.AppendToString(sb, tabs, Name, Fields);
            }
            sb.AppendLine("}", --tabs);
        }
    }
}
