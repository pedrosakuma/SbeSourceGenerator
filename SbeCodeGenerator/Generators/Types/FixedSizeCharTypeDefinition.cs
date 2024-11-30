using System;
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
                sb.AppendLine("{", tabs);
                sb.AppendLine($"public byte Value;", tabs + 1);
                sb.AppendLine("}", tabs);
            }
            else
            {
                sb.AppendLine($"namespace {Namespace};", tabs);
                sb.AppendSummary(Description, tabs, nameof(FixedSizeCharTypeDefinition));
                sb.AppendLine($"[System.Runtime.CompilerServices.InlineArray({Length})]", tabs);
                sb.AppendLine($"public unsafe struct {Name}", tabs);
                sb.AppendLine("{", tabs);
                sb.AppendLine("private byte value;", tabs + 1);
                sb.AppendLine("public override string ToString()", tabs + 1);
                sb.AppendLine("{", tabs + 1);
                sb.AppendLine("fixed (byte* ptr = &value)", tabs + 2);
                sb.AppendLine("{", tabs + 2);
                sb.AppendLine("return System.Runtime.InteropServices.Marshal.PtrToStringUTF8((nint)ptr)!;", tabs + 3);
                sb.AppendLine("}", tabs + 2);
                sb.AppendLine("}", tabs + 1);
                sb.AppendLine("}", tabs);
            }
        }
    }
}
