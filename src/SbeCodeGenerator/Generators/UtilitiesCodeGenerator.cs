using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Generates utility code (e.g., NumberExtensions, EndianHelpers).
    /// </summary>
    public class UtilitiesCodeGenerator : ICodeGenerator
    {
        public IEnumerable<(string name, string content)> Generate(string ns, XmlDocument xmlDocument, SchemaContext context, SourceProductionContext sourceContext)
        {
            // Generate NumberExtensions
            StringBuilder sb = new StringBuilder();
            new NumberExtensions(ns).AppendFileContent(sb);
            yield return ($"{ns}\\Utilities\\NumberExtensions", sb.ToString());

            // Generate EndianHelpers
            sb = new StringBuilder();
            new EndianHelpers(ns).AppendFileContent(sb);
            yield return ($"{ns}\\Utilities\\EndianHelpers", sb.ToString());

            // Generate SpanReader
            sb = new StringBuilder();
            new SpanReaderGenerator(ns).AppendFileContent(sb);
            yield return ($"{ns}\\Runtime\\SpanReader", sb.ToString());

            // Generate SpanWriter
            sb = new StringBuilder();
            new SpanWriterGenerator(ns).AppendFileContent(sb);
            yield return ($"{ns}\\Runtime\\SpanWriter", sb.ToString());
        }
    }
}
