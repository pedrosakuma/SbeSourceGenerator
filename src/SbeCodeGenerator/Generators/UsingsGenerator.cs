using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    internal static class UsingsGenerator
    {
        internal static void AppendUsings(this StringBuilder sb, int tabs, params string[] namespaces)
        {
            if (namespaces is null || namespaces.Length == 0)
                return;

            HashSet<string> emitted = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var ns in namespaces)
            {
                if (string.IsNullOrWhiteSpace(ns))
                    continue;

                if (!emitted.Add(ns))
                    continue;

                sb.AppendTabs(tabs).Append("using ").Append(ns).AppendLine(";");
            }
        }
    }
}
