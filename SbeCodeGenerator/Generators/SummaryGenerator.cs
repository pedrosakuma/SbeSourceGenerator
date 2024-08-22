namespace SbeSourceGenerator
{
    public static class SummaryGenerator
    {
        internal static string Generate(string description, string source)
        {
            return Generate(description, 0, source);
        }
        internal static string Generate(string description, int tabs, string source)
        {
            return $$"""
            {{new string('\t', tabs)}}/// <summary>
            {{new string('\t', tabs)}}/// {{description}} ({{source}})
            {{new string('\t', tabs)}}/// </summary>        
            """;
        }
    }
}
