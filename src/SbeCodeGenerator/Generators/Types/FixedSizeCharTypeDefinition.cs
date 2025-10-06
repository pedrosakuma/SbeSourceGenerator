using System.Text;

namespace SbeSourceGenerator
{
    public record FixedSizeCharTypeDefinition(string Namespace, string Name, string Description,
        int Length) : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            if (Length == 0)
            {
                sb.AppendLine($"namespace {Namespace};", tabs);
                sb.AppendSummary(Description, tabs, nameof(FixedSizeCharTypeDefinition));
                sb.AppendLine($"public struct {Name}", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine($"public byte Value;", tabs);
                sb.AppendLine("}", --tabs);
            }
            else
            {
                sb.AppendLine($"namespace {Namespace};", tabs);
                sb.AppendSummary(Description, tabs, nameof(FixedSizeCharTypeDefinition));
                sb.AppendLine($"[System.Runtime.CompilerServices.InlineArray({Length})]", tabs);
                sb.AppendLine($"public unsafe struct {Name}", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("private byte value;", tabs);
                sb.AppendLine("public override string ToString()", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("fixed (byte* ptr = &value)", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine($"var span = new Span<byte>(ptr, {Length});", tabs);
                sb.AppendLine($"var index = span.IndexOf((byte)0);", tabs);
                sb.AppendLine($"return System.Runtime.InteropServices.Marshal.PtrToStringAnsi((nint)ptr, index == -1 ? {Length} : index)!;", tabs);
                sb.AppendLine("}", --tabs);
                sb.AppendLine("}", --tabs);
                sb.AppendLine("}", --tabs);
            }
        }
    }
}
