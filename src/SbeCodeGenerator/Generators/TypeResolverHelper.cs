using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Diagnostics;
using System;
using System.Collections.Generic;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Shared helpers for resolving SBE type names, value references,
    /// and type lengths across code generators.
    /// </summary>
    internal static class TypeResolverHelper
    {
        private static readonly HashSet<string> DotNetPrimitiveTypes = new(StringComparer.Ordinal)
        {
            "bool",
            "byte",
            "sbyte",
            "short",
            "ushort",
            "int",
            "uint",
            "long",
            "ulong",
            "float",
            "double",
            "char",
            "string"
        };

        public static bool IsDotNetPrimitive(string typeName) => DotNetPrimitiveTypes.Contains(typeName);

        public static string ResolveTypeName(string typeName, SchemaContext context)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeName;

            if (IsDotNetPrimitive(typeName))
                return typeName;

            if (context.GeneratedTypeNames.TryGetValue(typeName, out var generated))
                return generated;

            return TypeTranslator.NormalizeName(typeName);
        }

        public static string NormalizeValueRef(string valueRef, SchemaContext context)
        {
            if (string.IsNullOrWhiteSpace(valueRef))
                return valueRef;

            int separatorIndex = valueRef.IndexOf('.');
            string separator = ".";

            if (separatorIndex < 0)
            {
                separatorIndex = valueRef.IndexOf("::", StringComparison.Ordinal);
                if (separatorIndex >= 0)
                    separator = "::";
            }

            if (separatorIndex < 0)
                return ResolveTypeName(valueRef, context);

            var typePart = valueRef.Substring(0, separatorIndex);
            var remainder = valueRef.Substring(separatorIndex + separator.Length);
            var normalizedType = ResolveTypeName(typePart, context);

            if (remainder.Length == 0)
                return normalizedType;

            return string.Concat(normalizedType, separator, remainder);
        }

        public static int GetTypeLength(string type, SchemaContext context, SourceProductionContext sourceContext = default, string elementName = "")
        {
            if (TypesCatalog.PrimitiveTypeLengths.TryGetValue(type, out int length))
                return length;

            if (context.CustomTypeLengths.TryGetValue(type, out length))
                return length;

            var translated = TypeTranslator.Translate(type).PrimitiveType;
            if (TypesCatalog.PrimitiveTypeLengths.TryGetValue(translated, out length))
                return length;

            if (sourceContext.CancellationToken != default)
            {
                sourceContext.ReportDiagnostic(Diagnostic.Create(
                    SbeDiagnostics.UnresolvedTypeReference,
                    Location.None,
                    type,
                    elementName));
            }
            return 0;
        }

        public static string RegisterGeneratedTypeName(SchemaContext context, string originalName, SourceProductionContext sourceContext = default)
        {
            if (string.IsNullOrEmpty(originalName))
                return originalName;

            var normalized = TypeTranslator.NormalizeName(originalName);

            if (context.GeneratedTypeNames.ContainsKey(originalName))
            {
                if (sourceContext.CancellationToken != default)
                {
                    sourceContext.ReportDiagnostic(Diagnostic.Create(
                        SbeDiagnostics.DuplicateTypeName,
                        Location.None,
                        originalName));
                }
            }

            context.GeneratedTypeNames[originalName] = normalized;
            return normalized;
        }
    }
}
