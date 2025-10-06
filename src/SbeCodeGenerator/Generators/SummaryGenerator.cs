using System.Text;

namespace SbeSourceGenerator
{
    public static class SummaryGenerator
    {
        internal static void AppendSummary(this StringBuilder sb, string description, int tabs, string source)
        {
            sb.AppendLine("/// <summary>", tabs);
            foreach (var descriptionLine in description.Split('\r', '\n'))
            {
                sb.AppendLine($"/// {descriptionLine.Trim()}", tabs);
            }
            sb.AppendLine($"/// ({source})", tabs);
            sb.AppendLine("/// </summary>", tabs);
        }
    }
}
