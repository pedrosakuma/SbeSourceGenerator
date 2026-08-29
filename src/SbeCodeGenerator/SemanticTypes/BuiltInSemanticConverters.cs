using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace SbeSourceGenerator.SemanticTypes
{
    internal static class BuiltInSemanticConverters
    {
        public const string RuntimeNamespace = "SbeSourceGenerator.Runtime";

        public static readonly IReadOnlyList<SemanticConverterRegistration> All = new[]
        {
            New("UTCTimestampNanos",  "UtcTimestampNanosConverter",  SpecialType.System_UInt64, "global::System.DateTime"),
            New("UTCTimestampMicros", "UtcTimestampMicrosConverter", SpecialType.System_UInt64, "global::System.DateTime"),
            New("UTCTimestampMillis", "UtcTimestampMillisConverter", SpecialType.System_UInt64, "global::System.DateTime"),
            New("UTCTimestamp",       "UtcTimestampSecondsConverter",SpecialType.System_UInt64, "global::System.DateTime"),
            New("UTCDateOnly",        "UtcDateOnlyConverter",        SpecialType.System_UInt16, "global::System.DateOnly"),
            New("LocalMktDate",       "LocalMktDateConverter",       SpecialType.System_UInt16, "global::System.DateOnly"),
            New("UTCTimeOnly",        "UtcTimeOnlyNanosConverter",   SpecialType.System_UInt64, "global::System.TimeOnly"),
            New("MonthYear",          "MonthYearConverter",          SpecialType.System_UInt32, "(int Year, int Month)"),
        };

        private static SemanticConverterRegistration New(string semanticType, string converterTypeName, SpecialType wire, string semanticDisplay) =>
            new SemanticConverterRegistration(
                SemanticType: semanticType,
                ConverterFullyQualifiedName: "global::" + RuntimeNamespace + "." + converterTypeName,
                WireSpecialType: wire,
                SemanticTypeDisplay: semanticDisplay,
                IsBuiltIn: true,
                Location: Location.None);
    }
}
