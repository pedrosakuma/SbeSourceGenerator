using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Diagnostics;
using System;
using System.Xml;

namespace SbeSourceGenerator.Helpers
{
    /// <summary>
    /// Static helper class for XML parsing with guard methods to safely retrieve attributes and values.
    /// Reduces duplication and eliminates ad-hoc parsing logic scattered across generators.
    /// </summary>
    internal static class XmlParsingHelpers
    {
        /// <summary>
        /// Gets an attribute value from an XmlElement. Returns empty string if attribute doesn't exist.
        /// </summary>
        public static string GetAttributeOrEmpty(this XmlElement element, string attributeName)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return element.GetAttribute(attributeName) ?? string.Empty;
        }

        /// <summary>
        /// Gets an attribute value from an XmlElement with a fallback default value.
        /// </summary>
        public static string GetAttributeOrDefault(this XmlElement element, string attributeName, string defaultValue)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var value = element.GetAttribute(attributeName);
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        /// <summary>
        /// Gets an attribute value from an XmlElement and validates it's not empty.
        /// Throws an exception if the attribute is missing or empty.
        /// </summary>
        public static string GetRequiredAttribute(this XmlElement element, string attributeName)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var value = element.GetAttribute(attributeName);
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException($"Required attribute '{attributeName}' is missing or empty in element '{element.Name}'");

            return value;
        }

        /// <summary>
        /// Gets an integer attribute value from an XmlElement. Returns null if attribute doesn't exist or is empty.
        /// </summary>
        public static int? GetIntAttributeOrNull(this XmlElement element, string attributeName)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var value = element.GetAttribute(attributeName);
            if (string.IsNullOrEmpty(value))
                return null;

            if (int.TryParse(value, out int result))
                return result;

            throw new InvalidOperationException($"Attribute '{attributeName}' value '{value}' in element '{element.Name}' is not a valid integer");
        }

        /// <summary>
        /// Gets an integer attribute value from an XmlElement with a fallback default value.
        /// </summary>
        public static int GetIntAttributeOrDefault(this XmlElement element, string attributeName, int defaultValue)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var value = element.GetAttribute(attributeName);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            if (int.TryParse(value, out int result))
                return result;

            throw new InvalidOperationException($"Attribute '{attributeName}' value '{value}' in element '{element.Name}' is not a valid integer");
        }

        /// <summary>
        /// Checks if an attribute exists and has a value.
        /// </summary>
        public static bool HasAttribute(this XmlElement element, string attributeName)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return element.HasAttribute(attributeName) && !string.IsNullOrEmpty(element.GetAttribute(attributeName));
        }

        /// <summary>
        /// Gets inner text from an XmlElement. Returns empty string if element is null or has no inner text.
        /// </summary>
        public static string GetInnerTextOrEmpty(this XmlElement element)
        {
            if (element == null)
                return string.Empty;

            return element.InnerText ?? string.Empty;
        }

        // ========== Diagnostic-aware methods ==========

        /// <summary>
        /// Gets an integer attribute value from an XmlElement. Returns null if attribute doesn't exist or is empty.
        /// Emits a diagnostic if the value cannot be parsed as an integer.
        /// </summary>
        public static int? GetIntAttributeOrNull(this XmlElement element, string attributeName, SourceProductionContext context)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var value = element.GetAttribute(attributeName);
            if (string.IsNullOrEmpty(value))
                return null;

            if (int.TryParse(value, out int result))
                return result;

            // Only report diagnostic if context has a valid CancellationToken (not default)
            if (context.CancellationToken != default)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SbeDiagnostics.InvalidIntegerAttribute,
                    Location.None,
                    attributeName,
                    value,
                    element.Name));
            }

            return null;
        }

        /// <summary>
        /// Gets an integer attribute value from an XmlElement with a fallback default value.
        /// Emits a diagnostic if the value cannot be parsed as an integer.
        /// </summary>
        public static int GetIntAttributeOrDefault(this XmlElement element, string attributeName, int defaultValue, SourceProductionContext context)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var value = element.GetAttribute(attributeName);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            if (int.TryParse(value, out int result))
                return result;

            // Only report diagnostic if context has a valid CancellationToken (not default)
            if (context.CancellationToken != default)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SbeDiagnostics.InvalidIntegerAttribute,
                    Location.None,
                    attributeName,
                    value,
                    element.Name));
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets an attribute value from an XmlElement and validates it's not empty.
        /// Emits a diagnostic if the attribute is missing or empty.
        /// </summary>
        public static string GetRequiredAttribute(this XmlElement element, string attributeName, SourceProductionContext context)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var value = element.GetAttribute(attributeName);
            if (string.IsNullOrEmpty(value))
            {
                // Only report diagnostic if context has a valid CancellationToken (not default)
                if (context.CancellationToken != default)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        SbeDiagnostics.MissingRequiredAttribute,
                        Location.None,
                        attributeName,
                        element.Name));
                }

                return string.Empty;
            }

            return value;
        }

        /// <summary>
        /// Safely parses an integer value for enum flag bit-shifting operations.
        /// Emits a diagnostic if the value cannot be parsed.
        /// </summary>
        public static int? ParseEnumFlagValue(string value, string fieldName, SourceProductionContext context)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (int.TryParse(value, out int result))
                return result;

            // Only report diagnostic if context has a valid CancellationToken (not default)
            if (context.CancellationToken != default)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SbeDiagnostics.InvalidEnumFlagValue,
                    Location.None,
                    fieldName,
                    value));
            }

            return null;
        }
    }
}
