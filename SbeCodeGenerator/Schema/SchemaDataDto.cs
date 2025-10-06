namespace SbeSourceGenerator.Schema
{
    /// <summary>
    /// Immutable DTO representing a data field from the XML schema.
    /// </summary>
    internal record SchemaDataDto(
        string Name,
        string Id,
        string Type,
        string Description
    );
}
