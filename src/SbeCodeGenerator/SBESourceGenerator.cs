using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Diagnostics;
using SbeSourceGenerator.Generators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            // Stage 1: Collect XML schema files from additional files
            IncrementalValuesProvider<AdditionalText> xmlSchemaFiles = CollectXmlSchemaFiles(initContext);

            // Stage 2 & 3: Register source generation with diagnostic support
            RegisterSourceGeneration(initContext, xmlSchemaFiles);
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
        private static void RegisterSourceGeneration(IncrementalGeneratorInitializationContext initContext, IncrementalValuesProvider<AdditionalText> xmlFiles)
        {
            initContext.RegisterSourceOutput(xmlFiles, (sourceContext, text) =>
            {
                try
                {
                    string path = text.Path;
                    string ns = GetNamespaceFromPath(path);
                    var d = new XmlDocument();
                    d.Load(path);
                    
                    // Create a per-schema context to hold mutable state
                    var context = new SchemaContext();
                    
                    // Use specialized generators to handle different categories
                    var typesGenerator = new TypesCodeGenerator();
                    var messagesGenerator = new MessagesCodeGenerator();
                    var utilitiesGenerator = new UtilitiesCodeGenerator();
                    
                    foreach (var item in typesGenerator.Generate(ns, d, context, sourceContext))
                        sourceContext.AddSource(item.name, item.content);
                    foreach (var item in messagesGenerator.Generate(ns, d, context, sourceContext))
                        sourceContext.AddSource(item.name, item.content);
                    foreach (var item in utilitiesGenerator.Generate(ns, d, context, sourceContext))
                        sourceContext.AddSource(item.name, item.content);
                }
                catch (Exception ex)
                {
                    // Only report diagnostic if context has a valid CancellationToken (not default)
                    if (sourceContext.CancellationToken != default)
                    {
                        sourceContext.ReportDiagnostic(Diagnostic.Create(
                            SbeDiagnostics.MalformedSchema,
                            Location.None,
                            text.Path,
                            ex.Message));
                    }
                }
            });
        }


        private static string GetNamespaceFromPath(string path)
        {
            return string.Join(".", Path.GetFileName(path)
                .Split('-')
                .Where(part => !part.Contains(".xml"))
                .Select(part => part.FirstCharToUpper())
            );
        }
    }
}
