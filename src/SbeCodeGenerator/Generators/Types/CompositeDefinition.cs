using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SbeSourceGenerator
{
    public record CompositeDefinition(string Namespace, string Name, string Description, string SemanticType,
        List<IFileContentGenerator> Fields, EndianConversion EndianConversion = EndianConversion.None,
        bool IsMessageHeader = false) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            bool blittable = Fields.All(f => f is IBlittable);
            if (EndianConversion != EndianConversion.None)
                sb.AppendUsings(tabs, "System.Runtime.InteropServices", "System.Buffers.Binary");
            else
                sb.AppendUsings(tabs, "System.Runtime.InteropServices");
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendSummary(Description, tabs);

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
                        sb.AppendSummary(vfd.Description, tabs);
                        sb.AppendTabs(tabs).Append("public readonly ").Append(vfd.PrimitiveType).Append(" ").Append(vfd.Name).AppendLine(";");
                    }
                    else if (field is ArrayFieldDefinition afd)
                    {
                        sb.AppendSummary(afd.Description, tabs);
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
                    int lengthSize = TypesCatalog.GetPrimitiveLength(valueField.PrimitiveType);

                    sb.AppendSummary($"Initializes a new instance of {Name} with the specified values.", tabs);
                    sb.AppendTabs(tabs).Append("public ").Append(Name).Append("(").Append(valueField.PrimitiveType).Append(" ").Append(valueField.Name.FirstCharToLower()).Append(", ReadOnlySpan<").Append(arrayField.PrimitiveType).Append("> ").Append(arrayField.Name.FirstCharToLower()).AppendLine(")");
                    sb.AppendLine("{", tabs++);
                    sb.AppendTabs(tabs).Append(valueField.Name).Append(" = ").Append(valueField.Name.FirstCharToLower()).AppendLine(";");
                    sb.AppendTabs(tabs).Append(arrayField.Name).Append(" = ").Append(arrayField.Name.FirstCharToLower()).AppendLine(";");
                    sb.AppendLine("}", --tabs);
                    sb.AppendLine("", tabs);

                    sb.AppendSummary("Create instance from buffer, reading the length prefix and slicing the data.", tabs);
                    sb.AppendTabs(tabs).Append("public static ").Append(Name).Append(" Create(ReadOnlySpan<byte> buffer)");
                    sb.AppendLine();
                    sb.AppendLine("{", tabs++);
                    sb.AppendTabs(tabs).Append("var length = MemoryMarshal.AsRef<").Append(valueField.PrimitiveType).AppendLine(">(buffer);");
                    sb.AppendTabs(tabs).Append("return new ").Append(Name).Append("(length, buffer.Slice(").Append(lengthSize.ToString()).Append(", (int)length));").AppendLine();
                    sb.AppendLine("}", --tabs);

                    sb.AppendSummary("Total wire size of this variable-length segment (length prefix + data).", tabs);
                    sb.AppendTabs(tabs).Append("public int TotalLength => ").Append(lengthSize.ToString()).Append(" + (int)").Append(valueField.Name).AppendLine(";");
                }

                sb.AppendSummary("Callback delegate used on ConsumeVariableLengthSegments", tabs);
                sb.AppendTabs(tabs).Append("public delegate void Callback(").Append(Name).AppendLine(" data);");
            }
            else
            {
                FileContentGeneratorExtensions.AppendToString(sb, tabs, Name, Fields);
                if (IsMessageHeader)
                {
                    AppendHeaderHelpers(sb, tabs);
                }
            }
            sb.AppendLine("}", --tabs);
        }

        private void AppendHeaderHelpers(StringBuilder sb, int tabs)
        {
            // Issue #156: static convenience helpers so consumers don't hardcode
            // header offsets when they need to peek at the templateId before dispatching.
            // Field accessors handle endian conversion, so we delegate to them.
            var valueFields = Fields.OfType<ValueFieldDefinition>().ToList();
            bool hasField(string n) => valueFields.Any(f => string.Equals(f.Name, n, System.StringComparison.OrdinalIgnoreCase));
            if (!(hasField("BlockLength") && hasField("TemplateId") && hasField("SchemaId") && hasField("Version")))
                return;

            sb.AppendLine("", tabs);
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendLine("/// Reads the templateId from a buffer that starts with this header. Returns false if the buffer is too small.", tabs);
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendLine("[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]", tabs);
            sb.AppendLine("public static bool TryReadTemplateId(ReadOnlySpan<byte> buffer, out ushort templateId)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("if (buffer.Length < MESSAGE_SIZE) { templateId = 0; return false; }", tabs);
            sb.AppendTabs(tabs).Append("templateId = MemoryMarshal.AsRef<").Append(Name).AppendLine(">(buffer).TemplateId;");
            sb.AppendLine("return true;", tabs);
            sb.AppendLine("}", --tabs);

            sb.AppendLine("", tabs);
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendLine("/// Reads the four standard header fields (blockLength, templateId, schemaId, version)", tabs);
            sb.AppendLine("/// in a single call. Returns false if the buffer is smaller than the header.", tabs);
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendLine("[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]", tabs);
            sb.AppendLine("public static bool TryReadHeader(ReadOnlySpan<byte> buffer, out ushort blockLength, out ushort templateId, out ushort schemaId, out ushort version)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("if (buffer.Length < MESSAGE_SIZE)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("blockLength = 0; templateId = 0; schemaId = 0; version = 0;", tabs);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendTabs(tabs).Append("ref readonly var h = ref MemoryMarshal.AsRef<").Append(Name).AppendLine(">(buffer);");
            sb.AppendLine("blockLength = h.BlockLength;", tabs);
            sb.AppendLine("templateId = h.TemplateId;", tabs);
            sb.AppendLine("schemaId = h.SchemaId;", tabs);
            sb.AppendLine("version = h.Version;", tabs);
            sb.AppendLine("return true;", tabs);
            sb.AppendLine("}", --tabs);
        }
    }
}
