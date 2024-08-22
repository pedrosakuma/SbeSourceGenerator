using System.Collections.Generic;

namespace SbeSourceGenerator
{
    public record NullableEnumDefinition(string Namespace, string Name, string Description, string EncodingType, string SemanticType, int Length, List<EnumFieldDefinition> Fields)
        : EnumDefinition(Namespace, Name, Description, EncodingType, SemanticType, Length, AddNullValue(Fields))
    {
        private static List<EnumFieldDefinition> AddNullValue(List<EnumFieldDefinition> fields)
        {
            fields.Insert(0, new EnumFieldDefinition("None", "None", "0"));
            return fields;
        }
    }
}