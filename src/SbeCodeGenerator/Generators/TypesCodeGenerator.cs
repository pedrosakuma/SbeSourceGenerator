using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Diagnostics;
using SbeSourceGenerator.Helpers;
using SbeSourceGenerator.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Generates code for SBE type definitions (types, enums, sets, composites).
    /// </summary>
    internal class TypesCodeGenerator : ICodeGenerator
    {
        public IEnumerable<(string name, string content)> Generate(string ns, ParsedSchema schema, SchemaContext context, SourceProductionContext sourceContext)
        {
            // Types, enums, and sets must be processed before composites
            // because composites may reference them via <ref> fields.
            foreach (var typeDto in schema.Types)
            {
                foreach (var item in GenerateType(ns, typeDto, context, sourceContext))
                    yield return item;
            }
            foreach (var enumDto in schema.Enums)
            {
                foreach (var item in GenerateEnum(ns, enumDto, context, sourceContext))
                    yield return item;
            }
            foreach (var setDto in schema.Sets)
            {
                foreach (var item in GenerateSet(ns, setDto, context, sourceContext))
                    yield return item;
            }
            foreach (var compositeDto in schema.Composites)
            {
                foreach (var item in GenerateComposite(ns, compositeDto, context, sourceContext))
                    yield return item;
            }
        }

        private static IEnumerable<(string name, string content)> GenerateSet(string ns, SchemaEnumDto enumDto, SchemaContext context, SourceProductionContext sourceContext)
        {
            var generatedName = TypeResolverHelper.RegisterGeneratedTypeName(context, enumDto.Name, sourceContext);
            var encodingTranslated = TypeTranslator.Translate(enumDto.EncodingType);
            int maxBitPosition = TypesCatalog.GetPrimitiveLength(encodingTranslated.PrimitiveType) * 8 - 1;

            var validChoices = enumDto.Choices
                .Select(choice =>
                {
                    var parsedValue = XmlParsingHelpers.ParseEnumFlagValue(choice.InnerText, choice.Name, sourceContext);
                    return new { choice, parsedValue };
                })
                .Where(x =>
                {
                    if (x.parsedValue.HasValue && x.parsedValue.Value > maxBitPosition)
                    {
                        if (sourceContext.CancellationToken != default)
                        {
                            sourceContext.ReportDiagnostic(Diagnostic.Create(
                                SbeDiagnostics.SetChoiceExceedsBitWidth,
                                Location.None,
                                x.choice.Name,
                                enumDto.Name,
                                x.parsedValue.Value,
                                maxBitPosition,
                                enumDto.EncodingType));
                        }
                        return false;
                    }
                    return true;
                })
                .Select(x => new EnumFieldDefinition(
                    TypeTranslator.NormalizeName(x.choice.Name),
                    x.choice.Description,
                    x.parsedValue?.ToString() ?? "0",
                    x.choice.SinceVersion,
                    x.choice.Deprecated
                ))
                .ToList();

            var generator = new EnumFlagsDefinition(
                ns,
                generatedName,
                enumDto.Description,
                encodingTranslated.PrimitiveType,
                TypeResolverHelper.GetTypeLength(encodingTranslated.PrimitiveType, context),
                validChoices
            );
            if (generator is IBlittable blittableType)
                context.CustomTypeLengths[enumDto.Name] = blittableType.Length;
            StringBuilder sb = new StringBuilder(1024);
            generator.AppendFileContent(sb);
            yield return (context.CreateHintName(ns, "Sets", generatedName), sb.ToString());
        }

        private static IEnumerable<(string name, string content)> GenerateType(string ns, SchemaTypeDto typeDto, SchemaContext context, SourceProductionContext sourceContext)
        {
            var primitiveTranslated = TypeTranslator.Translate(typeDto.PrimitiveType);

            if (!TypeTranslator.IsPrimitive(typeDto.Name))
            {
                var generatedName = TypeResolverHelper.RegisterGeneratedTypeName(context, typeDto.Name, sourceContext);
                int lengthValue = 0;
                if (!string.IsNullOrEmpty(typeDto.Length))
                {
                    if (!int.TryParse(typeDto.Length, out lengthValue))
                    {
                        if (sourceContext.CancellationToken != default)
                        {
                            sourceContext.ReportDiagnostic(Diagnostic.Create(
                                SbeDiagnostics.InvalidIntegerAttribute,
                                Location.None,
                                "length",
                                typeDto.Length,
                                "type"));
                        }
                    }
                }

                var nativeType = primitiveTranslated.PrimitiveType;
                var generator = (nativeType, typeDto.Presence) switch
                {
                    (_, "constant") => new ConstantTypeDefinition(
                        ns,
                        generatedName,
                        typeDto.Description,
                        TypeResolverHelper.ResolveTypeName(nativeType, context),
                        typeDto.SemanticType,
                        typeDto.Length,
                        typeDto.InnerText
                    ),
                    ("char", _) => (IFileContentGenerator)new FixedSizeCharTypeDefinition(
                        ns,
                        generatedName,
                        typeDto.Description,
                        lengthValue,
                        typeDto.CharacterEncoding
                    ),
                    (_, "optional") => new OptionalTypeDefinition(
                        ns,
                        generatedName,
                        typeDto.Description,
                        TypeResolverHelper.ResolveTypeName(nativeType, context),
                        typeDto.SemanticType,
                        typeDto.NullValue,
                        TypeResolverHelper.GetTypeLength(nativeType, context)
                    ),
                    _ => new TypeDefinition(
                        ns,
                        generatedName,
                        typeDto.Description,
                        TypeResolverHelper.ResolveTypeName(nativeType, context),
                        typeDto.SemanticType,
                        TypeResolverHelper.GetTypeLength(nativeType, context)
                    )
                };
                if (generator is ConstantTypeDefinition)
                {
                    context.CustomConstantTypes[typeDto.Name] = 0;
                    context.ConstantTypeInfo[typeDto.Name] = (TypeResolverHelper.ResolveTypeName(nativeType, context), typeDto.InnerText);
                }
                if (generator is OptionalTypeDefinition)
                {
                    context.OptionalTypes[typeDto.Name] = (nativeType, typeDto.NullValue);
                    var resolvedType = TypeResolverHelper.ResolveTypeName(nativeType, context);
                    if (string.IsNullOrEmpty(typeDto.NullValue) && !TypesCatalog.HasNullValue(resolvedType)
                        && sourceContext.CancellationToken != default)
                    {
                        sourceContext.ReportDiagnostic(Diagnostic.Create(
                            SbeDiagnostics.UnknownPrimitiveTypeFallback,
                            Location.None,
                            resolvedType, "null sentinel", typeDto.Name));
                    }
                }
                if (generator is TypeDefinition)
                {
                    context.TypePrimitiveMapping[typeDto.Name] = TypeResolverHelper.ResolveTypeName(nativeType, context);
                }
                if (generator is IBlittable blittableType)
                    context.CustomTypeLengths[typeDto.Name] = blittableType.Length;
                StringBuilder sb = new StringBuilder(1024);
                generator.AppendFileContent(sb);
                yield return (context.CreateHintName(ns, "Types", generatedName), sb.ToString());

                // Detect LocalMktDate pattern on simple types
                if (string.Equals(typeDto.SemanticType, "LocalMktDate", System.StringComparison.OrdinalIgnoreCase))
                {
                    var dateHelper = generator is OptionalTypeDefinition
                        ? DateHelperDefinition.LocalMktDateOptional(ns, generatedName)
                        : DateHelperDefinition.LocalMktDate(ns, generatedName);
                    sb.Clear();
                    dateHelper.AppendFileContent(sb);
                    yield return (context.CreateHintName(ns, "Types", generatedName + ".ToDateOnly"), sb.ToString());
                }
            }
        }

        private static IEnumerable<(string name, string content)> GenerateEnum(string ns, SchemaEnumDto enumDto, SchemaContext context, SourceProductionContext sourceContext)
        {
            var generatedName = TypeResolverHelper.RegisterGeneratedTypeName(context, enumDto.Name, sourceContext);
            var encodingTranslated = TypeTranslator.Translate(enumDto.EncodingType);

            if (!TypesCatalog.HasPrimitiveLength(encodingTranslated.PrimitiveType) && sourceContext.CancellationToken != default)
            {
                sourceContext.ReportDiagnostic(Diagnostic.Create(
                    SbeDiagnostics.UnknownPrimitiveTypeFallback,
                    Location.None,
                    encodingTranslated.PrimitiveType, "length", enumDto.Name));
            }

            var generator = encodingTranslated.IsNullableEncoding switch
            {
                true => new NullableEnumDefinition(
                    ns,
                    generatedName,
                    enumDto.Description,
                    encodingTranslated.PrimitiveType,
                    enumDto.SemanticType,
                    TypesCatalog.GetPrimitiveLength(encodingTranslated.PrimitiveType),
                    enumDto.Choices
                        .Select(choice => new EnumFieldDefinition(
                            TypeTranslator.NormalizeName(choice.Name),
                            choice.Description,
                            choice.InnerText,
                            choice.SinceVersion,
                            choice.Deprecated
                        ))
                        .ToList()
                ),
                _ => new EnumDefinition(
                    ns,
                    generatedName,
                    enumDto.Description,
                    encodingTranslated.PrimitiveType,
                    enumDto.SemanticType,
                    TypesCatalog.GetPrimitiveLength(encodingTranslated.PrimitiveType),
                    enumDto.Choices
                        .Select(choice => new EnumFieldDefinition(
                            TypeTranslator.NormalizeName(choice.Name),
                            choice.Description,
                            choice.InnerText,
                            choice.SinceVersion,
                            choice.Deprecated
                        ))
                        .ToList()
                )
            };
            if (generator is IBlittable blittableType)
                context.CustomTypeLengths[enumDto.Name] = blittableType.Length;
            if (generator is EnumDefinition enumDefinition)
                context.EnumPrimitiveTypes[enumDto.Name] = enumDefinition.EncodingType;
            StringBuilder sb = new StringBuilder(1024);
            generator.AppendFileContent(sb);
            yield return (context.CreateHintName(ns, "Enums", generatedName), sb.ToString());
        }

        private static IEnumerable<(string name, string content)> GenerateComposite(string ns, SchemaCompositeDto compositeDto, SchemaContext context, SourceProductionContext sourceContext)
        {
            // Recursively generate nested composites first so their types are registered
            if (compositeDto.NestedComposites != null)
            {
                foreach (var nested in compositeDto.NestedComposites)
                {
                    foreach (var result in GenerateComposite(ns, nested, context, sourceContext))
                        yield return result;
                }
            }

            // Generate nested enums
            if (compositeDto.NestedEnums != null)
            {
                foreach (var nestedEnum in compositeDto.NestedEnums)
                {
                    foreach (var result in GenerateEnum(ns, nestedEnum, context, sourceContext))
                        yield return result;
                }
            }

            var generatedName = TypeResolverHelper.RegisterGeneratedTypeName(context, compositeDto.Name, sourceContext);

            // Separate ref fields from primitive fields
            var refFields = compositeDto.Fields
                .Where(f => string.IsNullOrEmpty(f.PrimitiveType) && !string.IsNullOrEmpty(f.Type))
                .ToList();
            var primitiveFields = compositeDto.Fields
                .Where(f => !string.IsNullOrEmpty(f.PrimitiveType))
                .ToList();

            // Pre-translate primitive field types
            var fieldTranslations = primitiveFields
                .Select(f => new { Field = f, Translation = TypeTranslator.Translate(f.PrimitiveType) })
                .ToList();

            foreach (var ft in fieldTranslations)
            {
                context.CompositeFieldTypes[$"{compositeDto.Name}.{ft.Field.Name}"] = TypeResolverHelper.ResolveTypeName(ft.Translation.PrimitiveType, context);

                if (ft.Field.Presence == "optional" && string.IsNullOrEmpty(ft.Field.NullValue))
                {
                    var resolvedType = TypeResolverHelper.ResolveTypeName(ft.Translation.PrimitiveType, context);
                    if (!TypesCatalog.HasNullValue(resolvedType) && sourceContext.CancellationToken != default)
                    {
                        sourceContext.ReportDiagnostic(Diagnostic.Create(
                            SbeDiagnostics.UnknownPrimitiveTypeFallback,
                            Location.None,
                            resolvedType, "null sentinel", $"{compositeDto.Name}.{ft.Field.Name}"));
                    }
                }
            }

            // Register ref fields in CompositeFieldTypes
            foreach (var rf in refFields)
            {
                var resolvedRefType = TypeResolverHelper.ResolveTypeName(rf.Type, context);
                context.CompositeFieldTypes[$"{compositeDto.Name}.{rf.Name}"] = resolvedRefType;
            }

            // Generate InlineArray types for char fields with length > 1
            var charArrayTypes = new Dictionary<string, (string GeneratedName, int Length)>();
            foreach (var ft in fieldTranslations)
            {
                if (ft.Translation.PrimitiveType == "char" && ft.Field.Presence != "constant"
                    && int.TryParse(ft.Field.Length, out var charLen) && charLen > 1)
                {
                    var charTypeName = TypeResolverHelper.RegisterGeneratedTypeName(context, ft.Field.Name, sourceContext);
                    var charTypeDef = new FixedSizeCharTypeDefinition(ns, charTypeName, ft.Field.Description, charLen, ft.Field.CharacterEncoding);
                    context.CustomTypeLengths[ft.Field.Name] = charLen;
                    context.StructTypeNames.Add(ft.Field.Name);
                    charArrayTypes[ft.Field.Name] = (charTypeName, charLen);
                    var charSb = new StringBuilder(256);
                    charTypeDef.AppendFileContent(charSb);
                    yield return (context.CreateHintName(ns, "Types", charTypeName), charSb.ToString());
                }
            }

            // Build field definitions preserving original order
            var fields = new List<IFileContentGenerator>(compositeDto.Fields.Count);
            foreach (var field in compositeDto.Fields)
            {
                if (!string.IsNullOrEmpty(field.PrimitiveType))
                {
                    var ft = fieldTranslations.FirstOrDefault(x => x.Field == field);
                    if (ft == null) continue;

                    // Char array fields use the generated InlineArray type
                    if (charArrayTypes.TryGetValue(ft.Field.Name, out var charArrayInfo))
                    {
                        fields.Add(new CompositeRefFieldDefinition(
                            TypeTranslator.NormalizeName(ft.Field.Name),
                            ft.Field.Description,
                            charArrayInfo.GeneratedName,
                            charArrayInfo.Length
                        ));
                    }
                    else
                    {
                        fields.Add((ft.Field.Presence, ft.Field.Length) switch
                        {
                            ("constant", _) => new ConstantTypeFieldDefinition(
                                TypeTranslator.NormalizeName(ft.Field.Name),
                                ft.Field.Description,
                                TypeResolverHelper.ResolveTypeName(ft.Translation.PrimitiveType, context),
                                InsertQuotationsIfNeeded(ft.Field.InnerText, ft.Field.PrimitiveType, ft.Field.Length),
                                TypeResolverHelper.NormalizeValueRef(ft.Field.ValueRef, context)
                            ),
                            ("optional", _) => new NullableValueFieldDefinition(
                                TypeTranslator.NormalizeName(ft.Field.Name),
                                ft.Field.Description,
                                TypeResolverHelper.ResolveTypeName(ft.Translation.PrimitiveType, context),
                                TypeResolverHelper.GetTypeLength(ft.Translation.PrimitiveType, context),
                                ft.Field.NullValue == "" ? null : ft.Field.NullValue,
                                context.EndianConversion
                            ),
                            ("", "0") => new ArrayFieldDefinition(
                                TypeTranslator.NormalizeName(ft.Field.Name),
                                ft.Field.Description,
                                "byte"
                            ),
                            _ => (IFileContentGenerator)new ValueFieldDefinition(
                                TypeTranslator.NormalizeName(ft.Field.Name),
                                ft.Field.Description,
                                TypeResolverHelper.ResolveTypeName(ft.Translation.PrimitiveType, context),
                                TypeResolverHelper.GetTypeLength(ft.Translation.PrimitiveType, context),
                                context.EndianConversion
                            )
                        });
                    }
                }
                else if (!string.IsNullOrEmpty(field.Type))
                {
                    var resolvedRefType = TypeResolverHelper.ResolveTypeName(field.Type, context);
                    var refLength = context.CustomTypeLengths.TryGetValue(field.Type, out var len) ? len : 0;
                    fields.Add(new CompositeRefFieldDefinition(
                        TypeTranslator.NormalizeName(field.Name),
                        field.Description,
                        resolvedRefType,
                        refLength
                    ));
                }
            }

            var generator = new CompositeDefinition(
                ns,
                generatedName,
                compositeDto.Description,
                compositeDto.SemanticType,
                fields,
                context.EndianConversion
            );
            if (generator.Fields.All(f => f is IBlittable))
                context.CustomTypeLengths[compositeDto.Name] = generator.Fields.SumFieldLength();
            context.StructTypeNames.Add(compositeDto.Name);
            StringBuilder sb = new StringBuilder(1024);
            generator.AppendFileContent(sb);
            yield return (context.CreateHintName(ns, "Composites", generatedName), sb.ToString());

            // Detect decimal pattern: mantissa field + constant exponent field
            var decimalHelper = TryCreateDecimalHelper(ns, generatedName, compositeDto);
            if (decimalHelper != null)
            {
                sb.Clear();
                decimalHelper.AppendFileContent(sb);
                yield return (context.CreateHintName(ns, "Composites", generatedName + ".ToDecimal"), sb.ToString());
            }

            // Detect timestamp pattern: time field + constant unit field
            var timestampHelper = TryCreateTimestampHelper(ns, generatedName, compositeDto);
            if (timestampHelper != null)
            {
                sb.Clear();
                timestampHelper.AppendFileContent(sb);
                yield return (context.CreateHintName(ns, "Composites", generatedName + ".ToDateTime"), sb.ToString());
            }

            // Detect MonthYear pattern: semanticType="MonthYear" with year/month fields
            var monthYearHelper = TryCreateMonthYearHelper(ns, generatedName, compositeDto);
            if (monthYearHelper != null)
            {
                sb.Clear();
                monthYearHelper.AppendFileContent(sb);
                yield return (context.CreateHintName(ns, "Composites", generatedName + ".ToDateOnly"), sb.ToString());
            }
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

        private static DecimalHelperDefinition? TryCreateDecimalHelper(string ns, string generatedName, SchemaCompositeDto compositeDto)
        {
            SchemaFieldDto mantissaField = null;
            SchemaFieldDto exponentField = null;

            foreach (var field in compositeDto.Fields)
            {
                if (string.Equals(field.Name, "mantissa", System.StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(field.PrimitiveType))
                    mantissaField = field;
                else if (string.Equals(field.Name, "exponent", System.StringComparison.OrdinalIgnoreCase)
                    && field.Presence == "constant"
                    && !string.IsNullOrEmpty(field.InnerText))
                    exponentField = field;
            }

            if (mantissaField == null || exponentField == null)
                return null;

            if (!int.TryParse(exponentField.InnerText, out var exponent))
                return null;

            bool isOptional = mantissaField.Presence == "optional";
            return new DecimalHelperDefinition(ns, generatedName, compositeDto.Description, exponent, isOptional);
        }

        private static TimestampHelperDefinition? TryCreateTimestampHelper(string ns, string generatedName, SchemaCompositeDto compositeDto)
        {
            SchemaFieldDto? timeField = null;
            SchemaFieldDto? unitField = null;

            foreach (var field in compositeDto.Fields)
            {
                if (string.Equals(field.Name, "time", System.StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(field.PrimitiveType))
                    timeField = field;
                else if (string.Equals(field.Name, "unit", System.StringComparison.OrdinalIgnoreCase)
                    && field.Presence == "constant")
                    unitField = field;
            }

            if (timeField == null || unitField == null)
                return null;

            var unit = ResolveTimeUnit(unitField);
            if (unit == null)
                return null;

            bool isOptional = timeField.Presence == "optional";
            return new TimestampHelperDefinition(ns, generatedName, compositeDto.Description, unit.Value, isOptional);
        }

        private static TimeUnitKind? ResolveTimeUnit(SchemaFieldDto unitField)
        {
            // Try valueRef first (e.g., "TimeUnit.nanosecond")
            var valueRef = unitField.ValueRef ?? "";
            var dotIndex = valueRef.LastIndexOf('.');
            var valueName = dotIndex >= 0 ? valueRef.Substring(dotIndex + 1) : valueRef;

            if (!string.IsNullOrEmpty(valueName))
            {
                if (valueName.IndexOf("nanosecond", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return TimeUnitKind.Nanosecond;
                if (valueName.IndexOf("microsecond", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return TimeUnitKind.Microsecond;
                if (valueName.IndexOf("millisecond", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return TimeUnitKind.Millisecond;
                if (valueName.IndexOf("second", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return TimeUnitKind.Second;
            }

            return null;
        }

        private static DateHelperDefinition? TryCreateMonthYearHelper(string ns, string generatedName, SchemaCompositeDto compositeDto)
        {
            if (!string.Equals(compositeDto.SemanticType, "MonthYear", System.StringComparison.OrdinalIgnoreCase))
                return null;

            SchemaFieldDto? yearField = null;
            SchemaFieldDto? monthField = null;
            bool hasDay = false;

            foreach (var field in compositeDto.Fields)
            {
                if (string.Equals(field.Name, "year", System.StringComparison.OrdinalIgnoreCase))
                    yearField = field;
                else if (string.Equals(field.Name, "month", System.StringComparison.OrdinalIgnoreCase))
                    monthField = field;
                else if (string.Equals(field.Name, "day", System.StringComparison.OrdinalIgnoreCase))
                    hasDay = true;
            }

            if (yearField == null || monthField == null)
                return null;

            return DateHelperDefinition.MonthYear(ns, generatedName,
                yearOptional: yearField.Presence == "optional",
                monthOptional: monthField.Presence == "optional",
                hasDay: hasDay);
        }
    }
}
