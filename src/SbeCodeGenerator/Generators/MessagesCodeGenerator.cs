using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Diagnostics;
using SbeSourceGenerator.Generators.Fields;
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
    /// Generates code for SBE message definitions.
    /// </summary>
    public class MessagesCodeGenerator : ICodeGenerator
    {
        public IEnumerable<(string name, string content)> Generate(string ns, XmlDocument xmlDocument, SchemaContext context, SourceProductionContext sourceContext)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
            nsmgr.AddNamespace("sbe", "http://fixprotocol.io/2016/sbe");
            var messageNodes = xmlDocument.SelectNodes("//sbe:message", nsmgr);
            foreach (XmlElement messageNode in messageNodes)
            {
                var messageDto = SchemaParser.ParseMessage(messageNode, context);
                
                // Determine all versions for this message based on sinceVersion attributes
                var versions = GetMessageVersions(messageDto);
                
                // Generate a separate type for each version
                foreach (var version in versions)
                {
                    var versionNamespace = version == 0 ? ns : $"{ns}.V{version}";
                    var fieldsForVersion = GetFieldsForVersion(messageDto.Fields, version, sourceContext, context);
                    
                    var generator = new MessageDefinition(
                            versionNamespace,
                            messageDto.Name.FirstCharToUpper(),
                            messageDto.Id,
                            $"{messageDto.Description} (Version {version})",
                            messageDto.SemanticType,
                            messageDto.Deprecated,
                            fieldsForVersion,
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
                                    versionNamespace,
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
                                                ParseOffset(field.Offset, field.Name, sourceContext),
                                                GetTypeLength(field.Type, context),
                                                field.SinceVersion,
                                                field.Deprecated
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
                                        ).ToList(),
                                    GetNumInGroupType(group.DimensionType, context)
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
                    StringBuilder sb = new StringBuilder();
                    generator.AppendFileContent(sb);
                    var fileName = version == 0 
                        ? $"{ns}\\Messages\\{generator.Name}"
                        : $"{ns}\\V{version}\\Messages\\{generator.Name}";
                    yield return (fileName, sb.ToString());
                }
            }
        }

        /// <summary>
        /// Determines all versions needed for a message based on sinceVersion attributes.
        /// Returns a list like [0, 1, 2] for a message with fields at versions 0, 1, and 2.
        /// </summary>
        private static List<int> GetMessageVersions(SchemaMessageDto messageDto)
        {
            var versions = new HashSet<int> { 0 }; // Always generate version 0
            
            foreach (var field in messageDto.Fields)
            {
                if (!string.IsNullOrEmpty(field.SinceVersion) && int.TryParse(field.SinceVersion, out int sinceVersion))
                {
                    // Add this version and all intermediate versions
                    for (int v = 0; v <= sinceVersion; v++)
                    {
                        versions.Add(v);
                    }
                }
            }
            
            return versions.OrderBy(v => v).ToList();
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
                    // Include field if it has no sinceVersion or if sinceVersion <= current version
                    if (string.IsNullOrEmpty(field.SinceVersion))
                        return true;
                    
                    if (int.TryParse(field.SinceVersion, out int sinceVersion))
                        return sinceVersion <= version;
                    
                    return true; // Include if we can't parse the version
                })
                .Select(field =>
                {
                    // Check if this field is optional either by:
                    // 1. Having presence="optional" attribute
                    // 2. Using a type that is defined as optional (e.g., Int64NULL)
                    bool isOptional = field.Presence == "optional" || context.OptionalTypes.ContainsKey(field.Type);
                    
                    return isOptional
                        ? new OptionalMessageFieldDefinition(
                            field.Name.FirstCharToUpper(),
                            field.Id,
                            ToNativeType(field.Type),
                            GetUnderlyingType(field.Type, context),
                            field.Description,
                            ParseOffset(field.Offset, field.Name, sourceContext),
                            GetTypeLength(field.Type, context),
                            field.SinceVersion,
                            field.Deprecated
                        )
                        : (IFileContentGenerator)new MessageFieldDefinition(
                            field.Name.FirstCharToUpper(),
                            field.Id,
                            ToNativeType(field.Type),
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

        private static int GetTypeLength(string type, SchemaContext context)
        {
            int length;
            if (!TypesCatalog.PrimitiveTypeLengths.TryGetValue(ToNativeType(type), out length)
                && !context.CustomTypeLengths.TryGetValue(type, out length))
                throw new ArgumentException($"Could not get type {type} length");
            return length;
        }

        private static string GetNumInGroupType(string dimensionType, SchemaContext context)
        {
            // Look up the type of the numInGroup field in the dimension composite type
            // Default to "uint" if not found for backward compatibility
            var key = $"{dimensionType}.numInGroup";
            if (context.CompositeFieldTypes.TryGetValue(key, out string? numInGroupType))
            {
                return numInGroupType;
            }
            // Fallback to "uint" for backward compatibility with existing schemas
            return "uint";
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
