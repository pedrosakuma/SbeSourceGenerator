using Microsoft.CodeAnalysis;

namespace SbeSourceGenerator.SemanticTypes
{
    public sealed record SemanticConverterRegistration(
        string SemanticType,
        string ConverterFullyQualifiedName,
        SpecialType WireSpecialType,
        string SemanticTypeDisplay,
        bool IsBuiltIn,
        Location Location);
}
