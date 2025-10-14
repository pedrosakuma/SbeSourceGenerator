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
                sb.AppendLine($"public readonly ref partial struct {Name}", tabs);

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

            // Append fields with readonly modifier for ref structs
            foreach (var field in Fields)
            {
                if (!blittable)
                {
                    // For ref structs, manually add readonly fields
                    if (field is ValueFieldDefinition vfd)
                    {
                        sb.AppendSummary(vfd.Description, tabs, nameof(ValueFieldDefinition));
                        sb.AppendLine($"public readonly {vfd.PrimitiveType} {vfd.Name};", tabs);
                    }
                    else if (field is ArrayFieldDefinition afd)
                    {
                        sb.AppendSummary(afd.Description, tabs, nameof(ArrayFieldDefinition));
                        sb.AppendLine($"public readonly ReadOnlySpan<{afd.PrimitiveType}> {afd.Name};", tabs);
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
                string varDataType = Fields.Where(f => f is ArrayFieldDefinition)
                    .Select(f => ((ArrayFieldDefinition)f).PrimitiveType)
                    .First();

                // Generate constructor for readonly ref struct
                var valueField = Fields.Where(f => f is ValueFieldDefinition).Cast<ValueFieldDefinition>().First();
                var arrayField = Fields.Where(f => f is ArrayFieldDefinition).Cast<ArrayFieldDefinition>().First();
                
                sb.AppendSummary($"Initializes a new instance of {Name} with the specified values.", tabs, nameof(CompositeDefinition));
                sb.AppendLine($"public {Name}({valueField.PrimitiveType} {valueField.Name.FirstCharToLower()}, ReadOnlySpan<{arrayField.PrimitiveType}> {arrayField.Name.FirstCharToLower()})", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine($"{valueField.Name} = {valueField.Name.FirstCharToLower()};", tabs);
                sb.AppendLine($"{arrayField.Name} = {arrayField.Name.FirstCharToLower()};", tabs);
                sb.AppendLine("}", --tabs);
                sb.AppendLine("", tabs);

                sb.AppendSummary("Create instance from buffer", tabs, nameof(CompositeDefinition));
                sb.AppendLine($"public static {Name} Create(ReadOnlySpan<byte> buffer) => new {Name}(MemoryMarshal.AsRef<byte>(buffer), buffer.Slice(1));", tabs);

                sb.AppendSummary("Callback delegate used on ConsumeVariableLengthSegments", tabs, nameof(CompositeDefinition));
                sb.AppendLine($"public delegate void Callback({Name} data);", tabs);
            }
            sb.AppendLine("}", --tabs);
        }
    }
}
