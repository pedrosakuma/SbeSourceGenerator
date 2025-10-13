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
        public static SchemaFieldDto ParseField(XmlElement fieldElement)
        {
            return new SchemaFieldDto(
                Name: fieldElement.GetAttributeOrEmpty("name"),
                Description: fieldElement.GetAttributeOrEmpty("description"),
                PrimitiveType: fieldElement.GetAttributeOrEmpty("primitiveType"),
                Presence: fieldElement.GetAttributeOrEmpty("presence"),
                Length: fieldElement.GetAttributeOrEmpty("length"),
                NullValue: fieldElement.GetAttributeOrEmpty("nullValue"),
                ValueRef: fieldElement.GetAttributeOrEmpty("valueRef"),
                InnerText: fieldElement.GetInnerTextOrEmpty(),
                Id: fieldElement.GetAttributeOrEmpty("id"),
                Offset: fieldElement.GetAttributeOrEmpty("offset"),
                Type: fieldElement.GetAttributeOrEmpty("type")
            );
        }

        /// <summary>
        /// Parses an XmlElement representing a composite type into a SchemaCompositeDto.
        /// </summary>
        public static SchemaCompositeDto ParseComposite(XmlElement compositeElement)
        {
            var fields = compositeElement.ChildNodes
                .Cast<XmlElement>()
                .Select(ParseField)
                .ToList();

            return new SchemaCompositeDto(
                Name: compositeElement.GetAttributeOrEmpty("name"),
                Description: compositeElement.GetAttributeOrEmpty("description"),
                SemanticType: compositeElement.GetAttributeOrEmpty("semanticType"),
                Fields: fields
            );
        }

        /// <summary>
        /// Parses an XmlElement representing an enum or set type into a SchemaEnumDto.
        /// </summary>
        public static SchemaEnumDto ParseEnum(XmlElement enumElement)
        {
            var choices = enumElement.ChildNodes
                .Cast<XmlElement>()
                .Select(ParseField)
                .ToList();

            return new SchemaEnumDto(
                Name: enumElement.GetAttributeOrEmpty("name"),
                Description: enumElement.GetAttributeOrEmpty("description"),
                EncodingType: enumElement.GetAttributeOrEmpty("encodingType"),
                SemanticType: enumElement.GetAttributeOrEmpty("semanticType"),
                Choices: choices
            );
        }

        /// <summary>
        /// Parses an XmlElement representing a simple type into a SchemaTypeDto.
        /// </summary>
        public static SchemaTypeDto ParseType(XmlElement typeElement)
        {
            return new SchemaTypeDto(
                Name: typeElement.GetAttributeOrEmpty("name"),
                Description: typeElement.GetAttributeOrEmpty("description"),
                PrimitiveType: typeElement.GetAttributeOrEmpty("primitiveType"),
                SemanticType: typeElement.GetAttributeOrEmpty("semanticType"),
                Presence: typeElement.GetAttributeOrEmpty("presence"),
                NullValue: typeElement.GetAttributeOrEmpty("nullValue"),
                Length: typeElement.GetAttributeOrEmpty("length"),
                InnerText: typeElement.GetInnerTextOrEmpty()
            );
        }

        /// <summary>
        /// Parses an XmlElement representing a group into a SchemaGroupDto.
        /// </summary>
        public static SchemaGroupDto ParseGroup(XmlElement groupElement, SchemaContext context)
        {
            var fields = groupElement.ChildNodes
                .Cast<XmlElement>()
                .Where(x => x.Name == "field")
                .Where(x => x.GetAttributeOrEmpty("presence") == "" || x.GetAttributeOrEmpty("presence") == "optional")
                .Where(x => !context.CustomConstantTypes.ContainsKey(x.GetAttributeOrEmpty("type")))
                .Select(ParseField)
                .ToList();

            var constants = groupElement.ChildNodes
                .Cast<XmlElement>()
                .Where(x => x.Name == "field" && x.GetAttributeOrEmpty("presence") == "constant")
                .Select(ParseField)
                .ToList();

            return new SchemaGroupDto(
                Name: groupElement.GetAttributeOrEmpty("name"),
                Id: groupElement.GetAttributeOrEmpty("id"),
                DimensionType: groupElement.GetAttributeOrEmpty("dimensionType") == "" ? "GroupSizeEncoding" : groupElement.GetAttributeOrEmpty("dimensionType"),
                Description: groupElement.GetAttributeOrEmpty("description"),
                Fields: fields,
                Constants: constants
            );
        }

        /// <summary>
        /// Parses an XmlElement representing a data field into a SchemaDataDto.
        /// </summary>
        public static SchemaDataDto ParseData(XmlElement dataElement)
        {
            return new SchemaDataDto(
                Name: dataElement.GetAttributeOrEmpty("name"),
                Id: dataElement.GetAttributeOrEmpty("id"),
                Type: dataElement.GetAttributeOrEmpty("type"),
                Description: dataElement.GetAttributeOrEmpty("description")
            );
        }

        /// <summary>
        /// Parses an XmlElement representing a message into a SchemaMessageDto.
        /// </summary>
        public static SchemaMessageDto ParseMessage(XmlElement messageElement, SchemaContext context)
        {
            var fields = messageElement.ChildNodes
                .Cast<XmlElement>()
                .Where(x => x.Name == "field")
                .Where(x => x.GetAttributeOrEmpty("presence") == "" || x.GetAttributeOrEmpty("presence") == "optional")
                .Where(x => !context.CustomConstantTypes.ContainsKey(x.GetAttributeOrEmpty("type")))
                .Select(ParseField)
                .ToList();

            var constants = messageElement.ChildNodes
                .Cast<XmlElement>()
                .Where(x => x.Name == "field" && x.GetAttributeOrEmpty("presence") == "constant" && x.GetAttributeOrEmpty("valueRef") != "")
                .Select(ParseField)
                .ToList();

            var groups = messageElement.ChildNodes
                .Cast<XmlElement>()
                .Where(x => x.Name == "group")
                .Select(x => ParseGroup(x, context))
                .ToList();

            var data = messageElement.ChildNodes
                .Cast<XmlElement>()
                .Where(x => x.Name == "data")
                .Select(ParseData)
                .ToList();

            return new SchemaMessageDto(
                Name: messageElement.GetAttributeOrEmpty("name"),
                Id: messageElement.GetAttributeOrEmpty("id"),
                Description: messageElement.GetAttributeOrEmpty("description"),
                SemanticType: messageElement.GetAttributeOrEmpty("semanticType"),
                Deprecated: messageElement.GetAttributeOrEmpty("deprecated"),
                Fields: fields,
                Constants: constants,
                Groups: groups,
                Data: data
            );
        }
    }
}
