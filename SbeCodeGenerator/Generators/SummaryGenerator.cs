using System.Text;

namespace SbeSourceGenerator
{
    public static class SummaryGenerator
    {
        internal static void AppendSummary(this StringBuilder sb, string description, int tabs, string source)
        {
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendLine($"/// {description} ({source})", tabs);
            sb.AppendLine("/// </summary>", tabs);
        }
    }
}
