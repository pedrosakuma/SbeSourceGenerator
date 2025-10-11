using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Generators.Types;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Legacy placeholder for the previous parser generation component.
    /// The parser feature has been removed; this type now returns no generated output.
    /// </summary>
    public sealed class ParserCodeGenerator : ICodeGenerator
    {
        public IEnumerable<(string name, string content)> Generate(string ns, XmlDocument xmlDocument, SchemaContext context, SourceProductionContext sourceContext)
        {
            yield break;
        }
    }
}
