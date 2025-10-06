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
        /// Initializes the source generator by defining the execution pipeline.
        /// Pipeline stages: XML schema collection → schema parsing → code generation → source output registration
        /// </summary>
        public void Initialize(IncrementalGeneratorInitializationContext initContext)
        {
            // Stage 1: Collect XML schema files from additional files
            IncrementalValuesProvider<AdditionalText> xmlSchemaFiles = CollectXmlSchemaFiles(initContext);

            // Stage 2 & 3: Register source generation with diagnostic support
            RegisterSourceGeneration(initContext, xmlSchemaFiles);
        }

        /// <summary>
        /// Stage 1: Collects all XML schema files (.xml) from the project's additional files.
        /// Data flow: AdditionalTextsProvider → filtered XML files
        /// </summary>
        private static IncrementalValuesProvider<AdditionalText> CollectXmlSchemaFiles(IncrementalGeneratorInitializationContext initContext)
        {
            return initContext.AdditionalTextsProvider.Where(file => file.Path.EndsWith(".xml"));
        }

        /// <summary>
        /// Stage 2 & 3: Registers the generated source files for output to the compilation with diagnostic support.
        /// Data flow: XML files → parsing & code generation → added to SourceProductionContext with diagnostics
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
