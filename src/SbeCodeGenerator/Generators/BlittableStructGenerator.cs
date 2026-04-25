using System.Text;

namespace SbeSourceGenerator
{
    internal static class BlittableStructGenerator
    {
        internal static void AppendStructDefinition(this StringBuilder sb, int tabs, string description, string name, string ns)
        {
            sb.AppendUsings(tabs, "System.Runtime.InteropServices");
            sb.AppendTabs(tabs).Append("namespace ").Append(ns).AppendLine(";");
            sb.AppendStructDefinition(tabs, description, name);
        }
        internal static void AppendStructDefinition(this StringBuilder sb, int tabs, string description, string name)
        {
            sb.AppendSummary(description, tabs);
            sb.AppendLine("[StructLayout(LayoutKind.Explicit, Pack = 1)]", tabs);
            sb.AppendTabs(tabs).Append("public partial struct ").Append(name).AppendLine("Data");
        }

    }
}
