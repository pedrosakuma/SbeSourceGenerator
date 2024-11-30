using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Generators.Fields;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

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
            StringBuilder sb = new StringBuilder();
            new NumberExtensions(ns).AppendFileContent(sb);
            yield return ($"Utilities\\NumberExtensions", sb.ToString());
        }

        private static IEnumerable<(string name, string content)> GenerateMessages(string ns, XmlDocument d)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(d.NameTable);
            nsmgr.AddNamespace("sbe", "http://fixprotocol.io/2016/sbe");
            var messageNodes = d.SelectNodes("//sbe:message", nsmgr);
            foreach (XmlElement messageNode in messageNodes)
            {
                var generator = new MessageDefinition(
                        ns,
                        messageNode.GetAttribute("name").FirstCharToUpper(),
                        messageNode.GetAttribute("id"),
                        messageNode.GetAttribute("description"),
                        messageNode.GetAttribute("semanticType"),
                        messageNode.GetAttribute("deprecated"),
                        messageNode.ChildNodes
                            .Cast<XmlElement>()
                            .Where(x => x.Name == "field")
                            .Where(x => x.GetAttribute("presence") == "" || x.GetAttribute("presence") == "optional")
                            .Where(x => !TypesCatalog.CustomConstantTypes.Contains(x.GetAttribute("type")))
                            .Select(node =>
                                node.GetAttribute("presence") switch
                                {
                                    "optional" => new OptionalMessageFieldDefinition(
                                        node.GetAttribute("name").FirstCharToUpper(),
                                        node.GetAttribute("id"),
                                        ToNativeType(node.GetAttribute("type")),
                                        GetUnderlyingType(node.GetAttribute("type")),
                                        node.GetAttribute("description"),
                                        node.GetAttribute("offset") == "" ? null : int.Parse(node.GetAttribute("offset")),
                                        GetTypeLength(node.GetAttribute("type"))
                                    ),
                                    _ => (IFileContentGenerator)new MessageFieldDefinition(
                                        node.GetAttribute("name").FirstCharToUpper(),
                                        node.GetAttribute("id"),
                                        ToNativeType(node.GetAttribute("type")),
                                        node.GetAttribute("description"),
                                        node.GetAttribute("offset") == "" ? null : int.Parse(node.GetAttribute("offset")),
                                        GetTypeLength(node.GetAttribute("type"))
                                    )
                                }
                            )
                            .ToList(),
                        messageNode.ChildNodes
                            .Cast<XmlElement>()
                            .Where(x => x.Name == "field" && x.GetAttribute("presence") == "constant" && x.GetAttribute("valueRef") != "")
                            .Select(node => (IFileContentGenerator)
                                new ConstantMessageFieldDefinition(
                                    node.GetAttribute("name").FirstCharToUpper(),
                                    node.GetAttribute("id"),
                                    node.GetAttribute("type"),
                                    node.GetAttribute("description") != ""? node.GetAttribute("description") : node.GetAttribute("type"),
                                    node.GetAttribute("valueRef")
                                )
                            )
                            .ToList(),
                        messageNode.ChildNodes
                            .Cast<XmlElement>()
                            .Where(x => x.Name == "group")
                            .Select(node => (IFileContentGenerator)
                                new GroupDefinition(
                                    ns,
                                    node.GetAttribute("name").FirstCharToUpper(),
                                    node.GetAttribute("id"),
                                    node.GetAttribute("dimensionType"),
                                    node.GetAttribute("description"),
                                    node.ChildNodes
                                        .Cast<XmlElement>()
                                        .Where(x => x.Name == "field")
                                        .Where(x=> x.GetAttribute("presence") == "" || x.GetAttribute("presence") == "optional")
                                        .Where(x => !TypesCatalog.CustomConstantTypes.Contains(x.GetAttribute("type")))
                                        .Select(field => (IFileContentGenerator)
                                            new MessageFieldDefinition(
                                                field.GetAttribute("name").FirstCharToUpper(),
                                                field.GetAttribute("id"),
                                                ToNativeType(field.GetAttribute("type")),
                                                field.GetAttribute("description"),
                                                field.GetAttribute("offset") == "" ? null : int.Parse(node.GetAttribute("offset")),
                                                GetTypeLength(field.GetAttribute("type"))
                                            )
                                        ).ToList(),
                                    node.ChildNodes
                                        .Cast<XmlElement>()
                                        .Where(x => x.Name == "field" && x.GetAttribute("presence") == "constant")
                                        .Select(field => (IFileContentGenerator)
                                            new ConstantMessageFieldDefinition(
                                                field.GetAttribute("name").FirstCharToUpper(),
                                                field.GetAttribute("id"),
                                                field.GetAttribute("type"),
                                                field.GetAttribute("description"),
                                                field.GetAttribute("valueRef")
                                            )
                                        ).ToList()
                                )
                            ).ToList(),
                        messageNode.ChildNodes
                            .Cast<XmlElement>()
                            .Where(x => x.Name == "data")
                            .Select(node => (IFileContentGenerator)
                                new DataFieldDefinition(
                                    node.GetAttribute("name").FirstCharToUpper(),
                                    node.GetAttribute("id"),
                                    node.GetAttribute("type"),
                                    node.GetAttribute("description")
                                )
                            ).ToList()
                    );
                StringBuilder sb = new StringBuilder();
                generator.AppendFileContent(sb);
                yield return ($"{ns}\\Messages\\{generator.Name}", sb.ToString());
            }
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
            var generator = new EnumFlagsDefinition(
                ns,
                typeNode.GetAttribute("name").FirstCharToUpper(),
                typeNode.GetAttribute("description"),
                ToNativeType(typeNode.GetAttribute("encodingType")),
                GetTypeLength(ToNativeType(typeNode.GetAttribute("encodingType"))),
                typeNode.ChildNodes
                    .Cast<XmlElement>()
                    .Select(node => new EnumFieldDefinition(
                        node.GetAttribute("name"),
                        node.GetAttribute("description"),
                        node.InnerText
                    ))
                    .ToList()
            );
            if (generator is IBlittable blittableType)
                TypesCatalog.CustomTypeLengths[typeNode.GetAttribute("name")] = blittableType.Length;
            StringBuilder sb = new StringBuilder();
            generator.AppendFileContent(sb);
            yield return ($"{ns}\\Sets\\{typeNode.GetAttribute("name")}", sb.ToString());
        }

        private static IEnumerable<(string name, string content)> GenerateType(string ns, XmlElement typeNode)
        {
            if (!IsPrimitiveType(typeNode.GetAttribute("name")))
            {
                var generator =
                    (typeNode.GetAttribute("primitiveType"), typeNode.GetAttribute("presence")) switch
                    {
                        
                        (_, "constant") => new ConstantTypeDefinition(
                            ns,
                            typeNode.GetAttribute("name"),
                            typeNode.GetAttribute("description"),
                            ToNativeType(typeNode.GetAttribute("primitiveType")),
                            typeNode.GetAttribute("semanticType"),
                            typeNode.GetAttribute("length"),
                            typeNode.InnerText
                        ),
                        ("char", _) => (IFileContentGenerator)new FixedSizeCharTypeDefinition
                        (
                            ns,
                            typeNode.GetAttribute("name"),
                            typeNode.GetAttribute("description"),
                            typeNode.GetAttribute("length") == "" ? 0 : int.Parse(typeNode.GetAttribute("length"))
                        ),
                        (_, "optional") => new OptionalTypeDefinition(
                            ns,
                            typeNode.GetAttribute("name"),
                            typeNode.GetAttribute("description"),
                            ToNativeType(typeNode.GetAttribute("primitiveType")),
                            typeNode.GetAttribute("semanticType"),
                            typeNode.GetAttribute("nullValue"),
                            GetTypeLength(typeNode.GetAttribute("primitiveType"))
                        ),
                        (_, _) => new TypeDefinition(
                            ns,
                            typeNode.GetAttribute("name"),
                            typeNode.GetAttribute("description"),
                            ToNativeType(typeNode.GetAttribute("primitiveType")),
                            typeNode.GetAttribute("semanticType"),
                            GetTypeLength(typeNode.GetAttribute("primitiveType"))
                        )
                    };
                if (generator is ConstantTypeDefinition constantType) 
                    TypesCatalog.CustomConstantTypes.Add(constantType.Name);
                if (generator is IBlittable blittableType)
                    TypesCatalog.CustomTypeLengths[typeNode.GetAttribute("name")] = blittableType.Length;
                StringBuilder sb = new StringBuilder();
                generator.AppendFileContent(sb);
                yield return ($"{ns}\\Types\\{typeNode.GetAttribute("name")}", sb.ToString());
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
                    TypesCatalog.PrimitiveTypeLengths[ToNativeType(typeNode.GetAttribute("encodingType"))],
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
                    TypesCatalog.PrimitiveTypeLengths[ToNativeType(typeNode.GetAttribute("encodingType"))],
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
            if (generator is IBlittable blittableType)
                TypesCatalog.CustomTypeLengths[typeNode.GetAttribute("name")] = blittableType.Length;
            if(generator is EnumDefinition enumDefinition)
                TypesCatalog.EnumPrimitiveTypes.Add(enumDefinition.Name, enumDefinition.EncodingType);
            StringBuilder sb = new StringBuilder();
            generator.AppendFileContent(sb);
            yield return ($"{ns}\\Enums\\{typeNode.GetAttribute("name").FirstCharToUpper()}", sb.ToString());
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
                    .Select(node => (IFileContentGenerator)((node.GetAttribute("presence"), node.GetAttribute("length")) switch
                    {
                        ("constant", _) => new ConstantTypeFieldDefinition(
                            node.GetAttribute("name").FirstCharToUpper(),
                            node.GetAttribute("description"),
                            ToNativeType(node.GetAttribute("primitiveType")),
                            InsertQuotationsIfNeeded(node.InnerText, node.GetAttribute("primitiveType"), node.GetAttribute("length")),
                            node.GetAttribute("valueRef")
                        ),
                        ("optional", _) => new NullableValueFieldDefinition(
                            node.GetAttribute("name").FirstCharToUpper(),
                            node.GetAttribute("description"),
                            ToNativeType(node.GetAttribute("primitiveType")),
                            GetTypeLength(ToNativeType(node.GetAttribute("primitiveType")))
                        ),
                        ("", "0") => new ArrayFieldDefinition(
                            node.GetAttribute("name").FirstCharToUpper(),
                            node.GetAttribute("description"),
                            "byte"
                        ),
                        (_, _) => new ValueFieldDefinition(
                            node.GetAttribute("name").FirstCharToUpper(),
                            node.GetAttribute("description"),
                            ToNativeType(node.GetAttribute("primitiveType")),
                            GetTypeLength(ToNativeType(node.GetAttribute("primitiveType")))
                        )
                    }
                    ))
                    .ToList()
            );
            if (generator.Fields.All(f => f is IBlittable))
                TypesCatalog.CustomTypeLengths[typeNode.GetAttribute("name")] = generator.Fields.Sum(f => ((IBlittable)f).Length);
            StringBuilder sb = new StringBuilder();
            generator.AppendFileContent(sb);
            yield return ($"{ns}\\Composites\\{generator.Name}", sb.ToString());
            var semanticGenerator = (generator.SemanticType) switch
            {
                "Price" => (IFileContentGenerator)new DecimalSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "Percentage" => new DecimalSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "Qty" => new DecimalSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "MonthYear" => new MonthYearSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                "UTCTimestamp" => new UTCTimestampSemanticTypeDefinition(generator.Namespace, generator.Name, generator.Fields),
                _ => new NullSemanticTypeDefintion()
            };
            sb.Clear();
            semanticGenerator.AppendFileContent(sb);
            if (semanticGenerator is not NullSemanticTypeDefintion)
                yield return ($"{ns}\\Composites\\{generator.Name}.{generator.SemanticType}", sb.ToString());
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
