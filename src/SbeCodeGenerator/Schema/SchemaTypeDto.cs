namespace SbeSourceGenerator.Schema
{
    /// <summary>
    /// Immutable DTO representing a simple type from the XML schema.
    /// </summary>
    internal record SchemaTypeDto(
        string Name,
        string Description,
        string PrimitiveType,
        string SemanticType,
        string Presence,
        string NullValue,
        string Length,
        string InnerText
    );
}
