using System.Collections.Generic;

namespace SbeSourceGenerator.Schema
{
    /// <summary>
    /// Immutable DTO representing a group from the XML schema.
    /// </summary>
    internal record SchemaGroupDto(
        string Name,
        string Id,
        string DimensionType,
        string Description,
        List<SchemaFieldDto> Fields,
        List<SchemaFieldDto> Constants,
        List<SchemaDataDto>? Data = null
    );
}
