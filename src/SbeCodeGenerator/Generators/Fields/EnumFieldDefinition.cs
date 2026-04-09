namespace SbeSourceGenerator
{
    public record EnumFieldDefinition(string Name, string Description, string Value, string SinceVersion = "", string Deprecated = "");
}
