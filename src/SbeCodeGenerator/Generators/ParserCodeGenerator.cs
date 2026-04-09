using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Schema;
using System.Collections.Generic;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Legacy placeholder for the previous parser generation component.
    /// The parser feature has been removed; this type now returns no generated output.
    /// </summary>
    internal sealed class ParserCodeGenerator : ICodeGenerator
    {
        public IEnumerable<(string name, string content)> Generate(string ns, ParsedSchema schema, SchemaContext context, SourceProductionContext sourceContext)
        {
            yield break;
        }
    }
}
