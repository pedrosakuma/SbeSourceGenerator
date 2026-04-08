using System.Collections.Generic;

namespace SbeSourceGenerator.Schema
{
    /// <summary>
    /// Immutable DTO representing a fully parsed SBE XML schema.
    /// Produced by a single forward-only pass over the XML, eliminating
    /// the need for XmlDocument, XPath queries, and repeated attribute access.
    /// </summary>
    internal record ParsedSchema(
        string ByteOrder,
        string Package,
        string Version,
        string Id,
        string Description,
        string SemanticVersion,
        List<SchemaTypeDto> Types,
        List<SchemaCompositeDto> Composites,
        List<SchemaEnumDto> Enums,
        List<SchemaEnumDto> Sets,
        List<SchemaMessageDto> Messages
    );
}
