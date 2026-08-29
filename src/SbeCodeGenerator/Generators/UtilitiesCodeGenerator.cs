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
                foreach (var item in GenerateRuntimeSources(
                    runtimeNamespace,
                    typeName => context.CreateHintName(runtimeNamespace, "Runtime", typeName)))
                {
                    yield return item;
                }
            }
        }

        internal static IEnumerable<(string name, string content)> GenerateRuntimeSources(
            string runtimeNamespace,
            Func<string, string> createHintName)
        {
            // Generate SpanReader once per runtime namespace
            StringBuilder sb = new StringBuilder();
            new SpanReaderGenerator(runtimeNamespace).AppendFileContent(sb);
            yield return (createHintName("SpanReader"), sb.ToString());

            // Generate SpanWriter once per runtime namespace
            sb = new StringBuilder();
            new SpanWriterGenerator(runtimeNamespace).AppendFileContent(sb);
            yield return (createHintName("SpanWriter"), sb.ToString());
        }
    }
}
