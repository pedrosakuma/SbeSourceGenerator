using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Generators.Types;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Generates code for SBE message parsers.
    /// Wraps the existing ParserGenerator to fit the ICodeGenerator interface.
    /// </summary>
    public class ParserCodeGenerator : ICodeGenerator
    {
        public IEnumerable<(string name, string content)> Generate(string ns, XmlDocument xmlDocument, SchemaContext context, SourceProductionContext sourceContext)
        {
            // This method is not used directly by ParserCodeGenerator since it needs messages list
            // Instead, use GenerateParser method which is called by MessagesCodeGenerator
            yield break;
        }

        /// <summary>
        /// Generates parser code for the given messages.
        /// </summary>
        public IEnumerable<(string name, string content)> GenerateParser(string ns, List<MessageDefinition> messages, SchemaContext context)
        {
            StringBuilder sb = new StringBuilder();
            new ParserGenerator(ns, "", messages, context).AppendFileContent(sb);
            yield return ($"{ns}\\MessageParser", sb.ToString());
        }
    }
}
