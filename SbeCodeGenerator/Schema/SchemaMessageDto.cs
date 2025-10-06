using System.Collections.Generic;

namespace SbeSourceGenerator.Schema
{
    /// <summary>
    /// Immutable DTO representing a message from the XML schema.
    /// </summary>
    internal record SchemaMessageDto(
        string Name,
        string Id,
        string Description,
        string SemanticType,
        string Deprecated,
        List<SchemaFieldDto> Fields,
        List<SchemaFieldDto> Constants,
        List<SchemaGroupDto> Groups,
        List<SchemaDataDto> Data
    );
}
