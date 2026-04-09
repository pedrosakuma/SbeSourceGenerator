using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Schema;
using System.Collections.Generic;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Interface for specialized code generators that generate source files from parsed schemas.
    /// Each implementation handles a specific category of code generation (types, messages, utilities, parser).
    /// </summary>
    internal interface ICodeGenerator
    {
        /// <summary>
        /// Generates source code from a parsed schema and schema context.
        /// </summary>
        /// <param name="ns">The namespace for the generated code</param>
        /// <param name="schema">The pre-parsed schema DTOs</param>
        /// <param name="context">The schema context for tracking types and metadata</param>
        /// <param name="sourceContext">The source production context for reporting diagnostics</param>
        /// <returns>An enumerable of tuples containing the file name and content</returns>
        IEnumerable<(string name, string content)> Generate(string ns, ParsedSchema schema, SchemaContext context, SourceProductionContext sourceContext);
    }
}
