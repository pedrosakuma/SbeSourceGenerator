using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SbeSourceGenerator.Schema
{
    /// <summary>
    /// Forward-only XML reader that parses an SBE schema into a <see cref="ParsedSchema"/>
    /// in a single pass. Eliminates XmlDocument DOM materialization, XPath queries,
    /// and repeated attribute access.
    /// </summary>
    internal static class SchemaReader
    {
        public static ParsedSchema Parse(string xmlContent, SourceProductionContext sourceContext)
        {
            var types = new List<SchemaTypeDto>(16);
            var composites = new List<SchemaCompositeDto>(8);
            var enums = new List<SchemaEnumDto>(16);
            var sets = new List<SchemaEnumDto>(8);
            var messages = new List<SchemaMessageDto>(16);

            string byteOrder = "";
            string package = "";
            string version = "";
            string id = "";
            string description = "";
            string semanticVersion = "";
            string headerType = "messageHeader";

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            using (var reader = XmlReader.Create(new StringReader(xmlContent), settings))
            {
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element)
                        continue;

                    switch (reader.LocalName)
                    {
                        case "messageSchema":
                            byteOrder = reader.GetAttribute("byteOrder") ?? "";
                            package = reader.GetAttribute("package") ?? "";
                            version = reader.GetAttribute("version") ?? "";
                            id = reader.GetAttribute("id") ?? "";
                            description = reader.GetAttribute("description") ?? "";
                            semanticVersion = reader.GetAttribute("semanticVersion") ?? "";
                            headerType = reader.GetAttribute("headerType") ?? "messageHeader";
                            break;

                        case "types":
                            ReadTypes(reader, types, composites, enums, sets, sourceContext);
                            break;

                        case "message":
                            messages.Add(ReadMessage(reader, sourceContext));
                            break;
                    }
                }
            }

            return new ParsedSchema(byteOrder, package, version, id, description, semanticVersion,
                types, composites, enums, sets, messages, headerType);
        }

        public static ParsedSchema Parse(string xmlContent)
        {
            return Parse(xmlContent, default);
        }

        private static void ReadTypes(XmlReader reader,
            List<SchemaTypeDto> types,
            List<SchemaCompositeDto> composites,
            List<SchemaEnumDto> enums,
            List<SchemaEnumDto> sets,
            SourceProductionContext sourceContext)
        {
            if (reader.IsEmptyElement)
                return;

            int depth = reader.Depth;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
                    break;

                if (reader.NodeType != XmlNodeType.Element)
                    continue;

                switch (reader.LocalName)
                {
                    case "type":
                        types.Add(ReadType(reader, sourceContext));
                        break;
                    case "composite":
                        composites.Add(ReadComposite(reader, sourceContext));
                        break;
                    case "enum":
                        enums.Add(ReadEnum(reader, sourceContext));
                        break;
                    case "set":
                        sets.Add(ReadSet(reader, sourceContext));
                        break;
                }
            }
        }

        private static SchemaTypeDto ReadType(XmlReader reader, SourceProductionContext sourceContext)
        {
            string name = GetRequiredAttribute(reader, "name", "type", sourceContext);
            string desc = reader.GetAttribute("description") ?? "";
            string primitiveType = GetRequiredAttribute(reader, "primitiveType", "type", sourceContext);
            string semanticType = reader.GetAttribute("semanticType") ?? "";
            string presence = reader.GetAttribute("presence") ?? "";
            string nullValue = reader.GetAttribute("nullValue") ?? "";
            string length = reader.GetAttribute("length") ?? "";
            string minValue = reader.GetAttribute("minValue") ?? "";
            string maxValue = reader.GetAttribute("maxValue") ?? "";
            string characterEncoding = reader.GetAttribute("characterEncoding") ?? "";

            string innerText = "";
            if (!reader.IsEmptyElement)
            {
                int typeDepth = reader.Depth;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
                        innerText = reader.Value;
                    if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == typeDepth)
                        break;
                }
            }

            return new SchemaTypeDto(name, desc, primitiveType, semanticType, presence, nullValue, length, innerText, minValue, maxValue, characterEncoding);
        }

        private static SchemaCompositeDto ReadComposite(XmlReader reader, SourceProductionContext sourceContext)
        {
            string name = GetRequiredAttribute(reader, "name", "composite", sourceContext);
            string desc = reader.GetAttribute("description") ?? "";
            string semanticType = reader.GetAttribute("semanticType") ?? "";

            var fields = new List<SchemaFieldDto>(16);
            var nestedComposites = new List<SchemaCompositeDto>();
            var nestedEnums = new List<SchemaEnumDto>();

            if (!reader.IsEmptyElement)
            {
                int depth = reader.Depth;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
                        break;
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.LocalName)
                        {
                            case "composite":
                                var nested = ReadComposite(reader, sourceContext);
                                nestedComposites.Add(nested);
                                // Add a ref-like field placeholder to preserve ordering
                                fields.Add(new SchemaFieldDto(nested.Name, nested.Description,
                                    "", "", "", "", "", "", "", "", nested.Name, "", "", "", ""));
                                break;
                            case "enum":
                                var nestedEnum = ReadEnum(reader, sourceContext);
                                nestedEnums.Add(nestedEnum);
                                break;
                            case "set":
                                // Sets inside composites are handled similarly to enums
                                // Skip the set element content for now
                                if (!reader.IsEmptyElement)
                                {
                                    int setDepth = reader.Depth;
                                    while (reader.Read())
                                    {
                                        if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == setDepth)
                                            break;
                                    }
                                }
                                break;
                            default:
                                fields.Add(ReadField(reader, sourceContext));
                                break;
                        }
                    }
                }
            }

            return new SchemaCompositeDto(name, desc, semanticType, fields, nestedComposites, nestedEnums);
        }

        private static SchemaEnumDto ReadEnum(XmlReader reader, SourceProductionContext sourceContext)
        {
            string name = GetRequiredAttribute(reader, "name", "enum", sourceContext);
            string desc = reader.GetAttribute("description") ?? "";
            string encodingType = GetRequiredAttribute(reader, "encodingType", "enum", sourceContext);
            string semanticType = reader.GetAttribute("semanticType") ?? "";

            var choices = new List<SchemaFieldDto>(16);
            if (!reader.IsEmptyElement)
            {
                int depth = reader.Depth;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
                        break;
                    if (reader.NodeType == XmlNodeType.Element)
                        choices.Add(ReadField(reader, sourceContext));
                }
            }

            return new SchemaEnumDto(name, desc, encodingType, semanticType, choices);
        }

        private static SchemaEnumDto ReadSet(XmlReader reader, SourceProductionContext sourceContext)
        {
            // Sets use the same DTO as enums
            return ReadEnum(reader, sourceContext);
        }

        private static SchemaMessageDto ReadMessage(XmlReader reader, SourceProductionContext sourceContext)
        {
            string name = GetRequiredAttribute(reader, "name", "message", sourceContext);
            string msgId = GetRequiredAttribute(reader, "id", "message", sourceContext);
            string desc = reader.GetAttribute("description") ?? "";
            string semanticType = reader.GetAttribute("semanticType") ?? "";
            string deprecated = reader.GetAttribute("deprecated") ?? "";
            string blockLengthAttr = reader.GetAttribute("blockLength") ?? "";

            var fields = new List<SchemaFieldDto>(16);
            var constants = new List<SchemaFieldDto>(4);
            var groups = new List<SchemaGroupDto>(4);
            var data = new List<SchemaDataDto>(4);

            if (!reader.IsEmptyElement)
            {
                int depth = reader.Depth;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
                        break;
                    if (reader.NodeType != XmlNodeType.Element)
                        continue;

                    switch (reader.LocalName)
                    {
                        case "field":
                            var field = ReadField(reader, sourceContext);
                            if (field.Presence == "constant")
                                constants.Add(field);
                            else
                                fields.Add(field);
                            break;
                        case "group":
                            groups.Add(ReadGroup(reader, sourceContext));
                            break;
                        case "data":
                            data.Add(ReadData(reader, sourceContext));
                            break;
                    }
                }
            }

            return new SchemaMessageDto(name, msgId, desc, semanticType, deprecated, fields, constants, groups, data, blockLengthAttr);
        }

        private static SchemaGroupDto ReadGroup(XmlReader reader, SourceProductionContext sourceContext)
        {
            string name = GetRequiredAttribute(reader, "name", "group", sourceContext);
            string groupId = GetRequiredAttribute(reader, "id", "group", sourceContext);
            string dimensionType = reader.GetAttribute("dimensionType") ?? "";
            if (string.IsNullOrEmpty(dimensionType))
                dimensionType = "GroupSizeEncoding";
            string desc = reader.GetAttribute("description") ?? "";

            var fields = new List<SchemaFieldDto>(16);
            var constants = new List<SchemaFieldDto>(4);
            var dataList = new List<SchemaDataDto>();
            var nestedGroups = new List<SchemaGroupDto>();

            if (!reader.IsEmptyElement)
            {
                int depth = reader.Depth;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
                        break;
                    if (reader.NodeType != XmlNodeType.Element)
                        continue;

                    if (reader.LocalName == "field")
                    {
                        var field = ReadField(reader, sourceContext);
                        if (field.Presence == "constant")
                            constants.Add(field);
                        else
                            fields.Add(field);
                    }
                    else if (reader.LocalName == "data")
                    {
                        dataList.Add(ReadData(reader, sourceContext));
                    }
                    else if (reader.LocalName == "group")
                    {
                        nestedGroups.Add(ReadGroup(reader, sourceContext));
                    }
                }
            }

            return new SchemaGroupDto(name, groupId, dimensionType, desc, fields, constants,
                dataList.Count > 0 ? dataList : null,
                nestedGroups.Count > 0 ? nestedGroups : null);
        }

        private static SchemaDataDto ReadData(XmlReader reader, SourceProductionContext sourceContext)
        {
            string name = GetRequiredAttribute(reader, "name", "data", sourceContext);
            string dataId = GetRequiredAttribute(reader, "id", "data", sourceContext);
            string type = GetRequiredAttribute(reader, "type", "data", sourceContext);
            string desc = reader.GetAttribute("description") ?? "";
            string sinceVersion = reader.GetAttribute("sinceVersion") ?? "";

            if (!reader.IsEmptyElement)
                reader.Skip();

            return new SchemaDataDto(name, dataId, type, desc, sinceVersion);
        }

        private static SchemaFieldDto ReadField(XmlReader reader, SourceProductionContext sourceContext)
        {
            string name = GetRequiredAttribute(reader, "name", reader.LocalName, sourceContext);
            string desc = reader.GetAttribute("description") ?? "";
            string primitiveType = reader.GetAttribute("primitiveType") ?? "";
            string presence = reader.GetAttribute("presence") ?? "";
            string length = reader.GetAttribute("length") ?? "";
            string nullValue = reader.GetAttribute("nullValue") ?? "";
            string valueRef = reader.GetAttribute("valueRef") ?? "";
            string fieldId = reader.GetAttribute("id") ?? "";
            string offset = reader.GetAttribute("offset") ?? "";
            string type = reader.GetAttribute("type") ?? "";
            string sinceVersion = reader.GetAttribute("sinceVersion") ?? "";
            string minValue = reader.GetAttribute("minValue") ?? "";
            string maxValue = reader.GetAttribute("maxValue") ?? "";
            string deprecated = reader.GetAttribute("deprecated") ?? "";
            string characterEncoding = reader.GetAttribute("characterEncoding") ?? "";

            // Read inner text manually to leave reader on the end element,
            // so the parent loop's reader.Read() correctly advances to the next sibling.
            string innerText = "";
            if (!reader.IsEmptyElement)
            {
                int fieldDepth = reader.Depth;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
                        innerText = reader.Value;
                    if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == fieldDepth)
                        break;
                }
            }

            return new SchemaFieldDto(name, desc, primitiveType, presence, length, nullValue, valueRef,
                innerText, fieldId, offset, type, sinceVersion, minValue, maxValue, deprecated, characterEncoding);
        }

        private static string GetRequiredAttribute(XmlReader reader, string attributeName, string elementName, SourceProductionContext sourceContext)
        {
            string value = reader.GetAttribute(attributeName) ?? "";
            if (string.IsNullOrEmpty(value))
            {
                if (sourceContext.CancellationToken != default)
                {
                    sourceContext.ReportDiagnostic(Diagnostic.Create(
                        SbeDiagnostics.MissingRequiredAttribute,
                        Location.None,
                        attributeName,
                        elementName));
                }
            }
            return value;
        }
    }
}
