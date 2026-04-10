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

        // SBE007: Non-native byte order (informational)
        public static readonly DiagnosticDescriptor NonNativeByteOrder = new DiagnosticDescriptor(
            id: "SBE007",
            title: "Big-endian schema with conditional byte swap",
            messageFormat: "Schema '{0}' specifies byteOrder='{1}'. Generated properties include runtime endianness checks. Set SbeAssumeHostEndianness MSBuild property to eliminate the runtime check.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "The schema specifies big-endian byte order. Generated multi-byte field properties include a BitConverter.IsLittleEndian conditional. To optimize, set <SbeAssumeHostEndianness>LittleEndian</SbeAssumeHostEndianness> in your project file if you know the target host endianness.");

        // SBE008: Unresolved type reference
        public static readonly DiagnosticDescriptor UnresolvedTypeReference = new DiagnosticDescriptor(
            id: "SBE008",
            title: "Unresolved type reference",
            messageFormat: "Type '{0}' could not be resolved to a known primitive or custom type in element '{1}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A type reference in the schema could not be resolved. Check that the type is defined in the <types> section before it is used.");

        // SBE009: Invalid numeric constraint
        public static readonly DiagnosticDescriptor InvalidNumericConstraint = new DiagnosticDescriptor(
            id: "SBE009",
            title: "Invalid numeric constraint",
            messageFormat: "Attribute '{0}' has non-numeric value '{1}' in field '{2}'. Constraint will be ignored.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A minValue or maxValue constraint must be a valid numeric literal. Non-numeric constraints are ignored during validation code generation.");

        // SBE010: Unknown primitive type fallback
        public static readonly DiagnosticDescriptor UnknownPrimitiveTypeFallback = new DiagnosticDescriptor(
            id: "SBE010",
            title: "Unknown primitive type fallback",
            messageFormat: "Primitive type '{0}' is not recognized for {1} lookup in '{2}'. A fallback value will be used, which may produce incorrect generated code.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A primitive type used in the schema does not have a known mapping in the type catalog. The generator will use a fallback value (0 for length, 'default' for null sentinel), but the generated code may be incorrect. Verify the type name in the schema.");
        // SBE011: Set choice exceeds encoding type bit width
        public static readonly DiagnosticDescriptor SetChoiceExceedsBitWidth = new DiagnosticDescriptor(
            id: "SBE011",
            title: "Set choice exceeds encoding type bit width",
            messageFormat: "Choice '{0}' in set '{1}' has bit position {2} which exceeds the maximum ({3}) for encoding type '{4}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A set choice bit position must be less than the bit width of the encoding type (e.g., 0-7 for uint8, 0-15 for uint16). Choices exceeding the width are excluded from generated code.");

        // SBE012: Invalid SbeAssumeHostEndianness value
        public static readonly DiagnosticDescriptor InvalidHostEndianness = new DiagnosticDescriptor(
            id: "SBE012",
            title: "Invalid SbeAssumeHostEndianness value",
            messageFormat: "SbeAssumeHostEndianness has invalid value '{0}'. Expected 'LittleEndian' or 'BigEndian'. Falling back to safe conditional byte-swap behavior.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The SbeAssumeHostEndianness MSBuild property must be 'LittleEndian' or 'BigEndian'. Invalid values cause the generator to emit conditional runtime endianness checks.");
    }
}
