using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Generators.Types;
using SbeSourceGenerator.Helpers;
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
    public class TypesCodeGenerator : ICodeGenerator
    {
        private static readonly HashSet<string> DotNetPrimitiveTypes = new(StringComparer.Ordinal)
        {
            "bool",
            "byte",
            "sbyte",
            "short",
            "ushort",
            "int",
            "uint",
            "long",
            "ulong",
            "float",
            "double",
            "char",
            "string"
        };

        public IEnumerable<(string name, string content)> Generate(string ns, XmlDocument xmlDocument, SchemaContext context, SourceProductionContext sourceContext)
        {
            // Strip version suffix to use base namespace for types
            var baseNamespace = StripSchemaVersion(ns);
            
            var typeNodes = xmlDocument.SelectNodes("//types/*");
            foreach (XmlElement typeNode in typeNodes)
            {
                var generatedType = typeNode.Name switch
                {
                    "composite" => GenerateComposite(baseNamespace, typeNode, context, sourceContext),
                    "enum" => GenerateEnum(baseNamespace, typeNode, context, sourceContext),
                    "type" => GenerateType(baseNamespace, typeNode, context, sourceContext),
                    "set" => GenerateSet(baseNamespace, typeNode, context, sourceContext),
                    _ => Enumerable.Empty<(string name, string content)>()
                };
                foreach (var item in generatedType)
                    yield return item;
            }
        }

        private static IEnumerable<(string name, string content)> GenerateSet(string ns, XmlElement typeNode, SchemaContext context, SourceProductionContext sourceContext)
        {
            var enumDto = SchemaParser.ParseEnum(typeNode);
            var generatedName = RegisterGeneratedTypeName(context, enumDto.Name);
            var encodingTranslated = TypeTranslator.Translate(enumDto.EncodingType);

            var validChoices = enumDto.Choices
                .Select(choice =>
                {
                    var parsedValue = XmlParsingHelpers.ParseEnumFlagValue(choice.InnerText, choice.Name, sourceContext);
                    return new EnumFieldDefinition(
                        choice.Name,
                        choice.Description,
                        parsedValue?.ToString() ?? "0"
                    );
                })
                .ToList();

            var generator = new EnumFlagsDefinition(
                ns,
                generatedName,
                enumDto.Description,
                encodingTranslated.PrimitiveType,
                GetTypeLength(encodingTranslated.PrimitiveType, context),
                validChoices
            );
            if (generator is IBlittable blittableType)
                context.CustomTypeLengths[enumDto.Name] = blittableType.Length;
            StringBuilder sb = new StringBuilder();
            generator.AppendFileContent(sb);
            yield return (context.CreateHintName(ns, "Sets", generatedName), sb.ToString());
        }

        private static IEnumerable<(string name, string content)> GenerateType(string ns, XmlElement typeNode, SchemaContext context, SourceProductionContext sourceContext)
        {
            var typeDto = SchemaParser.ParseType(typeNode);
            var primitiveTranslated = TypeTranslator.Translate(typeDto.PrimitiveType);

            if (!TypeTranslator.IsPrimitive(typeDto.Name))
            {
                var generatedName = RegisterGeneratedTypeName(context, typeDto.Name);
                int lengthValue = 0;
                if (!string.IsNullOrEmpty(typeDto.Length))
                {
                    if (!int.TryParse(typeDto.Length, out lengthValue))
                        lengthValue = typeNode.GetIntAttributeOrDefault("length", 0, sourceContext);
                }

                var nativeType = primitiveTranslated.PrimitiveType;
                var generator = (nativeType, typeDto.Presence) switch
                {
                    (_, "constant") => new ConstantTypeDefinition(
                        ns,
                        generatedName,
                        typeDto.Description,
                        ResolveTypeName(nativeType, context),
                        typeDto.SemanticType,
                        typeDto.Length,
                        typeDto.InnerText
                    ),
                    ("char", _) => (IFileContentGenerator)new FixedSizeCharTypeDefinition(
                        ns,
                        generatedName,
                        typeDto.Description,
                        lengthValue
                    ),
                    (_, "optional") => new OptionalTypeDefinition(
                        ns,
                        generatedName,
                        typeDto.Description,
                        ResolveTypeName(nativeType, context),
                        typeDto.SemanticType,
                        typeDto.NullValue,
                        GetTypeLength(nativeType, context)
                    ),
                    _ => new TypeDefinition(
                        ns,
                        generatedName,
                        typeDto.Description,
                        ResolveTypeName(nativeType, context),
                        typeDto.SemanticType,
                        GetTypeLength(nativeType, context)
                    )
                };
                if (generator is ConstantTypeDefinition)
                    context.CustomConstantTypes[typeDto.Name] = 0;
                if (generator is OptionalTypeDefinition)
                    context.OptionalTypes[typeDto.Name] = nativeType;
                if (generator is IBlittable blittableType)
                    context.CustomTypeLengths[typeDto.Name] = blittableType.Length;
                StringBuilder sb = new StringBuilder();
                generator.AppendFileContent(sb);
                yield return (context.CreateHintName(ns, "Types", generatedName), sb.ToString());
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
                        yield return (context.CreateHintName(typeDefinition.Namespace, "Types", $"{typeDefinition.Name}.Semantic"), sb.ToString());
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
                        yield return (context.CreateHintName(optionalTypeDefinition.Namespace, "Types", $"{optionalTypeDefinition.Name}.Semantic"), sb.ToString());
                }
            }
        }

        private static IEnumerable<(string name, string content)> GenerateEnum(string ns, XmlElement typeNode, SchemaContext context, SourceProductionContext sourceContext)
        {
            var enumDto = SchemaParser.ParseEnum(typeNode);
            var generatedName = RegisterGeneratedTypeName(context, enumDto.Name);
            var encodingTranslated = TypeTranslator.Translate(enumDto.EncodingType);

            var generator = encodingTranslated.IsNullableEncoding switch
            {
                true => new NullableEnumDefinition(
                    ns,
                    generatedName,
                    enumDto.Description,
                    encodingTranslated.PrimitiveType,
                    enumDto.SemanticType,
                    TypesCatalog.PrimitiveTypeLengths[encodingTranslated.PrimitiveType],
                    enumDto.Choices
                        .Select(choice => new EnumFieldDefinition(
                            choice.Name,
                            choice.Description,
                            choice.InnerText
                        ))
                        .ToList()
                ),
                _ => new EnumDefinition(
                    ns,
                    generatedName,
                    enumDto.Description,
                    encodingTranslated.PrimitiveType,
                    enumDto.SemanticType,
                    TypesCatalog.PrimitiveTypeLengths[encodingTranslated.PrimitiveType],
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
                context.EnumPrimitiveTypes[enumDto.Name] = enumDefinition.EncodingType;
            StringBuilder sb = new StringBuilder();
            generator.AppendFileContent(sb);
            yield return (context.CreateHintName(ns, "Enums", generatedName), sb.ToString());
        }

        private static IEnumerable<(string name, string content)> GenerateComposite(string ns, XmlElement typeNode, SchemaContext context, SourceProductionContext sourceContext)
        {
            var compositeDto = SchemaParser.ParseComposite(typeNode);
            var generatedName = RegisterGeneratedTypeName(context, compositeDto.Name);

            // Pre-translate all field primitive types once to avoid repeated dictionary lookups
            var fieldTranslations = compositeDto.Fields
                .Select(f => new { Field = f, Translation = TypeTranslator.Translate(f.PrimitiveType) })
                .ToList();

            foreach (var ft in fieldTranslations)
            {
                context.CompositeFieldTypes[$"{compositeDto.Name}.{ft.Field.Name}"] = ResolveTypeName(ft.Translation.PrimitiveType, context);
            }

            var generator = new CompositeDefinition(
                ns,
                generatedName,
                compositeDto.Description,
                compositeDto.SemanticType,
                fieldTranslations
                    .Select(ft => (IFileContentGenerator)((ft.Field.Presence, ft.Field.Length) switch
                    {
                        ("constant", _) => new ConstantTypeFieldDefinition(
                            TypeTranslator.NormalizeName(ft.Field.Name),
                            ft.Field.Description,
                            ResolveTypeName(ft.Translation.PrimitiveType, context),
                            InsertQuotationsIfNeeded(ft.Field.InnerText, ft.Field.PrimitiveType, ft.Field.Length),
                            NormalizeValueRef(ft.Field.ValueRef, context)
                        ),
                        ("optional", _) => new NullableValueFieldDefinition(
                            TypeTranslator.NormalizeName(ft.Field.Name),
                            ft.Field.Description,
                            ResolveTypeName(ft.Translation.PrimitiveType, context),
                            GetTypeLength(ft.Translation.PrimitiveType, context),
                            ft.Field.NullValue == "" ? null : ft.Field.NullValue
                        ),
                        ("", "0") => new ArrayFieldDefinition(
                            TypeTranslator.NormalizeName(ft.Field.Name),
                            ft.Field.Description,
                            "byte"
                        ),
                        _ => new ValueFieldDefinition(
                            TypeTranslator.NormalizeName(ft.Field.Name),
                            ft.Field.Description,
                            ResolveTypeName(ft.Translation.PrimitiveType, context),
                            GetTypeLength(ft.Translation.PrimitiveType, context)
                        )
                    }))
                    .ToList()
            );
            if (generator.Fields.All(f => f is IBlittable))
                context.CustomTypeLengths[compositeDto.Name] = generator.Fields.SumFieldLength();
            StringBuilder sb = new StringBuilder();
            generator.AppendFileContent(sb);
            yield return (context.CreateHintName(ns, "Composites", generatedName), sb.ToString());
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
                yield return (context.CreateHintName(ns, "Composites", $"{generatedName}.Semantic"), sb.ToString());
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

        private static int GetTypeLength(string type, SchemaContext context)
        {
            int length;
            if (!TypesCatalog.PrimitiveTypeLengths.TryGetValue(type, out length)
                && !context.CustomTypeLengths.TryGetValue(type, out length)
                && !TypesCatalog.PrimitiveTypeLengths.TryGetValue(TypeTranslator.Translate(type).PrimitiveType, out length))
                throw new ArgumentException($"Could not get type {type} length");
            return length;
        }

        private static string RegisterGeneratedTypeName(SchemaContext context, string originalName)
        {
            if (string.IsNullOrEmpty(originalName))
                return originalName;

            var normalized = TypeTranslator.NormalizeName(originalName);
            context.GeneratedTypeNames[originalName] = normalized;
            return normalized;
        }

        private static string ResolveTypeName(string typeName, SchemaContext context)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeName;

            if (IsDotNetPrimitive(typeName))
                return typeName;

            if (context.GeneratedTypeNames.TryGetValue(typeName, out var generated))
                return generated;

            return TypeTranslator.NormalizeName(typeName);
        }

        private static bool IsDotNetPrimitive(string typeName) => DotNetPrimitiveTypes.Contains(typeName);

        private static string NormalizeValueRef(string valueRef, SchemaContext context)
        {
            if (string.IsNullOrWhiteSpace(valueRef))
                return valueRef;

            int separatorIndex = valueRef.IndexOf('.');
            string separator = ".";

            if (separatorIndex < 0)
            {
                separatorIndex = valueRef.IndexOf("::", StringComparison.Ordinal);
                if (separatorIndex >= 0)
                {
                    separator = "::";
                }
            }

            if (separatorIndex < 0)
                return ResolveTypeName(valueRef, context);

            var typePart = valueRef.Substring(0, separatorIndex);
            var remainder = valueRef.Substring(separatorIndex + separator.Length);
            var normalizedType = ResolveTypeName(typePart, context);

            if (remainder.Length == 0)
                return normalizedType;

            return string.Concat(normalizedType, separator, remainder);
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
