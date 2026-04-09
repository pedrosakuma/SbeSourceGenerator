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
            new string('\t', 5),
            new string('\t', 6),
            new string('\t', 7),
            new string('\t', 8),
            new string('\t', 9),
            new string('\t', 10),
        ];
        public static void AppendLine(this StringBuilder sb, string value, int tabs = 0)
        {
            sb.Append(Tabs[tabs]);
            sb.AppendLine(value);
        }
        public static void Append(this StringBuilder sb, string value, int tabs = 0)
        {
            sb.Append(Tabs[tabs]);
            sb.Append(value);
        }
        public static StringBuilder AppendTabs(this StringBuilder sb, int tabs)
        {
            sb.Append(Tabs[tabs]);
            return sb;
        }
    }
}
