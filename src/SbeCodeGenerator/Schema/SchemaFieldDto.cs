namespace SbeSourceGenerator.Schema
{
    /// <summary>
    /// Immutable DTO representing a field from the XML schema.
    /// Used in composites, messages, groups, and types.
    /// </summary>
    internal record SchemaFieldDto(
        string Name,
        string Description,
        string PrimitiveType,
        string Presence,
        string Length,
        string NullValue,
        string ValueRef,
        string InnerText,
        string Id,
        string Offset,
        string Type,
        string SinceVersion,
        string MinValue,
        string MaxValue,
        string Deprecated
    );
}
