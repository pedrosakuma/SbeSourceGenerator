using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Generators;
using SbeSourceGenerator.Generators.Fields;
using SbeSourceGenerator.Generators.Types;
using SbeSourceGenerator.Helpers;
using SbeSourceGenerator.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            foreach (var item in GenerateTypes(ns, d))
                yield return item;
            foreach (var item in GenerateMessages(ns, d))
                yield return item;
            StringBuilder sb = new StringBuilder();
            new NumberExtensions(ns).AppendFileContent(sb);
            yield return ($"Utilities\\NumberExtensions", sb.ToString());
        }

        private static IEnumerable<(string name, string content)> GenerateMessages(string ns, XmlDocument d)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(d.NameTable);
            nsmgr.AddNamespace("sbe", "http://fixprotocol.io/2016/sbe");
            var messageNodes = d.SelectNodes("//sbe:message", nsmgr);
            var messages = new List<MessageDefinition>();
            foreach (XmlElement messageNode in messageNodes)
            {
                var messageDto = SchemaParser.ParseMessage(messageNode);
                
                var generator = new MessageDefinition(
                        ns,
                        messageDto.Name.FirstCharToUpper(),
                        messageDto.Id,
                        messageDto.Description,
                        messageDto.SemanticType,
                        messageDto.Deprecated,
                        messageDto.Fields
                            .Select(field =>
                                field.Presence switch
                                {
                                    "optional" => new OptionalMessageFieldDefinition(
                                        field.Name.FirstCharToUpper(),
                                        field.Id,
                                        ToNativeType(field.Type),
                                        GetUnderlyingType(field.Type),
                                        field.Description,
                                        field.Offset == "" ? null : int.Parse(field.Offset),
                                        GetTypeLength(field.Type)
                                    ),
                                    _ => (IFileContentGenerator)new MessageFieldDefinition(
                                        field.Name.FirstCharToUpper(),
                                        field.Id,
                                        ToNativeType(field.Type),
                                        field.Description,
                                        field.Offset == "" ? null : int.Parse(field.Offset),
                                        GetTypeLength(field.Type)
                                    )
                                }
                            )
                            .ToList(),
                        messageDto.Constants
                            .Select(constant => (IFileContentGenerator)
                                new ConstantMessageFieldDefinition(
                                    constant.Name.FirstCharToUpper(),
                                    constant.Id,
                                    constant.Type,
                                    constant.Description != "" ? constant.Description : constant.Type,
                                    constant.ValueRef
                                )
                            )
                            .ToList(),
                        messageDto.Groups
                            .Select(group => (IFileContentGenerator)
                                new GroupDefinition(
                                    ns,
                                    group.Name.FirstCharToUpper(),
                                    group.Id,
                                    group.DimensionType,
                                    group.Description,
                                    group.Fields
                                        .Select(field => (IFileContentGenerator)
                                            new MessageFieldDefinition(
                                                field.Name.FirstCharToUpper(),
                                                field.Id,
                                                ToNativeType(field.Type),
                                                field.Description,
                                                field.Offset == "" ? null : int.Parse(field.Offset),
                                                GetTypeLength(field.Type)
                                            )
                                        ).ToList(),
                                    group.Constants
                                        .Select(constant => (IFileContentGenerator)
                                            new ConstantMessageFieldDefinition(
                                                constant.Name.FirstCharToUpper(),
                                                constant.Id,
                                                constant.Type,
                                                constant.Description,
                                                constant.ValueRef
                                            )
                                        ).ToList()
                                )
                            ).ToList(),
                        messageDto.Data
                            .Select(data => (IFileContentGenerator)
                                new DataFieldDefinition(
                                    data.Name.FirstCharToUpper(),
                                    data.Id,
                                    data.Type,
                                    data.Description
                                )
                            ).ToList()
                    );
                messages.Add(generator);
                StringBuilder sb = new StringBuilder();
                generator.AppendFileContent(sb);
                yield return ($"{ns}\\Messages\\{generator.Name}", sb.ToString());
            }
            foreach (var item in GenerateParser(ns, messages))
                yield return item;
        }

        private static IEnumerable<(string name, string content)> GenerateParser(string ns, List<MessageDefinition> messages)
        {
            StringBuilder sb = new StringBuilder();
            new ParserGenerator(ns, "", messages).AppendFileContent(sb);
            yield return ($"{ns}\\MessageParser", sb.ToString());
        }

        private static string? GetUnderlyingType(string type)
        {
            TypesCatalog.EnumPrimitiveTypes.TryGetValue(type, out string? underlyingType);
            return underlyingType;
        }
        private static int GetTypeLength(string type)
        {
            int length;
            if (!TypesCatalog.PrimitiveTypeLengths.TryGetValue(ToNativeType(type), out length)
                && !TypesCatalog.CustomTypeLengths.TryGetValue(type, out length))
                throw new ArgumentException($"Could not get type {type} length");
            return length;
        }

        private static string GetNamespaceFromPath(string path)
        {
            return string.Join(".", Path.GetFileName(path)
                .Split('-')
                .Where(part => !part.Contains(".xml"))
                .Select(part => part.FirstCharToUpper())
            );
        }

        private static IEnumerable<(string name, string content)> GenerateTypes(string ns, XmlDocument d)
        {
            var typeNodes = d.SelectNodes("//types/*");
            foreach (XmlElement typeNode in typeNodes)
            {
                var generatedType = typeNode.Name switch
                {
                    "composite" => GenerateComposite(ns, typeNode),
                    "enum" => GenerateEnum(ns, typeNode),
                    "type" => GenerateType(ns, typeNode),
                    "set" => GenerateSet(ns, typeNode),
                    _ => Enumerable.Empty<(string name, string content)>()
                };
                foreach (var item in generatedType)
                    yield return item;
            }
        }

        private static IEnumerable<(string name, string content)> GenerateSet(string ns, XmlElement typeNode)
        {
            var enumDto = SchemaParser.ParseEnum(typeNode);
            
            var generator = new EnumFlagsDefinition(
                ns,
                enumDto.Name.FirstCharToUpper(),
                enumDto.Description,
                ToNativeType(enumDto.EncodingType),
                GetTypeLength(ToNativeType(enumDto.EncodingType)),
                enumDto.Choices
                    .Select(choice => new EnumFieldDefinition(
                        choice.Name,
                        choice.Description,
                        choice.InnerText
                    ))
                    .ToList()
            );
            if (generator is IBlittable blittableType)
                TypesCatalog.CustomTypeLengths[enumDto.Name] = blittableType.Length;
            StringBuilder sb = new StringBuilder();
            generator.AppendFileContent(sb);
            yield return ($"{ns}\\Sets\\{enumDto.Name}", sb.ToString());
        }

        private static IEnumerable<(string name, string content)> GenerateType(string ns, XmlElement typeNode)
        {
            var typeDto = SchemaParser.ParseType(typeNode);
            
            if (!IsPrimitiveType(typeDto.Name))
            {
                var generator =
                    (typeDto.PrimitiveType, typeDto.Presence) switch
                    {

                        (_, "constant") => new ConstantTypeDefinition(
                            ns,
                            typeDto.Name,
                            typeDto.Description,
                            ToNativeType(typeDto.PrimitiveType),
                            typeDto.SemanticType,
                            typeDto.Length,
                            typeDto.InnerText
                        ),
                        ("char", _) => (IFileContentGenerator)new FixedSizeCharTypeDefinition
                        (
                            ns,
                            typeDto.Name,
                            typeDto.Description,
                            typeDto.Length == "" ? 0 : int.Parse(typeDto.Length)
                        ),
                        (_, "optional") => new OptionalTypeDefinition(
                            ns,
                            typeDto.Name,
                            typeDto.Description,
                            ToNativeType(typeDto.PrimitiveType),
                            typeDto.SemanticType,
                            typeDto.NullValue,
                            GetTypeLength(typeDto.PrimitiveType)
                        ),
                        (_, _) => new TypeDefinition(
                            ns,
                            typeDto.Name,
                            typeDto.Description,
                            ToNativeType(typeDto.PrimitiveType),
                            typeDto.SemanticType,
                            GetTypeLength(typeDto.PrimitiveType)
                        )
                    };
                if (generator is ConstantTypeDefinition constantType)
                    TypesCatalog.CustomConstantTypes.TryAdd(constantType.Name, 0);
                if (generator is IBlittable blittableType)
                    TypesCatalog.CustomTypeLengths[typeDto.Name] = blittableType.Length;
                StringBuilder sb = new StringBuilder();
                generator.AppendFileContent(sb);
                yield return ($"{ns}\\Types\\{typeDto.Name}", sb.ToString());
                if (generator is TypeDefinition typeDefinition)
                {
                    var semanticGenerator = (typeDefinition.SemanticType) switch
                    {
                        "LocalMktDate" => (IFileContentGenerator)new LocalMktDateSemanticTypeDefinition(typeDefinition.Namespace, typeDefinition.Name, false),
                        _ => new NullSemanticTypeDefintion()
                    };
                    sb.Clear();
                    semanticGenerator.AppendFileContent(sb);
                    if (semanticGenerator is not NullSemanticTypeDefintion)
                        yield return ($"{typeDefinition.Namespace}\\Types\\{typeDefinition.Name}.Semantic", sb.ToString());
                }
                if (generator is OptionalTypeDefinition optionalTypeDefinition)
                {
                    var semanticGenerator = (optionalTypeDefinition.SemanticType) switch
                    {
                        "LocalMktDate" => (IFileContentGenerator)new LocalMktDateSemanticTypeDefinition(optionalTypeDefinition.Namespace, optionalTypeDefinition.Name, true),
                        _ => new NullSemanticTypeDefintion()
                    };
                    sb.Clear();
                    semanticGenerator.AppendFileContent(sb);
                    if (semanticGenerator is not NullSemanticTypeDefintion)
                        yield return ($"{optionalTypeDefinition.Namespace}\\Types\\{optionalTypeDefinition.Name}.Semantic", sb.ToString());
                }
            }
        }

        private static IEnumerable<(string name, string content)> GenerateEnum(string ns, XmlElement typeNode)
        {
            var enumDto = SchemaParser.ParseEnum(typeNode);
            
            var generator = IsNullable(enumDto.EncodingType) switch
            {
                true => new NullableEnumDefinition(
                    ns,
                    enumDto.Name.FirstCharToUpper(),
                    enumDto.Description,
                    ToNativeType(enumDto.EncodingType),
                    enumDto.SemanticType,
                    TypesCatalog.PrimitiveTypeLengths[ToNativeType(enumDto.EncodingType)],
                    enumDto.Choices
                        .Select(choice => new EnumFieldDefinition(
                            choice.Name,
                            choice.Description,
                            choice.InnerText
                        ))
                        .ToList()
                ),
                false => new EnumDefinition(
                    ns,
                    enumDto.Name.FirstCharToUpper(),
                    enumDto.Description,
                    ToNativeType(enumDto.EncodingType),
                    enumDto.SemanticType,
                    TypesCatalog.PrimitiveTypeLengths[ToNativeType(enumDto.EncodingType)],
                    enumDto.Choices
                        .Select(choice => new EnumFieldDefinition(
                            choice.Name,
                            choice.Description,
                            choice.InnerText
                        ))
                        .ToList()
                    )
            };
            if (generator is IBlittable blittableType)
                TypesCatalog.CustomTypeLengths[enumDto.Name] = blittableType.Length;
            if (generator is EnumDefinition enumDefinition)
                TypesCatalog.EnumPrimitiveTypes[enumDefinition.Name] = enumDefinition.EncodingType;
            StringBuilder sb = new StringBuilder();
            generator.AppendFileContent(sb);
            yield return ($"{ns}\\Enums\\{enumDto.Name.FirstCharToUpper()}", sb.ToString());
        }

        private static IEnumerable<(string name, string content)> GenerateComposite(string ns, XmlElement typeNode)
        {
            var compositeDto = SchemaParser.ParseComposite(typeNode);
            
            var generator = new CompositeDefinition(
                ns,
                compositeDto.Name.FirstCharToUpper(),
                compositeDto.Description,
                compositeDto.SemanticType,
                compositeDto.Fields
                    .Select(field => (IFileContentGenerator)((field.Presence, field.Length) switch
                    {
                        ("constant", _) => new ConstantTypeFieldDefinition(
                            field.Name.FirstCharToUpper(),
                            field.Description,
                            ToNativeType(field.PrimitiveType),
                            InsertQuotationsIfNeeded(field.InnerText, field.PrimitiveType, field.Length),
                            field.ValueRef
                        ),
                        ("optional", _) => new NullableValueFieldDefinition(
                            field.Name.FirstCharToUpper(),
                            field.Description,
                            ToNativeType(field.PrimitiveType),
                            GetTypeLength(ToNativeType(field.PrimitiveType)),
                            field.NullValue == "" ? null : field.NullValue
                        ),
                        ("", "0") => new ArrayFieldDefinition(
                            field.Name.FirstCharToUpper(),
                            field.Description,
                            "byte"
                        ),
                        (_, _) => new ValueFieldDefinition(
                            field.Name.FirstCharToUpper(),
                            field.Description,
                            ToNativeType(field.PrimitiveType),
                            GetTypeLength(ToNativeType(field.PrimitiveType))
                        )
                    }
                    ))
                    .ToList()
            );
            if (generator.Fields.All(f => f is IBlittable))
                TypesCatalog.CustomTypeLengths[compositeDto.Name] = generator.Fields.SumFieldLength();
            StringBuilder sb = new StringBuilder();
            generator.AppendFileContent(sb);
            yield return ($"{ns}\\Composites\\{generator.Name}", sb.ToString());
            var semanticGenerator = (generator.Name) switch
            {
                "Price" => (IFileContentGenerator)new DecimalSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "Price8" => new DecimalSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "PriceOptional" => new DecimalSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "PriceOffset8Optional" => new DecimalSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "Percentage" => new DecimalSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "RatioQty" => new DecimalSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "Fixed8" => new DecimalSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "Percentage9" => new DecimalSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "MaturityMonthYear" => new MonthYearSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "UTCTimestampNanos" => new UTCTimestampSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "UTCTimestampSeconds" => new UTCTimestampSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                _ => new NullSemanticTypeDefintion()
            };
            sb.Clear();
            semanticGenerator.AppendFileContent(sb);
            if (semanticGenerator is not NullSemanticTypeDefintion)
                yield return ($"{ns}\\Composites\\{generator.Name}.Semantic", sb.ToString());
        }

        private static string InsertQuotationsIfNeeded(string innerText, string type, string length)
        {
            return (type, length) switch
            {
                ("char", "") => $"(short)'{innerText}'",
                ("char", _) => $"\"{innerText}\"",
                _ => innerText
            };
        }

        private static bool IsPrimitiveType(string v)
        {
            return v switch
            {
                "Char" => true,
                "Int8" => true,
                "Int16" => true,
                "Int32" => true,
                "Int64" => true,
                "UInt8" => true,
                "UInt16" => true,
                "UInt32" => true,
                "UInt64" => true,
                _ => false
            };
        }

        private static bool IsNullable(string v)
        {
            return v switch
            {
                "Int8NULL" => true,
                "Int16NULL" => true,
                "Int32NULL" => true,
                "Int64NULL" => true,
                "CharNULL" => true,
                "UInt8NULL" => true,
                "UInt16NULL" => true,
                "UInt32NULL" => true,
                "UInt64NULL" => true,
                _ => false
            };
        }

        private static string ToNativeType(string v)
        {
            return v switch
            {
                "int8" => "sbyte",
                "int16" => "short",
                "int32" => "int",
                "int64" => "long",
                "uint8" => "byte",
                "uint16" => "ushort",
                "uint32" => "uint",
                "uint64" => "ulong",

                "Int8" => "sbyte",
                "Int16" => "short",
                "Int32" => "int",
                "Int64" => "long",
                "Char" => "char",
                "UInt8" => "byte",
                "UInt16" => "ushort",
                "UInt32" => "uint",
                "UInt64" => "ulong",

                "Int8NULL" => "sbyte",
                "Int16NULL" => "short",
                "Int32NULL" => "int",
                "Int64NULL" => "long",
                "CharNULL" => "char",
                "UInt8NULL" => "byte",
                "UInt16NULL" => "ushort",
                "UInt32NULL" => "uint",
                "UInt64NULL" => "ulong",
                _ => v
            };
        }
    }
}
