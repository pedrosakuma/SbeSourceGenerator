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
                sb.AppendTabs(tabs).Append("/// ").Append(descriptionLine.Trim()).AppendLine();
            }
            sb.AppendTabs(tabs).Append("/// (").Append(source).AppendLine(")");
            sb.AppendLine("/// </summary>", tabs);
        }
    }
}
