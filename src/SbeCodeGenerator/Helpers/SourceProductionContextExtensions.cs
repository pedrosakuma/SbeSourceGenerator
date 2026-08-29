using Microsoft.CodeAnalysis;

namespace SbeSourceGenerator.Helpers
{
    /// <summary>
    /// Helpers for safely reporting diagnostics from optional/default source-production contexts.
    /// </summary>
    internal static class SourceProductionContextExtensions
    {
        public static bool CanReportDiagnostics(this SourceProductionContext context)
        {
            return !context.Equals(default(SourceProductionContext));
        }
    }
}
