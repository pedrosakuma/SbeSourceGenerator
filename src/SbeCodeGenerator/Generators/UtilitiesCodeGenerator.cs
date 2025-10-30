using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
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
            // Strip version suffix to use base namespace for utilities
            var baseNamespace = StripSchemaVersion(ns);
            
            // Generate NumberExtensions
            StringBuilder sb = new StringBuilder();
            new NumberExtensions(baseNamespace).AppendFileContent(sb);
            yield return (context.CreateHintName(baseNamespace, "Utilities", "NumberExtensions"), sb.ToString());

            // Generate EndianHelpers
            sb = new StringBuilder();
            new EndianHelpers(baseNamespace).AppendFileContent(sb);
            yield return (context.CreateHintName(baseNamespace, "Utilities", "EndianHelpers"), sb.ToString());

            var runtimeNamespace = baseNamespace;

            if (context.GeneratedRuntimeNamespaces.Add(runtimeNamespace))
            {
                // Generate SpanReader once per runtime namespace
                sb = new StringBuilder();
                new SpanReaderGenerator(runtimeNamespace).AppendFileContent(sb);
                yield return (context.CreateHintName(runtimeNamespace, "Runtime", "SpanReader"), sb.ToString());

                // Generate SpanWriter once per runtime namespace
                sb = new StringBuilder();
                new SpanWriterGenerator(runtimeNamespace).AppendFileContent(sb);
                yield return (context.CreateHintName(runtimeNamespace, "Runtime", "SpanWriter"), sb.ToString());
            }
        }

        private static string StripSchemaVersion(string schemaNamespace)
        {
            if (string.IsNullOrEmpty(schemaNamespace))
                return string.Empty;

            var segments = schemaNamespace.Split('.');
            if (segments.Length == 0)
                return string.Empty;

            var last = segments[segments.Length - 1];
            if (!IsVersionSegment(last))
                return schemaNamespace;

            if (segments.Length == 1)
                return string.Empty;

            return string.Join(".", segments.Take(segments.Length - 1));
        }

        private static bool IsVersionSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment) || segment.Length < 2 || segment[0] != 'V')
                return false;

            for (int i = 1; i < segment.Length; i++)
            {
                char ch = segment[i];
                if (!char.IsDigit(ch) && ch != '_')
                    return false;
            }

            return true;
        }
    }
}
