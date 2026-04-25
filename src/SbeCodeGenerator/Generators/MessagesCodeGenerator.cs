using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Diagnostics;
using SbeSourceGenerator.Generators.Fields;
using SbeSourceGenerator.Schema;
using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Generates code for SBE message definitions.
    /// </summary>
    internal class MessagesCodeGenerator : ICodeGenerator
    {
        public IEnumerable<(string name, string content)> Generate(string ns, ParsedSchema schema, SchemaContext context, SourceProductionContext sourceContext)
        {
            int schemaVersion = -1;
            if (!string.IsNullOrEmpty(schema.Version))
                int.TryParse(schema.Version, out schemaVersion);

            foreach (var messageDto in schema.Messages)
            {
                var generatedMessageName = TypeResolverHelper.RegisterGeneratedTypeName(context, messageDto.Name, sourceContext);

                var versions = GetMessageVersions(messageDto, schemaVersion, sourceContext);
                var baseNamespace = StripSchemaVersion(ns);

                // Issue #146: collect (version, namespace, blockLength) for emitting a VersionMap when message has multiple versions.
                var versionMapEntries = new List<(int Version, string Namespace, int BlockLength)>(versions.Count);

                foreach (var version in versions)
                {
                    var versionNamespace = GetVersionNamespace(baseNamespace, ns, version);
                    var fieldsForVersion = GetFieldsForVersion(messageDto.Fields, version, sourceContext, context);

                    string headerTypeName = "MessageHeader";
                    if (context.GeneratedTypeNames.TryGetValue(context.HeaderType, out var resolvedHeaderName))
                        headerTypeName = resolvedHeaderName;

                    var generator = new MessageDefinition(
                        versionNamespace,
                        ns,
                        generatedMessageName,
                        messageDto.Id,
                        $"{messageDto.Description} (Version {version})",
                        messageDto.SemanticType,
                        messageDto.Deprecated,
                        fieldsForVersion,
                        BuildConstants(messageDto.Constants, context),
                        BuildGroups(messageDto.Groups, versionNamespace, context, sourceContext),
                        GetDataForVersion(messageDto.Data, version, context),
                        messageDto.BlockLength,
                        context.EndianConversion,
                        schema.Id,
                        version.ToString(),
                        headerTypeName
                    );
                    int estimatedCapacity = 2048 + fieldsForVersion.Count * 256
                        + messageDto.Groups.Count * 1024 + messageDto.Data.Count * 512;
                    StringBuilder sb = new StringBuilder(estimatedCapacity);
                    generator.AppendFileContent(sb);
                    var targetNamespace = string.IsNullOrEmpty(versionNamespace) ? ns : versionNamespace;
                    var fileName = version == 0
                        ? context.CreateHintName(targetNamespace, "Messages", generator.Name)
                        : context.CreateHintName(targetNamespace, "Messages", $"{generator.Name}V{version}");
                    yield return (fileName, sb.ToString());

                    // Compute this version's wire BLOCK_LENGTH using the version's *own* field set
                    // (sinceVersion <= version), NOT the V0 effectiveVersion override. This is what
                    // a producer at that version would have written on the wire — the basis for the version map.
                    int blockLength;
                    if (!string.IsNullOrEmpty(messageDto.BlockLength)
                        && int.TryParse(messageDto.BlockLength, out var overridden))
                    {
                        blockLength = overridden;
                    }
                    else
                    {
                        var ownFields = GetFieldsForVersion(messageDto.Fields, version, sourceContext, context);
                        blockLength = ownFields.SumFieldLength();
                    }
                    versionMapEntries.Add((version, targetNamespace, blockLength));
                }

                // Issue #146: emit a VersionMap helper when the message has multiple versions
                // OR when wire blockLength may differ from the V0 layout (schema-evolved messages).
                if (versionMapEntries.Count > 1)
                {
                    var (mapFileName, mapContent) = BuildVersionMap(generatedMessageName, baseNamespace, ns, versionMapEntries, context);
                    yield return (mapFileName, mapContent);
                }
            }
        }

        private static (string fileName, string content) BuildVersionMap(
            string messageName, string baseNamespace, string defaultNamespace,
            List<(int Version, string Namespace, int BlockLength)> entries, SchemaContext context)
        {
            // Emit the map in the V0 namespace (the canonical/base namespace).
            var v0 = entries.Find(e => e.Version == 0);
            var targetNs = !string.IsNullOrEmpty(v0.Namespace) ? v0.Namespace : defaultNamespace;

            var sb = new StringBuilder(1024);
            sb.AppendLine("// <auto-generated />");
            sb.Append("namespace ").Append(targetNs).AppendLine(";");
            sb.AppendLine();
            sb.Append("/// <summary>Maps a wire blockLength to the corresponding version of <see cref=\"")
              .Append(messageName).AppendLine("\"/>.</summary>");
            sb.Append("/// <remarks>Issue #146: zero-allocation lookup. The array is small (one entry per version) so a linear scan is faster than a dictionary.</remarks>");
            sb.AppendLine();
            sb.Append("public static class ").Append(messageName).AppendLine("VersionMap");
            sb.AppendLine("{");
            sb.AppendLine("\t/// <summary>(BlockLength, Version) tuples in declaration order.</summary>");
            sb.AppendLine("\tpublic static readonly (int BlockLength, int Version)[] Entries = new (int, int)[]");
            sb.AppendLine("\t{");
            foreach (var entry in entries)
            {
                sb.Append("\t\t(").Append(entry.BlockLength).Append(", ").Append(entry.Version).AppendLine("),");
            }
            sb.AppendLine("\t};");
            sb.AppendLine();
            sb.AppendLine("\t/// <summary>Looks up the version corresponding to a wire blockLength. Returns false if no exact match.</summary>");
            sb.AppendLine("\t[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine("\tpublic static bool TryGetVersion(int blockLength, out int version)");
            sb.AppendLine("\t{");
            sb.AppendLine("\t\tvar entries = Entries;");
            sb.AppendLine("\t\tfor (int i = 0; i < entries.Length; i++)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tif (entries[i].BlockLength == blockLength) { version = entries[i].Version; return true; }");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t\tversion = -1;");
            sb.AppendLine("\t\treturn false;");
            sb.AppendLine("\t}");
            sb.AppendLine("}");

            var fileName = context.CreateHintName(targetNs, "Messages", messageName + "VersionMap");
            return (fileName, sb.ToString());
        }

        /// <summary>
        /// Determines all versions needed for a message based on sinceVersion attributes.
        /// Returns a list like [0, 1, 2] for a message with fields at versions 0, 1, and 2.
        /// </summary>
        private static List<int> GetMessageVersions(SchemaMessageDto messageDto, int schemaVersion, SourceProductionContext sourceContext)
        {
            var versions = new HashSet<int> { 0 }; // Always generate version 0

            foreach (var field in messageDto.Fields)
            {
                if (!string.IsNullOrEmpty(field.SinceVersion))
                {
                    if (int.TryParse(field.SinceVersion, out int sinceVersion))
                    {
                        if (schemaVersion >= 0 && sinceVersion > schemaVersion && sourceContext.CancellationToken != default)
                        {
                            sourceContext.ReportDiagnostic(Diagnostic.Create(
                                SbeDiagnostics.SinceVersionExceedsSchemaVersion,
                                Location.None,
                                field.Name,
                                sinceVersion.ToString(),
                                schemaVersion.ToString()));
                        }

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

            foreach (var data in messageDto.Data)
            {
                if (!string.IsNullOrEmpty(data.SinceVersion))
                {
                    if (int.TryParse(data.SinceVersion, out int sinceVersion))
                    {
                        if (schemaVersion >= 0 && sinceVersion > schemaVersion && sourceContext.CancellationToken != default)
                        {
                            sourceContext.ReportDiagnostic(Diagnostic.Create(
                                SbeDiagnostics.SinceVersionExceedsSchemaVersion,
                                Location.None,
                                data.Name,
                                sinceVersion.ToString(),
                                schemaVersion.ToString()));
                        }

                        for (int v = 0; v <= sinceVersion; v++)
                            versions.Add(v);
                    }
                    else if (sourceContext.CancellationToken != default)
                    {
                        sourceContext.ReportDiagnostic(Diagnostic.Create(
                            SbeDiagnostics.InvalidIntegerAttribute,
                            Location.None,
                            "sinceVersion",
                            data.SinceVersion,
                            data.Name));
                    }
                }
            }

            var sorted = new List<int>(versions);
            sorted.Sort();
            return sorted;
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

            return string.Join(".", segments, 0, segments.Length - 1);
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
            var result = new List<IFileContentGenerator>(fields.Count);
            foreach (var field in fields)
            {
                if (!string.IsNullOrEmpty(field.SinceVersion))
                {
                    if (int.TryParse(field.SinceVersion, out int sinceVersion) && sinceVersion > version)
                        continue;
                }

                bool isOptional = field.Presence == "optional" || context.OptionalTypes.ContainsKey(field.Type);
                var translated = TypeTranslator.Translate(field.Type);
                var resolvedType = TypeResolverHelper.ResolveTypeName(translated.PrimitiveType, context);
                var generatedFieldName = TypeTranslator.NormalizeName(field.Name);

                if (isOptional)
                {
                    var underlyingType = GetUnderlyingType(field.Type, context);
                    var effectivePrimitiveType = underlyingType ?? resolvedType;

                    // When the effective type is not a known primitive (composites, char arrays),
                    // we can't generate optional null-check code. Fall back to a regular field.
                    if (!TypesCatalog.HasNullValue(effectivePrimitiveType) && string.IsNullOrEmpty(field.NullValue))
                    {
                        var fieldUnderlyingType = GetUnderlyingType(field.Type, context);
                        result.Add(new MessageFieldDefinition(
                            generatedFieldName,
                            field.Id,
                            resolvedType,
                            field.Description,
                            ParseOffset(field.Offset, field.Name, sourceContext),
                            TypeResolverHelper.GetTypeLength(field.Type, context),
                            field.SinceVersion,
                            field.Deprecated,
                            context.EndianConversion,
                            fieldUnderlyingType,
                            context.StructTypeNames.Contains(field.Type)
                        ));
                    }
                    else
                    {
                        // When the field references a named optional type (e.g., "RptSeq"),
                        // use the underlying primitive type directly instead of the wrapper struct.
                        // Also propagate the null value from the type definition.
                        var fieldType = resolvedType;
                        var fieldNullValue = field.NullValue == "" ? null : field.NullValue;
                        if (context.OptionalTypes.TryGetValue(field.Type, out var optionalTypeInfo))
                        {
                            fieldType = effectivePrimitiveType;
                            if (fieldNullValue == null && !string.IsNullOrEmpty(optionalTypeInfo.NullValue))
                            {
                                fieldNullValue = optionalTypeInfo.NullValue;
                            }
                        }
                        else if (context.TypePrimitiveMapping.ContainsKey(field.Type))
                        {
                            // Regular named type used as optional field - use primitive type directly
                            fieldType = effectivePrimitiveType;
                        }

                        result.Add(new OptionalMessageFieldDefinition(
                            generatedFieldName,
                            field.Id,
                            fieldType,
                            effectivePrimitiveType,
                            field.Description,
                            ParseOffset(field.Offset, field.Name, sourceContext),
                            TypeResolverHelper.GetTypeLength(field.Type, context),
                            field.SinceVersion,
                            field.Deprecated,
                            fieldNullValue,
                            context.EndianConversion
                        ));
                    }
                }
                else
                {
                    // For enum fields, get the underlying primitive type for endian conversion
                    var underlyingType = GetUnderlyingType(field.Type, context);
                    result.Add(new MessageFieldDefinition(
                        generatedFieldName,
                        field.Id,
                        resolvedType,
                        field.Description,
                        ParseOffset(field.Offset, field.Name, sourceContext),
                        TypeResolverHelper.GetTypeLength(field.Type, context),
                        field.SinceVersion,
                        field.Deprecated,
                        context.EndianConversion,
                        underlyingType,
                        context.StructTypeNames.Contains(field.Type)
                    ));
                }
            }
            return result;
        }

        private static List<IFileContentGenerator> BuildConstants(List<SchemaFieldDto> constants, SchemaContext context)
        {
            var result = new List<IFileContentGenerator>(constants.Count);
            foreach (var constant in constants)
            {
                var type = constant.Type;
                var resolvedType = TypeResolverHelper.ResolveTypeName(TypeTranslator.Translate(type).PrimitiveType, context);
                var valueRef = TypeResolverHelper.NormalizeValueRef(constant.ValueRef, context);

                // When a constant field references a constant type (e.g., SeqNum1),
                // resolve to the primitive type and use the type's stored value.
                if (string.IsNullOrWhiteSpace(valueRef) && context.ConstantTypeInfo.TryGetValue(type, out var info))
                {
                    resolvedType = info.PrimitiveType;
                    valueRef = info.Value;
                }

                result.Add(new ConstantMessageFieldDefinition(
                    TypeTranslator.NormalizeName(constant.Name),
                    constant.Id,
                    resolvedType,
                    constant.Description != "" ? constant.Description : constant.Type,
                    valueRef
                ));
            }
            return result;
        }

        private static List<IFileContentGenerator> BuildGroups(
            List<SchemaGroupDto> groups,
            string versionNamespace,
            SchemaContext context,
            SourceProductionContext sourceContext)
        {
            var result = new List<IFileContentGenerator>(groups.Count);
            foreach (var group in groups)
            {
                var groupName = TypeResolverHelper.RegisterGeneratedTypeName(context, group.Name, sourceContext);

                var groupFields = new List<IFileContentGenerator>(group.Fields.Count);
                foreach (var field in group.Fields)
                {
                    var translatedField = TypeTranslator.Translate(field.Type);
                    var resolvedFieldType = TypeResolverHelper.ResolveTypeName(translatedField.PrimitiveType, context);
                    var generatedFieldName = TypeTranslator.NormalizeName(field.Name);
                    var underlyingFieldType = GetUnderlyingType(field.Type, context);
                    groupFields.Add(new MessageFieldDefinition(
                        generatedFieldName,
                        field.Id,
                        resolvedFieldType,
                        field.Description,
                        ParseOffset(field.Offset, field.Name, sourceContext),
                        TypeResolverHelper.GetTypeLength(field.Type, context),
                        field.SinceVersion,
                        field.Deprecated,
                        context.EndianConversion,
                        underlyingFieldType,
                        context.StructTypeNames.Contains(field.Type)
                    ));
                }

                var groupConstants = BuildConstants(group.Constants, context);
                var numInGroupType = TypeResolverHelper.ResolveTypeName(GetNumInGroupType(group.DimensionType, context, sourceContext), context);

                result.Add(new GroupDefinition(
                    versionNamespace,
                    groupName,
                    group.Id,
                    TypeResolverHelper.ResolveTypeName(group.DimensionType, context),
                    group.Description,
                    groupFields,
                    groupConstants,
                    numInGroupType,
                    group.Data != null ? BuildData(group.Data, context) : null,
                    group.Groups != null ? BuildGroups(group.Groups, versionNamespace, context, sourceContext) : null,
                    context.EndianConversion
                ));
            }
            return result;
        }

        private static List<IFileContentGenerator> BuildData(List<SchemaDataDto> dataList, SchemaContext context)
        {
            return GetDataForVersion(dataList, int.MaxValue, context);
        }

        private static List<IFileContentGenerator> GetDataForVersion(List<SchemaDataDto> dataList, int version, SchemaContext context)
        {
            var result = new List<IFileContentGenerator>(dataList.Count);
            foreach (var data in dataList)
            {
                if (!string.IsNullOrEmpty(data.SinceVersion)
                    && int.TryParse(data.SinceVersion, out int sinceVersion)
                    && sinceVersion > version)
                    continue;

                var lengthPrefixType = "byte";
                var lengthKey = $"{data.Type}.length";
                if (context.CompositeFieldTypes.TryGetValue(lengthKey, out var resolvedLengthType))
                    lengthPrefixType = resolvedLengthType;

                result.Add(new DataFieldDefinition(
                    TypeTranslator.NormalizeName(data.Name),
                    data.Id,
                    TypeResolverHelper.ResolveTypeName(data.Type, context),
                    data.Description,
                    lengthPrefixType
                ));
            }
            return result;
        }

        private static string? GetUnderlyingType(string type, SchemaContext context)
        {
            // Check if it's an enum type first
            if (context.EnumPrimitiveTypes.TryGetValue(type, out string? underlyingType))
                return underlyingType;

            // Check if it's an optional type (e.g., Int64NULL)
            if (context.OptionalTypes.TryGetValue(type, out var optionalInfo))
                return optionalInfo.PrimitiveType;

            // Check if it's a regular named type wrapping a primitive (e.g., SettlType -> ushort)
            if (context.TypePrimitiveMapping.TryGetValue(type, out var primitiveType))
                return primitiveType;

            return null;
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
