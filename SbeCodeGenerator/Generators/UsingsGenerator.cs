using System;
using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    internal static class UsingsGenerator
    {
        internal static void AppendUsings(this StringBuilder sb, int tabs, params string[] namespaces)
        {
            foreach (var ns in namespaces)
                sb.AppendLine($"using {ns};", tabs);
        }
    }
}
