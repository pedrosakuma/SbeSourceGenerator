using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Diagnostics;
using SbeSourceGenerator.Generators;
using SbeSourceGenerator.Schema;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SbeSourceGenerator
{
    [Generator]
    public class SBESourceGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// Initializes the source generator by configuring the incremental pipeline.
        /// </summary>
        public void Initialize(IncrementalGeneratorInitializationContext initContext)
        {
            // Stage 1: Collect XML schema files from additional files
            IncrementalValuesProvider<AdditionalText> xmlSchemaFiles = CollectXmlSchemaFiles(initContext);

            // Stage 2 & 3: Register source generation with diagnostic support
            RegisterSourceGeneration(initContext, xmlSchemaFiles.Collect());
        }

        /// <summary>
        /// Collects SBE XML schema files from the project's additional files.
        /// </summary>
        private static IncrementalValuesProvider<AdditionalText> CollectXmlSchemaFiles(IncrementalGeneratorInitializationContext initContext)
        {
            return initContext.AdditionalTextsProvider.Where(file => file.Path.EndsWith(".xml"));
        }

        /// <summary>
        /// Registers source output for each XML schema with diagnostic reporting.
        /// </summary>
        private static void RegisterSourceGeneration(IncrementalGeneratorInitializationContext initContext, IncrementalValueProvider<ImmutableArray<AdditionalText>> xmlFiles)
        {
            initContext.RegisterSourceOutput(xmlFiles, (sourceContext, text) =>
            {
                if (text.IsDefaultOrEmpty)
                    return;

                var emittedRuntimeNamespaces = new HashSet<string>(StringComparer.Ordinal);

                foreach (var additionalText in text)
                {
                    try
                    {
                        string path = additionalText.Path;
                        var xmlContent = additionalText.GetText(sourceContext.CancellationToken)?.ToString();
                        if (string.IsNullOrEmpty(xmlContent))
                            continue;

                        var schema = SchemaReader.Parse(xmlContent!, sourceContext);

                        string ns = GetNamespaceFromSchema(schema, path);
                        string schemaKey = CreateSchemaKey(path);

                        // Create a per-schema context to hold mutable state (sharing runtime tracking)
                        var context = new SchemaContext(schemaKey, emittedRuntimeNamespaces);

                        if (!string.IsNullOrEmpty(schema.ByteOrder))
                        {
                            context.ByteOrder = schema.ByteOrder;
                            if (!schema.ByteOrder.Equals("littleEndian", StringComparison.OrdinalIgnoreCase)
                                && !sourceContext.CancellationToken.IsCancellationRequested)
                            {
                                sourceContext.ReportDiagnostic(Diagnostic.Create(
                                    SbeDiagnostics.NonNativeByteOrder,
                                    Location.None,
                                    path,
                                    schema.ByteOrder));
                            }
                        }

                        if (!string.IsNullOrEmpty(schema.HeaderType))
                            context.HeaderType = schema.HeaderType;

                        // Use specialized generators to handle different categories
                        var typesGenerator = new TypesCodeGenerator();
                        var messagesGenerator = new MessagesCodeGenerator();
                        var utilitiesGenerator = new UtilitiesCodeGenerator();
                        var validationGenerator = new ValidationGenerator();

                        var generators = new (string phase, ICodeGenerator gen)[]
                        {
                            ("types", typesGenerator),
                            ("messages", messagesGenerator),
                            ("utilities", utilitiesGenerator),
                            ("validation", validationGenerator)
                        };

                        foreach (var (phase, gen) in generators)
                        {
                            try
                            {
                                foreach (var item in gen.Generate(ns, schema, context, sourceContext))
                                    sourceContext.AddSource(item.name, item.content);
                            }
                            catch (Exception genEx) when (!sourceContext.CancellationToken.IsCancellationRequested)
                            {
                                sourceContext.ReportDiagnostic(Diagnostic.Create(
                                    SbeDiagnostics.MalformedSchema,
                                    Location.None,
                                    path,
                                    $"[{phase}] {genEx.Message}"));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!sourceContext.CancellationToken.IsCancellationRequested)
                        {
                            sourceContext.ReportDiagnostic(Diagnostic.Create(
                                SbeDiagnostics.MalformedSchema,
                                Location.None,
                                additionalText.Path,
                                ex.Message));
                        }
                    }
                }
            });
        }

        private static string CreateSchemaKey(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "schema";
            }

            var sanitized = new StringBuilder(fileName.Length);
            foreach (char ch in fileName)
            {
                sanitized.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            }

            string hash;
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(path);
                var digest = sha.ComputeHash(bytes);
                hash = BitConverter.ToString(digest, 0, 4).Replace("-", string.Empty);
            }

            return string.Concat(sanitized.ToString(), "_", hash);
        }


        private static string GetNamespaceFromSchema(ParsedSchema schema, string path)
        {
            var baseNamespaceFromPath = GetNamespaceFromPath(path);

            string package = schema.Package;
            string version = schema.Version;

            string packageNamespace = NormalizePackage(package);

            string baseNamespace;
            if (string.IsNullOrWhiteSpace(packageNamespace))
            {
                baseNamespace = baseNamespaceFromPath;
            }
            else if (string.IsNullOrWhiteSpace(baseNamespaceFromPath))
            {
                baseNamespace = packageNamespace;
            }
            else
            {
                int packageSegments = packageNamespace.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Length;
                int pathSegments = baseNamespaceFromPath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Length;
                baseNamespace = pathSegments > packageSegments ? baseNamespaceFromPath : packageNamespace;
            }

            string versionSegment = NormalizeVersion(version);
            if (string.IsNullOrWhiteSpace(versionSegment))
                return baseNamespace;

            if (string.IsNullOrWhiteSpace(baseNamespace))
                return $"V{versionSegment}";

            return string.Concat(baseNamespace, ".V", versionSegment);
        }

        private static string GetNamespaceFromPath(string path)
        {
            return string.Join(".", Path.GetFileName(path)
                .Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => !part.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .Where(part => !part.Equals("schema", StringComparison.OrdinalIgnoreCase))
                .Select(part => part.FirstCharToUpper())
            );
        }

        private static string NormalizePackage(string package)
        {
            var separators = new[] { '.', '-', '_', ' ' };
            var segments = package
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => NormalizeIdentifier(segment.Trim()))
                .Where(segment => segment.Length > 0)
                .ToArray();

            if (segments.Length == 0)
                return NormalizeIdentifier(package);

            return string.Join(".", segments);
        }

        private static string NormalizeVersion(string version)
        {
            var sb = new StringBuilder();
            bool appendedUnderscore = false;
            foreach (char ch in version)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                    appendedUnderscore = false;
                }
                else
                {
                    if (!appendedUnderscore)
                    {
                        sb.Append('_');
                        appendedUnderscore = true;
                    }
                }
            }

            string normalized = sb.ToString().Trim('_');
            return string.IsNullOrEmpty(normalized) ? "0" : normalized;
        }

        private static string NormalizeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "_";

            var sb = new StringBuilder(value.Length);
            bool capitalizeNext = true;
            foreach (char ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    char toAppend = capitalizeNext ? char.ToUpperInvariant(ch) : ch;
                    sb.Append(toAppend);
                    capitalizeNext = false;
                }
                else
                {
                    capitalizeNext = true;
                }
            }

            if (sb.Length == 0)
                return "_";

            if (!char.IsLetter(sb[0]) && sb[0] != '_')
                sb.Insert(0, '_');

            return sb.ToString();
        }
    }
}
