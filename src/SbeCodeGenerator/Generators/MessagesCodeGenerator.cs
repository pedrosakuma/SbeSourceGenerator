using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Diagnostics;
using SbeSourceGenerator.Generators.Fields;
using SbeSourceGenerator.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Generates code for SBE message definitions.
    /// </summary>
    public class MessagesCodeGenerator : ICodeGenerator
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
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
            nsmgr.AddNamespace("sbe", "http://fixprotocol.io/2016/sbe");
            var messageNodes = xmlDocument.SelectNodes("//sbe:message", nsmgr);
            foreach (XmlElement messageNode in messageNodes)
            {
                var messageDto = SchemaParser.ParseMessage(messageNode, context, sourceContext);

                var generatedMessageName = RegisterGeneratedTypeName(context, messageDto.Name);

                var versions = GetMessageVersions(messageDto, sourceContext);
                var baseNamespace = StripSchemaVersion(ns);

                foreach (var version in versions)
                {
                    var versionNamespace = GetVersionNamespace(baseNamespace, ns, version);
                    var fieldsForVersion = GetFieldsForVersion(messageDto.Fields, version, sourceContext, context);

                    var generator = new MessageDefinition(
                        versionNamespace,
                        ns,
                        generatedMessageName,
                        messageDto.Id,
                        $"{messageDto.Description} (Version {version})",
                        messageDto.SemanticType,
                        messageDto.Deprecated,
                        fieldsForVersion,
                        messageDto.Constants
                            .Select(constant => (IFileContentGenerator)
                                new ConstantMessageFieldDefinition(
                                    TypeTranslator.NormalizeName(constant.Name),
                                    constant.Id,
                                    ResolveTypeName(TypeTranslator.Translate(constant.Type).PrimitiveType, context),
                                    constant.Description != "" ? constant.Description : constant.Type,
                                    NormalizeValueRef(constant.ValueRef, context)
                                )
                            )
                            .ToList(),
                        messageDto.Groups
                            .Select(group =>
                            {
                                var groupName = RegisterGeneratedTypeName(context, group.Name);
                                var groupFields = group.Fields
                                    .Select(field =>
                                    {
                                        var translatedField = TypeTranslator.Translate(field.Type);
                                        var resolvedFieldType = ResolveTypeName(translatedField.PrimitiveType, context);
                                        var generatedFieldName = TypeTranslator.NormalizeName(field.Name);
                                        return (IFileContentGenerator)new MessageFieldDefinition(
                                            generatedFieldName,
                                            field.Id,
                                            resolvedFieldType,
                                            field.Description,
                                            ParseOffset(field.Offset, field.Name, sourceContext),
                                            GetTypeLength(field.Type, context),
                                            field.SinceVersion,
                                            field.Deprecated
                                        );
                                    })
                                    .ToList();

                                var groupConstants = group.Constants
                                    .Select(constant => (IFileContentGenerator)
                                        new ConstantMessageFieldDefinition(
                                            TypeTranslator.NormalizeName(constant.Name),
                                            constant.Id,
                                            ResolveTypeName(TypeTranslator.Translate(constant.Type).PrimitiveType, context),
                                            constant.Description,
                                            NormalizeValueRef(constant.ValueRef, context)
                                        )
                                    )
                                    .ToList();

                                var numInGroupType = ResolveTypeName(GetNumInGroupType(group.DimensionType, context, sourceContext), context);

                                return (IFileContentGenerator)new GroupDefinition(
                                    versionNamespace,
                                    groupName,
                                    group.Id,
                                    ResolveTypeName(group.DimensionType, context),
                                    group.Description,
                                    groupFields,
                                    groupConstants,
                                    numInGroupType
                                );
                            })
                            .ToList(),
                        messageDto.Data
                            .Select(data => (IFileContentGenerator)
                                new DataFieldDefinition(
                                    TypeTranslator.NormalizeName(data.Name),
                                    data.Id,
                                    ResolveTypeName(data.Type, context),
                                    data.Description
                                )
                            ).ToList()
                    );
                    StringBuilder sb = new StringBuilder();
                    generator.AppendFileContent(sb);
                    var targetNamespace = string.IsNullOrEmpty(versionNamespace) ? ns : versionNamespace;
                    var fileName = version == 0
                        ? context.CreateHintName(targetNamespace, "Messages", generator.Name)
                        : context.CreateHintName(targetNamespace, "Messages", $"{generator.Name}V{version}");
                    yield return (fileName, sb.ToString());
                }
            }
        }

        /// <summary>
        /// Determines all versions needed for a message based on sinceVersion attributes.
        /// Returns a list like [0, 1, 2] for a message with fields at versions 0, 1, and 2.
        /// </summary>
        private static List<int> GetMessageVersions(SchemaMessageDto messageDto, SourceProductionContext sourceContext)
        {
            var versions = new HashSet<int> { 0 }; // Always generate version 0

            foreach (var field in messageDto.Fields)
            {
                if (!string.IsNullOrEmpty(field.SinceVersion))
                {
                    if (int.TryParse(field.SinceVersion, out int sinceVersion))
                    {
                        for (int v = 0; v <= sinceVersion; v++)
                        {
                            versions.Add(v);
                        }
                    }
                    else if (sourceContext.CancellationToken != default)
                    {
                        sourceContext.ReportDiagnostic(Diagnostic.Create(
                            SbeDiagnostics.InvalidIntegerAttribute,
                            Location.None,
                            "sinceVersion",
                            field.SinceVersion,
                            field.Name));
                    }
                }
            }

            return versions.OrderBy(v => v).ToList();
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

        private static string GetVersionNamespace(string baseNamespace, string schemaNamespace, int version)
        {
            if (version == 0)
            {
                return string.IsNullOrEmpty(schemaNamespace) ? baseNamespace : schemaNamespace;
            }

            if (!string.IsNullOrEmpty(schemaNamespace))
            {
                return string.Concat(schemaNamespace, ".V", version);
            }

            if (!string.IsNullOrEmpty(baseNamespace))
            {
                return string.Concat(baseNamespace, ".V", version);
            }

            return $"V{version}";
        }

        /// <summary>
        /// Gets the fields that should be included for a specific version.
        /// Only includes fields where sinceVersion is less than or equal to version.
        /// </summary>
        private static List<IFileContentGenerator> GetFieldsForVersion(
            List<SchemaFieldDto> fields,
            int version,
            SourceProductionContext sourceContext,
            SchemaContext context)
        {
            return fields
                .Where(field =>
                {
                    if (string.IsNullOrEmpty(field.SinceVersion))
                        return true;

                    if (int.TryParse(field.SinceVersion, out int sinceVersion))
                        return sinceVersion <= version;

                    // sinceVersion is present but not a valid integer — already reported by GetMessageVersions
                    return true;
                })
                .Select(field =>
                {
                    // Check if this field is optional either by:
                    // 1. Having presence="optional" attribute
                    // 2. Using a type that is defined as optional (e.g., Int64NULL)
                    bool isOptional = field.Presence == "optional" || context.OptionalTypes.ContainsKey(field.Type);
                    var translated = TypeTranslator.Translate(field.Type);
                    var resolvedType = ResolveTypeName(translated.PrimitiveType, context);
                    var generatedFieldName = TypeTranslator.NormalizeName(field.Name);

                    return isOptional
                        ? new OptionalMessageFieldDefinition(
                            generatedFieldName,
                            field.Id,
                            resolvedType,
                            GetUnderlyingType(field.Type, context),
                            field.Description,
                            ParseOffset(field.Offset, field.Name, sourceContext),
                            GetTypeLength(field.Type, context),
                            field.SinceVersion,
                            field.Deprecated
                        )
                        : (IFileContentGenerator)new MessageFieldDefinition(
                            generatedFieldName,
                            field.Id,
                            resolvedType,
                            field.Description,
                            ParseOffset(field.Offset, field.Name, sourceContext),
                            GetTypeLength(field.Type, context),
                            field.SinceVersion,
                            field.Deprecated
                        );
                })
                .ToList();
        }

        private static string? GetUnderlyingType(string type, SchemaContext context)
        {
            // Check if it's an enum type first
            if (context.EnumPrimitiveTypes.TryGetValue(type, out string? underlyingType))
                return underlyingType;

            // Check if it's an optional type (e.g., Int64NULL)
            if (context.OptionalTypes.TryGetValue(type, out underlyingType))
                return underlyingType;

            return null;
        }

        private static int GetTypeLength(string type, SchemaContext context, SourceProductionContext sourceContext = default, string elementName = "")
        {
            int length;
            var translated = TypeTranslator.Translate(type).PrimitiveType;
            if (!TypesCatalog.PrimitiveTypeLengths.TryGetValue(translated, out length)
                && !context.CustomTypeLengths.TryGetValue(type, out length))
            {
                if (sourceContext.CancellationToken != default)
                {
                    sourceContext.ReportDiagnostic(Diagnostic.Create(
                        SbeDiagnostics.UnresolvedTypeReference,
                        Location.None,
                        type,
                        elementName));
                }
                return 0;
            }
            return length;
        }

        private static string GetNumInGroupType(string dimensionType, SchemaContext context, SourceProductionContext sourceContext = default)
        {
            var key = $"{dimensionType}.numInGroup";
            if (context.CompositeFieldTypes.TryGetValue(key, out string? numInGroupType))
            {
                return numInGroupType;
            }
            if (sourceContext.CancellationToken != default)
            {
                sourceContext.ReportDiagnostic(Diagnostic.Create(
                    SbeDiagnostics.UnsupportedConstruct,
                    Location.None,
                    "dimensionType",
                    dimensionType,
                    $"Composite type '{dimensionType}' not found. Falling back to ushort for numInGroup."));
            }
            return "ushort";
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
                    separator = "::";
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

        private static int? ParseOffset(string offset, string fieldName, SourceProductionContext sourceContext)
        {
            if (string.IsNullOrEmpty(offset))
                return null;

            if (int.TryParse(offset, out int result))
                return result;

            // Only report diagnostic if context has a valid CancellationToken (not default)
            if (sourceContext.CancellationToken != default)
            {
                // Report diagnostic for invalid offset
                sourceContext.ReportDiagnostic(Diagnostic.Create(
                    SbeDiagnostics.InvalidIntegerAttribute,
                    Location.None,
                    "offset",
                    offset,
                    fieldName));
            }

            return null;
        }
    }
}
