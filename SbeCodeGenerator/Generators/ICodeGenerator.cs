using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Xml;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Interface for specialized code generators that generate source files from XML schemas.
    /// Each implementation handles a specific category of code generation (types, messages, utilities, parser).
    /// </summary>
    public interface ICodeGenerator
    {
        /// <summary>
        /// Generates source code from an XML document and schema context.
        /// </summary>
        /// <param name="ns">The namespace for the generated code</param>
        /// <param name="xmlDocument">The XML schema document to process</param>
        /// <param name="context">The schema context for tracking types and metadata</param>
        /// <param name="sourceContext">The source production context for reporting diagnostics</param>
        /// <returns>An enumerable of tuples containing the file name and content</returns>
        IEnumerable<(string name, string content)> Generate(string ns, XmlDocument xmlDocument, SchemaContext context, SourceProductionContext sourceContext);
    }
}
