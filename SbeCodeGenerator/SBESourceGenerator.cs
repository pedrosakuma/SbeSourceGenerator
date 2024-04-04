using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.CompilerServices;

namespace SbeSourceGenerator
{
    [Generator]
    public class SBESourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext initContext)
        {
            // define the execution pipeline here via a series of transformations:

            // find all additional files that end with .xml
            IncrementalValuesProvider<AdditionalText> xmlFiles = initContext.AdditionalTextsProvider.Where(file => file.Path.EndsWith(".xml"));

            // read their contents and save their name
            IncrementalValuesProvider<(string name, string content)> namesAndContents = GetNamesAndContents(xmlFiles);

            // generate a class that contains their values as const strings
            initContext.RegisterSourceOutput(namesAndContents, (spc, nameAndContent) =>
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

            yield return ($"Utilities\\NumberExtensions", new NumberExtensions(ns).GenerateFileContent());
        }

        private static IEnumerable<(string name, string content)> GenerateMessages(string ns, XmlDocument d)
        {
            // WIP
            //var messageNodes = d.SelectNodes("//messages/message");
            //foreach (XmlElement messageNode in messageNodes)
            //{
            //    var generator = new MessageDefinition(
            //        ns,
            //        messageNode.GetAttribute("name").FirstCharToUpper(),
            //        messageNode.GetAttribute("id"),
            //        messageNode.GetAttribute("description"),
            //        messageNode.GetAttribute("semanticType"),
            //        messageNode.GetAttribute("deprecated"),
            //        messageNode.ChildNodes
            //            .Cast<XmlElement>()
            //            .Select(node => (node.Name, node.GetAttribute("presence")) switch
            //            {
            //                ("field", "constant") => (IFileContentGenerator)new ConstantMessageFieldDefinition(
            //                    node.GetAttribute("name").FirstCharToUpper(),
            //                    node.GetAttribute("id"),
            //                    node.GetAttribute("type"),
            //                    node.GetAttribute("valueRef"),
            //                    node.GetAttribute("description")
            //                ),
            //                ("field", _) => (IFileContentGenerator)new MessageFieldDefinition(
            //                    node.GetAttribute("name").FirstCharToUpper(),
            //                    node.GetAttribute("id"),
            //                    node.GetAttribute("type"),
            //                    node.GetAttribute("valueRef"),
            //                    node.GetAttribute("description"),
            //                    node.GetAttribute("offset")
            //                ),

            //                // the presence of group and data should indicate that type is not blittable

            //                // should create inner type and field for group size
            //                ("group", _) => null,
            //                // data represents variable size field
            //                ("data", _) => null,
            //                (_, _) => null
            //            })
            //            .Where(x => x != null)
            //            .ToList()
            //    );
            //    yield return ($"{ns}\\Messages\\{generator.Name}", generator.GenerateFileContent());
            //}
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
            var generator = new EnumFlagsDefinition(
                ns,
                typeNode.GetAttribute("name").FirstCharToUpper(),
                typeNode.GetAttribute("description"),
                ToNativeType(typeNode.GetAttribute("encodingType")),
                typeNode.ChildNodes
                    .Cast<XmlElement>()
                    .Select(node => new EnumFieldDefinition(
                        node.GetAttribute("name"),
                        node.GetAttribute("description"),
                        node.InnerText
                    ))
                    .ToList()
            );
            yield return ($"{ns}\\Sets\\{typeNode.GetAttribute("name")}", generator.GenerateFileContent());
        }

        private static IEnumerable<(string name, string content)> GenerateType(string ns, XmlElement typeNode)
        {
            if (!IsPrimitiveType(typeNode.GetAttribute("name")))
            {
                var generator =
                    (typeNode.GetAttribute("primitiveType"), typeNode.GetAttribute("presence")) switch
                    {
                        ("char", _) => (IFileContentGenerator)new FixedSizeCharTypeDefinition
                        (
                            ns,
                            typeNode.GetAttribute("name"),
                            typeNode.GetAttribute("description"),
                            typeNode.GetAttribute("length")
                        ),
                        (_, "optional") => new OptionalTypeDefinition(
                            ns,
                            typeNode.GetAttribute("name"),
                            typeNode.GetAttribute("description"),
                            ToNativeType(typeNode.GetAttribute("primitiveType")),
                            typeNode.GetAttribute("semanticType"),
                            typeNode.GetAttribute("nullValue")
                        ),
                        (_, "constant") => new ConstantTypeDefinition(
                            ns,
                            typeNode.GetAttribute("name"),
                            typeNode.GetAttribute("description"),
                            ToNativeType(typeNode.GetAttribute("primitiveType")),
                            typeNode.GetAttribute("semanticType"),
                            typeNode.GetAttribute("length"),
                            typeNode.InnerText
                        ),
                        (_, _) => new TypeDefinition(
                            ns,
                            typeNode.GetAttribute("name"),
                            typeNode.GetAttribute("description"),
                            ToNativeType(typeNode.GetAttribute("primitiveType")),
                            typeNode.GetAttribute("semanticType")
                        )
                    };
                yield return ($"{ns}\\Types\\{typeNode.GetAttribute("name")}", generator.GenerateFileContent());
            }
        }

        private static IEnumerable<(string name, string content)> GenerateEnum(string ns, XmlElement typeNode)
        {
            var generator = IsNullable(typeNode.GetAttribute("encodingType")) switch
            {
                true => new NullableEnumDefinition(
                    ns,
                    typeNode.GetAttribute("name").FirstCharToUpper(),
                    typeNode.GetAttribute("description"),
                    ToNativeType(typeNode.GetAttribute("encodingType")),
                    typeNode.GetAttribute("semanticType"),
                    typeNode.ChildNodes
                        .Cast<XmlElement>()
                        .Select(node => new EnumFieldDefinition(
                            node.GetAttribute("name"),
                            node.GetAttribute("description"),
                            node.InnerText
                        ))
                        .ToList()
                ),
                false => new EnumDefinition(
                    ns,
                    typeNode.GetAttribute("name").FirstCharToUpper(),
                    typeNode.GetAttribute("description"),
                    ToNativeType(typeNode.GetAttribute("encodingType")),
                    typeNode.GetAttribute("semanticType"),
                    typeNode.ChildNodes
                        .Cast<XmlElement>()
                        .Select(node => new EnumFieldDefinition(
                            node.GetAttribute("name"),
                            node.GetAttribute("description"),
                            node.InnerText
                        ))
                        .ToList()
                    )
            };
            yield return ($"{ns}\\Enums\\{typeNode.GetAttribute("name").FirstCharToUpper()}", generator.GenerateFileContent());
        }

        private static IEnumerable<(string name, string content)> GenerateComposite(string ns, XmlElement typeNode)
        {
            var generator = new CompositeDefinition(
                ns,
                typeNode.GetAttribute("name").FirstCharToUpper(),
                typeNode.GetAttribute("description"),
                typeNode.GetAttribute("semanticType"),
                typeNode.ChildNodes
                    .Cast<XmlElement>()
                    .Select(node => (node.GetAttribute("presence"), node.GetAttribute("length")) switch
                    {
                        ("constant", _) => (IFileContentGenerator)new ConstantTypeFieldDefinition(
                            node.GetAttribute("name").FirstCharToUpper(),
                            node.GetAttribute("description"),
                            ToNativeType(node.GetAttribute("primitiveType")),
                            InsertQuotationsIfNeeded(node.InnerText, node.GetAttribute("primitiveType"), node.GetAttribute("length")),
                            node.GetAttribute("valueRef")
                        ),
                        ("optional", _) => new NullableValueFieldDefinition(
                            node.GetAttribute("name").FirstCharToUpper(),
                            node.GetAttribute("description"),
                            ToNativeType(node.GetAttribute("primitiveType"))
                        ),
                        ("", "0") => new ArrayFieldDefinition(
                            node.GetAttribute("name").FirstCharToUpper(),
                            node.GetAttribute("description"),
                            ToNativeType(node.GetAttribute("primitiveType"))
                        ),
                        (_, _) => new ValueFieldDefinition(
                            node.GetAttribute("name").FirstCharToUpper(),
                            node.GetAttribute("description"),
                            ToNativeType(node.GetAttribute("primitiveType"))
                        )
                    }
                    )
                    .ToList()
            );
            yield return ($"{ns}\\Composites\\{generator.Name}", generator.GenerateFileContent());
            var semanticGenerator = (generator.SemanticType) switch
            {
                "Price" => (IFileContentGenerator)new DecimalSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "Percentage" => new DecimalSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "Qty" => new DecimalSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "MonthYear" => new MonthYearSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "UTCTimestamp" => new UTCTimestampSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                _ => new NullSemanticTypeDefintion()
            };
            if (semanticGenerator is not NullSemanticTypeDefintion)
                yield return ($"{ns}\\Composites\\{generator.Name}.{generator.SemanticType}", semanticGenerator.GenerateFileContent());
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
