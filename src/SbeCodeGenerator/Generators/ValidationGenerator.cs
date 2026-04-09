using Microsoft.CodeAnalysis;
using SbeSourceGenerator.Diagnostics;
using SbeSourceGenerator.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Generates validation methods for SBE messages and types based on schema constraints.
    /// </summary>
    internal class ValidationGenerator : ICodeGenerator
    {
        public IEnumerable<(string name, string content)> Generate(string ns, ParsedSchema schema, SchemaContext context, SourceProductionContext sourceContext)
        {
            foreach (var messageDto in schema.Messages)
            {
                var validationCode = GenerateMessageValidation(ns, messageDto, context, sourceContext);
                if (validationCode != null)
                {
                    yield return (context.CreateHintName(ns, "Validation", $"{messageDto.Name.FirstCharToUpper()}Validation"), validationCode);
                }
            }

            foreach (var typeDto in schema.Types)
            {
                var validationCode = GenerateTypeValidation(ns, typeDto);
                if (validationCode != null)
                {
                    yield return (context.CreateHintName(ns, "Validation", $"{typeDto.Name}Validation"), validationCode);
                }
            }
        }

        private string? GenerateMessageValidation(string ns, SchemaMessageDto messageDto, SchemaContext context, SourceProductionContext sourceContext)
        {
            var fieldsWithConstraints = messageDto.Fields
                .Where(f => !string.IsNullOrEmpty(f.MinValue) || !string.IsNullOrEmpty(f.MaxValue))
                .ToList();

            if (!fieldsWithConstraints.Any())
                return null;

            // Filter out fields with non-numeric constraints and report diagnostics
            var validFields = new List<SchemaFieldDto>();
            foreach (var field in fieldsWithConstraints)
            {
                bool valid = true;
                if (!string.IsNullOrEmpty(field.MinValue) && !double.TryParse(field.MinValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
                {
                    if (sourceContext.CancellationToken != default)
                    {
                        sourceContext.ReportDiagnostic(Diagnostic.Create(
                            SbeDiagnostics.InvalidNumericConstraint,
                            Location.None,
                            "minValue",
                            field.MinValue,
                            field.Name));
                    }
                    valid = false;
                }
                if (!string.IsNullOrEmpty(field.MaxValue) && !double.TryParse(field.MaxValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
                {
                    if (sourceContext.CancellationToken != default)
                    {
                        sourceContext.ReportDiagnostic(Diagnostic.Create(
                            SbeDiagnostics.InvalidNumericConstraint,
                            Location.None,
                            "maxValue",
                            field.MaxValue,
                            field.Name));
                    }
                    valid = false;
                }
                if (valid)
                    validFields.Add(field);
            }

            if (!validFields.Any())
                return null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.Append("namespace ").Append(ns).AppendLine(";");
            sb.AppendLine();
            sb.AppendLine($"/// <summary>");
            sb.Append("/// Validation extension methods for ").Append(messageDto.Name).AppendLine(".");
            sb.AppendLine($"/// </summary>");
            sb.Append("public static class ").Append(messageDto.Name.FirstCharToUpper()).AppendLine("Validation");
            sb.AppendLine("{");

            // Generate TryValidate method first (contains the core logic)
            sb.AppendLine($"    /// <summary>");
            sb.Append("    /// Attempts to validate all fields with constraints in ").Append(messageDto.Name).AppendLine(".");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <param name=\"message\">The message to validate.</param>");
            sb.AppendLine($"    /// <param name=\"errorMessage\">The error message if validation fails, or null if validation succeeds.</param>");
            sb.AppendLine($"    /// <returns>True if validation succeeds, false otherwise.</returns>");
            sb.Append("    public static bool TryValidate(this ").Append(messageDto.Name.FirstCharToUpper()).AppendLine(" message, out string? errorMessage)");
            sb.AppendLine("    {");

            foreach (var field in validFields)
            {
                string fieldName = field.Name.FirstCharToUpper();

                if (!string.IsNullOrEmpty(field.MinValue) && !string.IsNullOrEmpty(field.MaxValue))
                {
                    sb.Append("        if (message.").Append(fieldName).Append(" < ").Append(field.MinValue).Append(" || message.").Append(fieldName).Append(" > ").Append(field.MaxValue).AppendLine(")");
                    sb.AppendLine("        {");
                    sb.Append("            errorMessage = $\"").Append(fieldName).Append(" must be between ").Append(field.MinValue).Append(" and ").Append(field.MaxValue).Append(". Actual value was {message.").Append(fieldName).AppendLine("}.\";");
                    sb.AppendLine("            return false;");
                    sb.AppendLine("        }");
                }
                else if (!string.IsNullOrEmpty(field.MinValue))
                {
                    sb.Append("        if (message.").Append(fieldName).Append(" < ").Append(field.MinValue).AppendLine(")");
                    sb.AppendLine("        {");
                    sb.Append("            errorMessage = $\"").Append(fieldName).Append(" must be greater than or equal to ").Append(field.MinValue).Append(". Actual value was {message.").Append(fieldName).AppendLine("}.\";");
                    sb.AppendLine("            return false;");
                    sb.AppendLine("        }");
                }
                else if (!string.IsNullOrEmpty(field.MaxValue))
                {
                    sb.Append("        if (message.").Append(fieldName).Append(" > ").Append(field.MaxValue).AppendLine(")");
                    sb.AppendLine("        {");
                    sb.Append("            errorMessage = $\"").Append(fieldName).Append(" must be less than or equal to ").Append(field.MaxValue).Append(". Actual value was {message.").Append(fieldName).AppendLine("}.\";");
                    sb.AppendLine("            return false;");
                    sb.AppendLine("        }");
                }
            }

            sb.AppendLine("        errorMessage = null;");
            sb.AppendLine("        return true;");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Generate Validate method that calls TryValidate
            sb.AppendLine($"    /// <summary>");
            sb.Append("    /// Validates all fields with constraints in ").Append(messageDto.Name).AppendLine(".");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <param name=\"message\">The message to validate.</param>");
            sb.AppendLine($"    /// <exception cref=\"ArgumentOutOfRangeException\">Thrown when a field value is outside its valid range.</exception>");
            sb.Append("    public static void Validate(this ").Append(messageDto.Name.FirstCharToUpper()).AppendLine(" message)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (!message.TryValidate(out string? errorMessage))");
            sb.AppendLine("        {");
            sb.AppendLine("            throw new ArgumentOutOfRangeException(nameof(message), errorMessage);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Generate factory method
            sb.AppendLine($"    /// <summary>");
            sb.Append("    /// Creates a validated instance of ").Append(messageDto.Name.FirstCharToUpper()).AppendLine(".");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <param name=\"message\">The message to validate.</param>");
            sb.AppendLine($"    /// <returns>The validated message.</returns>");
            sb.AppendLine($"    /// <exception cref=\"ArgumentOutOfRangeException\">Thrown when a field value is outside its valid range.</exception>");
            sb.Append("    public static ").Append(messageDto.Name.FirstCharToUpper()).Append(" CreateValidated(this ").Append(messageDto.Name.FirstCharToUpper()).AppendLine(" message)");
            sb.AppendLine("    {");
            sb.AppendLine("        message.Validate();");
            sb.AppendLine("        return message;");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string? GenerateTypeValidation(string ns, SchemaTypeDto typeDto)
        {
            if (string.IsNullOrEmpty(typeDto.MinValue) && string.IsNullOrEmpty(typeDto.MaxValue))
                return null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.Append("namespace ").Append(ns).AppendLine(";");
            sb.AppendLine();
            sb.AppendLine($"/// <summary>");
            sb.Append("/// Validation extension methods for ").Append(typeDto.Name).AppendLine(".");
            sb.AppendLine($"/// </summary>");
            sb.Append("public static class ").Append(typeDto.Name).AppendLine("Validation");
            sb.AppendLine("{");

            // Generate TryValidate method first (contains the core logic)
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Attempts to validate that the value is within the schema-defined constraints.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <param name=\"value\">The value to validate.</param>");
            sb.AppendLine($"    /// <param name=\"errorMessage\">The error message if validation fails, or null if validation succeeds.</param>");
            sb.AppendLine($"    /// <returns>True if validation succeeds, false otherwise.</returns>");
            sb.Append("    public static bool TryValidate(this ").Append(typeDto.Name).AppendLine(" value, out string? errorMessage)");
            sb.AppendLine("    {");

            if (!string.IsNullOrEmpty(typeDto.MinValue) && !string.IsNullOrEmpty(typeDto.MaxValue))
            {
                sb.Append("        if (value.Value < ").Append(typeDto.MinValue).Append(" || value.Value > ").Append(typeDto.MaxValue).AppendLine(")");
                sb.AppendLine("        {");
                sb.Append("            errorMessage = $\"").Append(typeDto.Name).Append(" must be between ").Append(typeDto.MinValue).Append(" and ").Append(typeDto.MaxValue).AppendLine(". Actual value was {value.Value}.\";");
                sb.AppendLine("            return false;");
                sb.AppendLine("        }");
            }
            else if (!string.IsNullOrEmpty(typeDto.MinValue))
            {
                sb.Append("        if (value.Value < ").Append(typeDto.MinValue).AppendLine(")");
                sb.AppendLine("        {");
                sb.Append("            errorMessage = $\"").Append(typeDto.Name).Append(" must be greater than or equal to ").Append(typeDto.MinValue).AppendLine(". Actual value was {value.Value}.\";");
                sb.AppendLine("            return false;");
                sb.AppendLine("        }");
            }
            else if (!string.IsNullOrEmpty(typeDto.MaxValue))
            {
                sb.Append("        if (value.Value > ").Append(typeDto.MaxValue).AppendLine(")");
                sb.AppendLine("        {");
                sb.Append("            errorMessage = $\"").Append(typeDto.Name).Append(" must be less than or equal to ").Append(typeDto.MaxValue).AppendLine(". Actual value was {value.Value}.\";");
                sb.AppendLine("            return false;");
                sb.AppendLine("        }");
            }

            sb.AppendLine("        errorMessage = null;");
            sb.AppendLine("        return true;");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Generate Validate method that calls TryValidate
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Validates that the value is within the schema-defined constraints.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <param name=\"value\">The value to validate.</param>");
            sb.AppendLine($"    /// <exception cref=\"ArgumentOutOfRangeException\">Thrown when the value is outside the valid range.</exception>");
            sb.Append("    public static void Validate(this ").Append(typeDto.Name).AppendLine(" value)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (!value.TryValidate(out string? errorMessage))");
            sb.AppendLine("        {");
            sb.AppendLine("            throw new ArgumentOutOfRangeException(nameof(value), errorMessage);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Generate factory method
            sb.AppendLine($"    /// <summary>");
            sb.Append("    /// Creates a validated instance of ").Append(typeDto.Name).AppendLine(".");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <param name=\"value\">The value to validate.</param>");
            sb.AppendLine($"    /// <returns>The validated value.</returns>");
            sb.AppendLine($"    /// <exception cref=\"ArgumentOutOfRangeException\">Thrown when the value is outside the valid range.</exception>");
            sb.Append("    public static ").Append(typeDto.Name).Append(" CreateValidated(this ").Append(typeDto.Name).AppendLine(" value)");
            sb.AppendLine("    {");
            sb.AppendLine("        value.Validate();");
            sb.AppendLine("        return value;");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
