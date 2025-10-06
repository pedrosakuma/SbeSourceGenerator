using SbeSourceGenerator.Generators.Fields;
using SbeSourceGenerator.Generators.Types;
using SbeSourceGenerator.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Generates code for SBE type definitions (types, enums, sets, composites).
    /// </summary>
    internal class TypesCodeGenerator : ICodeGenerator
    {
        public IEnumerable<(string name, string content)> Generate(string ns, XmlDocument xmlDocument, SchemaContext context)
        {
            var typeNodes = xmlDocument.SelectNodes("//types/*");
            foreach (XmlElement typeNode in typeNodes)
            {
                var generatedType = typeNode.Name switch
                {
                    "composite" => GenerateComposite(ns, typeNode, context),
                    "enum" => GenerateEnum(ns, typeNode, context),
                    "type" => GenerateType(ns, typeNode, context),
                    "set" => GenerateSet(ns, typeNode, context),
                    _ => Enumerable.Empty<(string name, string content)>()
                };
                foreach (var item in generatedType)
                    yield return item;
            }
        }

        private static IEnumerable<(string name, string content)> GenerateSet(string ns, XmlElement typeNode, SchemaContext context)
        {
            var enumDto = SchemaParser.ParseEnum(typeNode);
            
            var generator = new EnumFlagsDefinition(
                ns,
                enumDto.Name.FirstCharToUpper(),
                enumDto.Description,
                ToNativeType(enumDto.EncodingType),
                GetTypeLength(ToNativeType(enumDto.EncodingType), context),
                enumDto.Choices
                    .Select(choice => new EnumFieldDefinition(
                        choice.Name,
                        choice.Description,
                        choice.InnerText
                    ))
                    .ToList()
            );
            if (generator is IBlittable blittableType)
                context.CustomTypeLengths[enumDto.Name] = blittableType.Length;
            StringBuilder sb = new StringBuilder();
            generator.AppendFileContent(sb);
            yield return ($"{ns}\\Sets\\{enumDto.Name}", sb.ToString());
        }

        private static IEnumerable<(string name, string content)> GenerateType(string ns, XmlElement typeNode, SchemaContext context)
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
                            GetTypeLength(typeDto.PrimitiveType, context)
                        ),
                        (_, _) => new TypeDefinition(
                            ns,
                            typeDto.Name,
                            typeDto.Description,
                            ToNativeType(typeDto.PrimitiveType),
                            typeDto.SemanticType,
                            GetTypeLength(typeDto.PrimitiveType, context)
                        )
                    };
                if (generator is ConstantTypeDefinition constantType)
                    context.CustomConstantTypes[constantType.Name] = 0;
                if (generator is IBlittable blittableType)
                    context.CustomTypeLengths[typeDto.Name] = blittableType.Length;
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

        private static IEnumerable<(string name, string content)> GenerateEnum(string ns, XmlElement typeNode, SchemaContext context)
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
                context.CustomTypeLengths[enumDto.Name] = blittableType.Length;
            if (generator is EnumDefinition enumDefinition)
                context.EnumPrimitiveTypes[enumDefinition.Name] = enumDefinition.EncodingType;
            StringBuilder sb = new StringBuilder();
            generator.AppendFileContent(sb);
            yield return ($"{ns}\\Enums\\{enumDto.Name.FirstCharToUpper()}", sb.ToString());
        }

        private static IEnumerable<(string name, string content)> GenerateComposite(string ns, XmlElement typeNode, SchemaContext context)
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
                            GetTypeLength(ToNativeType(field.PrimitiveType), context),
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
                            GetTypeLength(ToNativeType(field.PrimitiveType), context)
                        )
                    }
                    ))
                    .ToList()
            );
            if (generator.Fields.All(f => f is IBlittable))
                context.CustomTypeLengths[compositeDto.Name] = generator.Fields.SumFieldLength();
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

        private static int GetTypeLength(string type, SchemaContext context)
        {
            int length;
            if (!TypesCatalog.PrimitiveTypeLengths.TryGetValue(ToNativeType(type), out length)
                && !context.CustomTypeLengths.TryGetValue(type, out length))
                throw new ArgumentException($"Could not get type {type} length");
            return length;
        }
    }
}
