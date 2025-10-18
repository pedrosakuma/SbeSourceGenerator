namespace SbeSourceGenerator.Translations
{
    internal sealed record TypeTranslationResult(
        string SchemaType,
        string PrimitiveType,
        bool IsNullableEncoding,
        string? NullValue
    );
}
