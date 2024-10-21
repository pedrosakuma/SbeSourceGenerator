using System;
using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    internal static class StringBuilderExtensions
    {
        private static readonly string[] Tabs = [
            new string('\t', 0),
            new string('\t', 1),
            new string('\t', 2),
            new string('\t', 3),
            new string('\t', 4),
        ]; 
        public static void AppendLine(this StringBuilder sb, string value, int tabs = 0)
        {
            sb.Append(Tabs[tabs]);
            sb.AppendLine(value);
        }
    }
}
