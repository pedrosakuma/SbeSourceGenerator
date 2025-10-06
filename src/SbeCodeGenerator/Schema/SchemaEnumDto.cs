using System.Collections.Generic;

namespace SbeSourceGenerator.Schema
{
    /// <summary>
    /// Immutable DTO representing an enum or set type from the XML schema.
    /// </summary>
    internal record SchemaEnumDto(
        string Name,
        string Description,
        string EncodingType,
        string SemanticType,
        List<SchemaFieldDto> Choices
    );
}
