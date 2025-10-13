using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SbeSourceGenerator
{
    public record CompositeDefinition(string Namespace, string Name, string Description, string SemanticType, List<IFileContentGenerator> Fields) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            bool blittable = Fields.All(f => f is IBlittable);
            sb.AppendUsings(tabs, "System.Runtime.InteropServices");
            sb.AppendLine($"namespace {Namespace};", tabs);
            sb.AppendSummary(Description, tabs, nameof(CompositeDefinition));

            if (blittable)
            {
                sb.AppendLine("[StructLayout(LayoutKind.Sequential, Pack = 1)]", tabs);
                sb.AppendLine($"public partial struct {Name}", tabs);
            }
            else
                sb.AppendLine($"public ref struct {Name}", tabs);

            sb.AppendLine("{", tabs++);
            if (blittable)
            {
                sb.AppendLine($"public const int MESSAGE_SIZE = {Fields.SumFieldLength()};", tabs);
                sb.AppendLine($"public static bool TryParse(ReadOnlySpan<byte> buffer, out {Name} value, out ReadOnlySpan<byte> remaining)", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("if (buffer.Length < MESSAGE_SIZE)", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("value = default;", tabs);
                sb.AppendLine("remaining = default;", tabs);
                sb.AppendLine("return false;", tabs);
                sb.AppendLine("}", --tabs);
                sb.AppendLine($"value = MemoryMarshal.AsRef<{Name}>(buffer);", tabs);
                sb.AppendLine("remaining = buffer.Slice(MESSAGE_SIZE);", tabs);
                sb.AppendLine("return true;", tabs);
                sb.AppendLine("}", --tabs);
            }

            foreach (var field in Fields)
                field.AppendFileContent(sb, tabs);
            if (!blittable)
            {
                string varDataType = Fields.Where(f => f is ArrayFieldDefinition)
                    .Select(f => ((ArrayFieldDefinition)f).PrimitiveType)
                    .First();

                sb.AppendSummary("Create instance from buffer", tabs, nameof(CompositeDefinition));
                sb.AppendLine($"public static {Name} Create(ReadOnlySpan<byte> buffer) => new {Name} {{ Length = MemoryMarshal.AsRef<byte>(buffer), VarData =  buffer.Slice(1) }};", tabs);

                sb.AppendSummary("Callback delegate used on ConsumeVariableLengthSegments", tabs, nameof(CompositeDefinition));
                sb.AppendLine($"public delegate void Callback({Name} data);", tabs);
            }
            sb.AppendLine("}", --tabs);
        }
    }
}
