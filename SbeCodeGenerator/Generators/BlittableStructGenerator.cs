using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace SbeSourceGenerator
{
    internal static  class BlittableStructGenerator
    {
        internal static void AppendStructDefinition(this StringBuilder sb, int tabs, string description, string name, string source, string ns)
        {
            sb.AppendUsings(tabs, "System.Runtime.InteropServices");
            sb.AppendLine($"namespace {ns};", tabs);
            sb.AppendStructDefinition(tabs, description, name, source);
        }
        internal static void AppendStructDefinition(this StringBuilder sb, int tabs, string description, string name, string source)
        {
            sb.AppendSummary(description, tabs, source);
            sb.AppendLine("[StructLayout(LayoutKind.Explicit, Pack = 1)]", tabs);
            sb.AppendLine($"public partial struct {name}Data", tabs);
        }

    }
}
