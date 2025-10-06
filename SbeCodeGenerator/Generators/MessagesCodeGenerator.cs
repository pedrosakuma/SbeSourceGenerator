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
    /// Generates code for SBE message definitions.
    /// </summary>
    public class MessagesCodeGenerator : ICodeGenerator
    {
        public IEnumerable<(string name, string content)> Generate(string ns, XmlDocument xmlDocument, SchemaContext context)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
            nsmgr.AddNamespace("sbe", "http://fixprotocol.io/2016/sbe");
            var messageNodes = xmlDocument.SelectNodes("//sbe:message", nsmgr);
            var messages = new List<MessageDefinition>();
            foreach (XmlElement messageNode in messageNodes)
            {
                var messageDto = SchemaParser.ParseMessage(messageNode, context);
                
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
                                        GetUnderlyingType(field.Type, context),
                                        field.Description,
                                        field.Offset == "" ? null : int.Parse(field.Offset),
                                        GetTypeLength(field.Type, context)
                                    ),
                                    _ => (IFileContentGenerator)new MessageFieldDefinition(
                                        field.Name.FirstCharToUpper(),
                                        field.Id,
                                        ToNativeType(field.Type),
                                        field.Description,
                                        field.Offset == "" ? null : int.Parse(field.Offset),
                                        GetTypeLength(field.Type, context)
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
                                                GetTypeLength(field.Type, context)
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
            
            // Generate parser for all messages
            var parserGenerator = new ParserCodeGenerator();
            foreach (var item in parserGenerator.GenerateParser(ns, messages, context))
                yield return item;
        }

        private static string? GetUnderlyingType(string type, SchemaContext context)
        {
            context.EnumPrimitiveTypes.TryGetValue(type, out string? underlyingType);
            return underlyingType;
        }

        private static int GetTypeLength(string type, SchemaContext context)
        {
            int length;
            if (!TypesCatalog.PrimitiveTypeLengths.TryGetValue(ToNativeType(type), out length)
                && !context.CustomTypeLengths.TryGetValue(type, out length))
                throw new ArgumentException($"Could not get type {type} length");
            return length;
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
