using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Generators;
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

            // Stage 2: Build transformation pipeline to parse schemas and generate source code
            IncrementalValuesProvider<(string name, string content)> generatedSources = BuildTransformationPipeline(xmlSchemaFiles);

            // Stage 3: Register generated sources for output
            RegisterSourceGeneration(initContext, generatedSources);
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
        /// Stage 2: Builds the transformation pipeline that parses XML schemas and generates C# source code.
        /// Data flow: XML files → parsed schema models → generated (name, content) tuples
        /// </summary>
        private static IncrementalValuesProvider<(string name, string content)> BuildTransformationPipeline(IncrementalValuesProvider<AdditionalText> xmlFiles)
        {
            return GetNamesAndContents(xmlFiles);
        }

        /// <summary>
        /// Stage 3: Registers the generated source files for output to the compilation.
        /// Data flow: (name, content) tuples → added to SourceProductionContext
        /// </summary>
        private static void RegisterSourceGeneration(IncrementalGeneratorInitializationContext initContext, IncrementalValuesProvider<(string name, string content)> generatedSources)
        {
            initContext.RegisterSourceOutput(generatedSources, (spc, nameAndContent) =>
            {
                spc.AddSource(nameAndContent.name, nameAndContent.content);
            });
        }

        private static IncrementalValuesProvider<(string name, string content)> GetNamesAndContents(IncrementalValuesProvider<AdditionalText> textFiles)
        {
            return textFiles.SelectMany((text, cancellationToken) => GetNameAndContent(text, cancellationToken));
        }

        private static IEnumerable<(string name, string content)> GetNameAndContent(AdditionalText text, CancellationToken cancellationToken)
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
            
            foreach (var item in typesGenerator.Generate(ns, d, context))
                yield return item;
            foreach (var item in messagesGenerator.Generate(ns, d, context))
                yield return item;
            foreach (var item in utilitiesGenerator.Generate(ns, d, context))
                yield return item;
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
