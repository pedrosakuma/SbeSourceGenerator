using Microsoft.CodeAnalysis;

namespace SbeSourceGenerator.Diagnostics
{
    /// <summary>
    /// Defines diagnostic descriptors for SBE source generator errors and warnings.
    /// </summary>
    internal static class SbeDiagnostics
    {
        private const string Category = "SbeSourceGenerator";

        // SBE001: Invalid integer attribute value
        public static readonly DiagnosticDescriptor InvalidIntegerAttribute = new DiagnosticDescriptor(
            id: "SBE001",
            title: "Invalid integer attribute value",
            messageFormat: "Attribute '{0}' has invalid integer value '{1}' in element '{2}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "An XML attribute expected to contain an integer has an invalid value.");

        // SBE002: Missing required attribute
        public static readonly DiagnosticDescriptor MissingRequiredAttribute = new DiagnosticDescriptor(
            id: "SBE002",
            title: "Missing required attribute",
            messageFormat: "Required attribute '{0}' is missing or empty in element '{1}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A required XML attribute is missing or has an empty value.");

        // SBE003: Invalid enum flag value
        public static readonly DiagnosticDescriptor InvalidEnumFlagValue = new DiagnosticDescriptor(
            id: "SBE003",
            title: "Invalid enum flag value",
            messageFormat: "Enum flag '{0}' has invalid integer value '{1}' that cannot be used for bit shifting",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "An enum flag value must be a valid integer for bit-shift operations.");

        // SBE004: Malformed schema
        public static readonly DiagnosticDescriptor MalformedSchema = new DiagnosticDescriptor(
            id: "SBE004",
            title: "Malformed schema",
            messageFormat: "Schema file '{0}' is malformed: {1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The XML schema file is malformed or cannot be parsed.");

        // SBE005: Unsupported construct
        public static readonly DiagnosticDescriptor UnsupportedConstruct = new DiagnosticDescriptor(
            id: "SBE005",
            title: "Unsupported schema construct",
            messageFormat: "Unsupported schema construct '{0}' in element '{1}': {2}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The schema contains a construct that is not yet supported by the generator.");

        // SBE006: Invalid type length
        public static readonly DiagnosticDescriptor InvalidTypeLength = new DiagnosticDescriptor(
            id: "SBE006",
            title: "Invalid type length",
            messageFormat: "Type '{0}' has invalid length value '{1}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A type definition has an invalid length attribute value.");
    }
}
