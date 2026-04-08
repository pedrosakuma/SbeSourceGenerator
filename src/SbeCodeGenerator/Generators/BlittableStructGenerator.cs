using System.Text;

namespace SbeSourceGenerator
{
    internal static class BlittableStructGenerator
    {
        internal static void AppendStructDefinition(this StringBuilder sb, int tabs, string description, string name, string source, string ns)
        {
            sb.AppendUsings(tabs, "System.Runtime.InteropServices");
            sb.AppendTabs(tabs).Append("namespace ").Append(ns).AppendLine(";");
            sb.AppendStructDefinition(tabs, description, name, source);
        }
        internal static void AppendStructDefinition(this StringBuilder sb, int tabs, string description, string name, string source)
        {
            sb.AppendSummary(description, tabs, source);
            sb.AppendLine("[StructLayout(LayoutKind.Explicit, Pack = 1)]", tabs);
            sb.AppendTabs(tabs).Append("public partial struct ").Append(name).AppendLine("Data");
        }

    }
}
