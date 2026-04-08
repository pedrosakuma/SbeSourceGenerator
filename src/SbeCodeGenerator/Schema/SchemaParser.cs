using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace SbeSourceGenerator.Schema
{
    /// <summary>
    /// Static utilities for parsing XML schema elements into DTOs.
    /// Centralizes XML parsing logic to reduce duplication across generators.
    /// </summary>
    internal static class SchemaParser
    {
        /// <summary>
        /// Parses an XmlElement representing a field into a SchemaFieldDto.
        /// </summary>
        public static SchemaFieldDto ParseField(XmlElement fieldElement, SourceProductionContext sourceContext)
        {
            return new SchemaFieldDto(
                Name: fieldElement.GetRequiredAttribute("name", sourceContext),
                Description: fieldElement.GetAttributeOrEmpty("description"),
                PrimitiveType: fieldElement.GetAttributeOrEmpty("primitiveType"),
                Presence: fieldElement.GetAttributeOrEmpty("presence"),
                Length: fieldElement.GetAttributeOrEmpty("length"),
                NullValue: fieldElement.GetAttributeOrEmpty("nullValue"),
                ValueRef: fieldElement.GetAttributeOrEmpty("valueRef"),
                InnerText: fieldElement.GetInnerTextOrEmpty(),
                Id: fieldElement.GetAttributeOrEmpty("id"),
                Offset: fieldElement.GetAttributeOrEmpty("offset"),
                Type: fieldElement.GetAttributeOrEmpty("type"),
                SinceVersion: fieldElement.GetAttributeOrEmpty("sinceVersion"),
                MinValue: fieldElement.GetAttributeOrEmpty("minValue"),
                MaxValue: fieldElement.GetAttributeOrEmpty("maxValue"),
                Deprecated: fieldElement.GetAttributeOrEmpty("deprecated")
            );
        }

        /// <summary>
        /// Parses an XmlElement representing a composite type into a SchemaCompositeDto.
        /// </summary>
        public static SchemaCompositeDto ParseComposite(XmlElement compositeElement, SourceProductionContext sourceContext)
        {
            var fields = EnumerateElements(compositeElement.ChildNodes)
                .Select(e => ParseField(e, sourceContext))
                .ToList();

            return new SchemaCompositeDto(
                Name: compositeElement.GetRequiredAttribute("name", sourceContext),
                Description: compositeElement.GetAttributeOrEmpty("description"),
                SemanticType: compositeElement.GetAttributeOrEmpty("semanticType"),
                Fields: fields
            );
        }

        /// <summary>
        /// Parses an XmlElement representing an enum or set type into a SchemaEnumDto.
        /// </summary>
        public static SchemaEnumDto ParseEnum(XmlElement enumElement, SourceProductionContext sourceContext)
        {
            var choices = EnumerateElements(enumElement.ChildNodes)
                .Select(e => ParseField(e, sourceContext))
                .ToList();

            return new SchemaEnumDto(
                Name: enumElement.GetRequiredAttribute("name", sourceContext),
                Description: enumElement.GetAttributeOrEmpty("description"),
                EncodingType: enumElement.GetRequiredAttribute("encodingType", sourceContext),
                SemanticType: enumElement.GetAttributeOrEmpty("semanticType"),
                Choices: choices
            );
        }

        /// <summary>
        /// Parses an XmlElement representing a simple type into a SchemaTypeDto.
        /// </summary>
        public static SchemaTypeDto ParseType(XmlElement typeElement, SourceProductionContext sourceContext)
        {
            return new SchemaTypeDto(
                Name: typeElement.GetRequiredAttribute("name", sourceContext),
                Description: typeElement.GetAttributeOrEmpty("description"),
                PrimitiveType: typeElement.GetRequiredAttribute("primitiveType", sourceContext),
                SemanticType: typeElement.GetAttributeOrEmpty("semanticType"),
                Presence: typeElement.GetAttributeOrEmpty("presence"),
                NullValue: typeElement.GetAttributeOrEmpty("nullValue"),
                Length: typeElement.GetAttributeOrEmpty("length"),
                InnerText: typeElement.GetInnerTextOrEmpty(),
                MinValue: typeElement.GetAttributeOrEmpty("minValue"),
                MaxValue: typeElement.GetAttributeOrEmpty("maxValue")
            );
        }

        /// <summary>
        /// Parses an XmlElement representing a group into a SchemaGroupDto.
        /// </summary>
        public static SchemaGroupDto ParseGroup(XmlElement groupElement, SchemaContext context, SourceProductionContext sourceContext)
        {
            var elements = EnumerateElements(groupElement.ChildNodes);

            var fields = elements
                .Where(x => x.Name == "field")
                .Where(x => x.GetAttributeOrEmpty("presence") == "" || x.GetAttributeOrEmpty("presence") == "optional")
                .Where(x => !context.CustomConstantTypes.ContainsKey(x.GetAttributeOrEmpty("type")))
                .Select(e => ParseField(e, sourceContext))
                .ToList();

            var constants = elements
                .Where(x => x.Name == "field" && x.GetAttributeOrEmpty("presence") == "constant")
                .Select(e => ParseField(e, sourceContext))
                .ToList();

            return new SchemaGroupDto(
                Name: groupElement.GetRequiredAttribute("name", sourceContext),
                Id: groupElement.GetRequiredAttribute("id", sourceContext),
                DimensionType: groupElement.GetAttributeOrEmpty("dimensionType") == "" ? "GroupSizeEncoding" : groupElement.GetAttributeOrEmpty("dimensionType"),
                Description: groupElement.GetAttributeOrEmpty("description"),
                Fields: fields,
                Constants: constants
            );
        }

        /// <summary>
        /// Parses an XmlElement representing a data field into a SchemaDataDto.
        /// </summary>
        public static SchemaDataDto ParseData(XmlElement dataElement, SourceProductionContext sourceContext)
        {
            return new SchemaDataDto(
                Name: dataElement.GetRequiredAttribute("name", sourceContext),
                Id: dataElement.GetRequiredAttribute("id", sourceContext),
                Type: dataElement.GetRequiredAttribute("type", sourceContext),
                Description: dataElement.GetAttributeOrEmpty("description")
            );
        }

        /// <summary>
        /// Parses an XmlElement representing a message into a SchemaMessageDto.
        /// </summary>
        public static SchemaMessageDto ParseMessage(XmlElement messageElement, SchemaContext context, SourceProductionContext sourceContext)
        {
            var elements = EnumerateElements(messageElement.ChildNodes);

            var fields = elements
                .Where(x => x.Name == "field")
                .Where(x => x.GetAttributeOrEmpty("presence") == "" || x.GetAttributeOrEmpty("presence") == "optional")
                .Where(x => !context.CustomConstantTypes.ContainsKey(x.GetAttributeOrEmpty("type")))
                .Select(e => ParseField(e, sourceContext))
                .ToList();

            var constants = elements
                .Where(x => x.Name == "field" && x.GetAttributeOrEmpty("presence") == "constant" && x.GetAttributeOrEmpty("valueRef") != "")
                .Select(e => ParseField(e, sourceContext))
                .ToList();

            var groups = elements
                .Where(x => x.Name == "group")
                .Select(x => ParseGroup(x, context, sourceContext))
                .ToList();

            var data = elements
                .Where(x => x.Name == "data")
                .Select(e => ParseData(e, sourceContext))
                .ToList();

            return new SchemaMessageDto(
                Name: messageElement.GetRequiredAttribute("name", sourceContext),
                Id: messageElement.GetRequiredAttribute("id", sourceContext),
                Description: messageElement.GetAttributeOrEmpty("description"),
                SemanticType: messageElement.GetAttributeOrEmpty("semanticType"),
                Deprecated: messageElement.GetAttributeOrEmpty("deprecated"),
                Fields: fields,
                Constants: constants,
                Groups: groups,
                Data: data
            );
        }

        private static List<XmlElement> EnumerateElements(XmlNodeList nodeList)
        {
            var result = new List<XmlElement>(nodeList.Count);
            foreach (XmlNode node in nodeList)
            {
                if (node.NodeType == XmlNodeType.Element)
                    result.Add((XmlElement)node);
            }
            return result;
        }
    }
}
