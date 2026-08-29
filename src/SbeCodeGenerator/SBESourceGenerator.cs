using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SbeSourceGenerator.Diagnostics;
using SbeSourceGenerator.Generators;
using SbeSourceGenerator.Schema;
using SbeSourceGenerator.SemanticTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;

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
            // Issue #166: emit the runtime types (SbeSemanticTypeAttribute, ISbeSemanticConverter,
            // built-in converters) into every consumer compilation BEFORE any user attribute can
            // reference them. RegisterPostInitializationOutput is the supported channel for this.
            initContext.RegisterPostInitializationOutput(ctx =>
                ctx.AddSource(SemanticTypesRuntimeSource.HintName, SemanticTypesRuntimeSource.Source));

            // Stage 1: Collect XML schema files from additional files
            IncrementalValuesProvider<AdditionalText> xmlSchemaFiles = CollectXmlSchemaFiles(initContext);

            // Issue #166: collect user [assembly: SbeSemanticType(...)] declarations syntax-first.
            var userAttributeResults = initContext.SyntaxProvider
                .CreateSyntaxProvider(SemanticTypesAttributeScanner.IsCandidate, SemanticTypesAttributeScanner.Transform)
                .SelectMany((arr, _) => arr)
                .Collect();

            // Surface SBE017 diagnostics from attribute parsing.
            initContext.RegisterSourceOutput(userAttributeResults, (ctx, results) =>
            {
                foreach (var r in results)
                {
                    if (r.Diagnostic != null)
                        ctx.ReportDiagnostic(r.Diagnostic);
                }
            });

            var semanticRegistry = userAttributeResults
                .Select(static (results, _) => BuildSemanticRegistry(results));

            // Stage 2: Combine with analyzer config options (for SbeAssumeHostEndianness hint) and the registry.
            var combined = xmlSchemaFiles
                .Combine(initContext.AnalyzerConfigOptionsProvider)
                .Combine(semanticRegistry)
                .Select(static (input, _) => (
                    Path: input.Left.Left.Path,
                    Schema: input.Left.Left,
                    Options: input.Left.Right,
                    SemanticRegistry: input.Right))
                .WithTrackingName("PerSchemaGeneration");

            var runtimeNamespaces = xmlSchemaFiles
                .Select(static (schemaFile, cancellationToken) => TryGetRuntimeNamespace(schemaFile, cancellationToken))
                .Collect()
                .WithTrackingName("RuntimeNamespaceCollection");

            // Stage 3: Register source generation with diagnostic support
            RegisterSourceGeneration(initContext, combined);
            RegisterRuntimeGeneration(initContext, runtimeNamespaces);
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
        private static void RegisterSourceGeneration(IncrementalGeneratorInitializationContext initContext,
            IncrementalValuesProvider<(string Path, AdditionalText Schema, AnalyzerConfigOptionsProvider Options, SemanticConverterRegistry SemanticRegistry)> combined)
        {
            initContext.RegisterSourceOutput(combined, (sourceContext, input) =>
            {
                var (path, additionalText, configOptions, semanticRegistry) = input;

                // Read the optional SbeAssumeHostEndianness MSBuild property
                string? hostHint = null;
                if (configOptions.GlobalOptions.TryGetValue("build_property.SbeAssumeHostEndianness", out var hintValue)
                    && !string.IsNullOrEmpty(hintValue))
                {
                    hostHint = hintValue;
                }

                var emittedHintNames = new HashSet<string>(StringComparer.Ordinal);

                SourceText? sourceText = null;
                try
                {
                    sourceText = additionalText.GetText(sourceContext.CancellationToken);
                    var xmlContent = sourceText?.ToString();
                    if (string.IsNullOrEmpty(xmlContent))
                        return;

                    var schema = SchemaReader.Parse(xmlContent!, sourceContext, path, sourceText);

                    string ns = GetNamespaceFromSchema(schema, path);
                    string schemaKey = CreateSchemaKey(path);

                    // Create a per-schema context to hold mutable state.
                    var context = new SchemaContext(schemaKey);
                    context.SemanticConverters = semanticRegistry;

                    if (!string.IsNullOrEmpty(schema.ByteOrder))
                    {
                        context.ByteOrder = schema.ByteOrder;
                    }

                    context.EndianConversion = ComputeEndianConversion(
                        context.ByteOrder, hostHint, sourceContext, schema, path);

                    if (!string.IsNullOrEmpty(schema.HeaderType))
                        context.HeaderType = schema.HeaderType;

                    // Use specialized generators to handle different categories
                    var typesGenerator = new TypesCodeGenerator();
                    var messagesGenerator = new MessagesCodeGenerator();
                    var validationGenerator = new ValidationGenerator();

                    var generators = new (string phase, ICodeGenerator gen)[]
                    {
                        ("types", typesGenerator),
                        ("messages", messagesGenerator),
                        ("dispatcher", new DispatcherGenerator()),
                        ("validation", validationGenerator)
                    };

                    foreach (var (phase, gen) in generators)
                    {
                        try
                        {
                            foreach (var item in gen.Generate(ns, schema, context, sourceContext))
                            {
                                try
                                {
                                    if (!emittedHintNames.Add(item.name))
                                    {
                                        // Roslyn would throw ArgumentException on duplicate hintName,
                                        // aborting the rest of the phase. Suppress and continue so a
                                        // single duplicate doesn't cascade into thousands of CS0246s
                                        // against partially-emitted files.
                                        if (!sourceContext.CancellationToken.IsCancellationRequested)
                                        {
                                            sourceContext.ReportDiagnostic(Diagnostic.Create(
                                                SbeDiagnostics.DuplicateGeneratedSource,
                                                Location.None,
                                                item.name,
                                                phase));
                                        }
                                        continue;
                                    }
                                    sourceContext.AddSource(item.name, item.content);
                                }
                                catch (Exception itemEx) when (!sourceContext.CancellationToken.IsCancellationRequested)
                                {
                                    // Per-item failure must not derail subsequent items in the same phase.
                                    sourceContext.ReportDiagnostic(Diagnostic.Create(
                                        SbeDiagnostics.MalformedSchema,
                                        Location.None,
                                        path,
                                        $"[{phase}] {item.name}: {itemEx.Message}"));
                                }
                            }
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
                        var location = ex is XmlException xmlException
                            ? XmlDiagnosticLocation.CreateFromException(sourceText, additionalText.Path, xmlException)
                            : Location.None;
                        sourceContext.ReportDiagnostic(Diagnostic.Create(
                            SbeDiagnostics.MalformedSchema,
                            location,
                            additionalText.Path,
                            ex.Message));
                    }
                }
            });
        }

        private static void RegisterRuntimeGeneration(
            IncrementalGeneratorInitializationContext initContext,
            IncrementalValueProvider<ImmutableArray<string?>> runtimeNamespaces)
        {
            initContext.RegisterSourceOutput(runtimeNamespaces, (sourceContext, namespaces) =>
            {
                if (namespaces.IsDefaultOrEmpty)
                    return;

                var emittedRuntimeNamespaces = new HashSet<string>(StringComparer.Ordinal);
                foreach (var runtimeNamespace in namespaces)
                {
                    if (string.IsNullOrWhiteSpace(runtimeNamespace))
                        continue;

                    var resolvedRuntimeNamespace = runtimeNamespace!;
                    if (!emittedRuntimeNamespaces.Add(resolvedRuntimeNamespace))
                        continue;

                    foreach (var item in UtilitiesCodeGenerator.GenerateRuntimeSources(
                        resolvedRuntimeNamespace,
                        typeName => CreateRuntimeHintName(resolvedRuntimeNamespace, typeName)))
                    {
                        sourceContext.AddSource(item.name, item.content);
                    }
                }
            });
        }

        private static SemanticConverterRegistry BuildSemanticRegistry(ImmutableArray<UserAttributeResult> userRegs)
        {
            var userRegistrations = userRegs.IsDefaultOrEmpty
                ? ImmutableArray<SemanticConverterRegistration>.Empty
                : userRegs.Where(r => r.Registration != null).Select(r => r.Registration!).ToImmutableArray();

            return SemanticConverterRegistry.Build(userRegistrations);
        }

        private static string? TryGetRuntimeNamespace(AdditionalText additionalText, CancellationToken cancellationToken)
        {
            var xmlContent = additionalText.GetText(cancellationToken)?.ToString();
            if (string.IsNullOrEmpty(xmlContent))
                return null;

            try
            {
                var schema = SchemaReader.Parse(xmlContent!);
                return GetNamespaceFromSchema(schema, additionalText.Path);
            }
            catch
            {
                return null;
            }
        }

        private static string CreateRuntimeHintName(string runtimeNamespace, string typeName)
        {
            return string.Concat(runtimeNamespace, "\\Runtime\\", typeName);
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

        /// <summary>
        /// Computes the endian conversion strategy from schema byteOrder and optional host hint.
        /// </summary>
        private static EndianConversion ComputeEndianConversion(
            string schemaByteOrder,
            string? hostHint,
            SourceProductionContext sourceContext,
            ParsedSchema schema,
            string schemaPath)
        {
            bool isBigEndianSchema = schemaByteOrder.Equals("bigEndian", StringComparison.OrdinalIgnoreCase);

            if (hostHint == null)
            {
                // No hint: LE schemas use None (pragmatic — LE hosts are ~100% of .NET targets),
                // BE schemas use Conditional (safe path)
                if (isBigEndianSchema)
                {
                    if (!sourceContext.CancellationToken.IsCancellationRequested)
                    {
                        sourceContext.ReportDiagnostic(Diagnostic.Create(
                            SbeDiagnostics.NonNativeByteOrder,
                            schema.Source.GetAttributeOrElement("byteOrder"),
                            schemaPath,
                            schemaByteOrder));
                    }
                    return EndianConversion.Conditional;
                }
                return EndianConversion.None;
            }

            bool isLittleEndianHint = hostHint.Equals("LittleEndian", StringComparison.OrdinalIgnoreCase);
            bool isBigEndianHint = hostHint.Equals("BigEndian", StringComparison.OrdinalIgnoreCase);

            if (!isLittleEndianHint && !isBigEndianHint)
            {
                // Invalid hint — warn and fall back to safe behavior
                if (!sourceContext.CancellationToken.IsCancellationRequested)
                {
                    sourceContext.ReportDiagnostic(Diagnostic.Create(
                        SbeDiagnostics.InvalidHostEndianness,
                        Location.None,
                        hostHint));
                }
                return isBigEndianSchema ? EndianConversion.Conditional : EndianConversion.None;
            }

            // Schema and hint match → no conversion needed
            if (isBigEndianSchema == isBigEndianHint)
                return EndianConversion.None;

            // Schema and hint differ → always reverse
            return EndianConversion.AlwaysReverse;
        }
    }
}
