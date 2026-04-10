using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Schema;
using System;
using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Generates utility code (SpanReader, SpanWriter).
    /// </summary>
    internal class UtilitiesCodeGenerator : ICodeGenerator
    {
        public IEnumerable<(string name, string content)> Generate(string ns, ParsedSchema schema, SchemaContext context, SourceProductionContext sourceContext)
        {
            var runtimeNamespace = ns;

            if (context.GeneratedRuntimeNamespaces.Add(runtimeNamespace))
            {
                // Generate SpanReader once per runtime namespace
                StringBuilder sb = new StringBuilder();
                new SpanReaderGenerator(runtimeNamespace).AppendFileContent(sb);
                yield return (context.CreateHintName(runtimeNamespace, "Runtime", "SpanReader"), sb.ToString());

                // Generate SpanWriter once per runtime namespace
                sb = new StringBuilder();
                new SpanWriterGenerator(runtimeNamespace).AppendFileContent(sb);
                yield return (context.CreateHintName(runtimeNamespace, "Runtime", "SpanWriter"), sb.ToString());
            }
        }
    }
}
