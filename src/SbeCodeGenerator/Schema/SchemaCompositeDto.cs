using System.Collections.Generic;

namespace SbeSourceGenerator.Schema
{
    /// <summary>
    /// Immutable DTO representing a composite type from the XML schema.
    /// </summary>
    internal record SchemaCompositeDto(
        string Name,
        string Description,
        string SemanticType,
        List<SchemaFieldDto> Fields
    );
}
