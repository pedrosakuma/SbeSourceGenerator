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
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();
            sb.AppendLine($"/// <summary>");
            sb.AppendLine($"/// Validation extension methods for {messageDto.Name}.");
            sb.AppendLine($"/// </summary>");
            sb.AppendLine($"public static class {messageDto.Name.FirstCharToUpper()}Validation");
            sb.AppendLine("{");

            // Generate TryValidate method first (contains the core logic)
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Attempts to validate all fields with constraints in {messageDto.Name}.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <param name=\"message\">The message to validate.</param>");
            sb.AppendLine($"    /// <param name=\"errorMessage\">The error message if validation fails, or null if validation succeeds.</param>");
            sb.AppendLine($"    /// <returns>True if validation succeeds, false otherwise.</returns>");
            sb.AppendLine($"    public static bool TryValidate(this {messageDto.Name.FirstCharToUpper()} message, out string? errorMessage)");
            sb.AppendLine("    {");

            foreach (var field in validFields)
            {
                string fieldName = field.Name.FirstCharToUpper();

                if (!string.IsNullOrEmpty(field.MinValue) && !string.IsNullOrEmpty(field.MaxValue))
                {
                    sb.AppendLine($"        if (message.{fieldName} < {field.MinValue} || message.{fieldName} > {field.MaxValue})");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            errorMessage = $\"{fieldName} must be between {field.MinValue} and {field.MaxValue}. Actual value was {{message.{fieldName}}}.\";");
                    sb.AppendLine("            return false;");
                    sb.AppendLine("        }");
                }
                else if (!string.IsNullOrEmpty(field.MinValue))
                {
                    sb.AppendLine($"        if (message.{fieldName} < {field.MinValue})");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            errorMessage = $\"{fieldName} must be greater than or equal to {field.MinValue}. Actual value was {{message.{fieldName}}}.\";");
                    sb.AppendLine("            return false;");
                    sb.AppendLine("        }");
                }
                else if (!string.IsNullOrEmpty(field.MaxValue))
                {
                    sb.AppendLine($"        if (message.{fieldName} > {field.MaxValue})");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            errorMessage = $\"{fieldName} must be less than or equal to {field.MaxValue}. Actual value was {{message.{fieldName}}}.\";");
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
            sb.AppendLine($"    /// Validates all fields with constraints in {messageDto.Name}.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <param name=\"message\">The message to validate.</param>");
            sb.AppendLine($"    /// <exception cref=\"ArgumentOutOfRangeException\">Thrown when a field value is outside its valid range.</exception>");
            sb.AppendLine($"    public static void Validate(this {messageDto.Name.FirstCharToUpper()} message)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (!message.TryValidate(out string? errorMessage))");
            sb.AppendLine("        {");
            sb.AppendLine("            throw new ArgumentOutOfRangeException(nameof(message), errorMessage);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Generate factory method
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Creates a validated instance of {messageDto.Name.FirstCharToUpper()}.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <param name=\"message\">The message to validate.</param>");
            sb.AppendLine($"    /// <returns>The validated message.</returns>");
            sb.AppendLine($"    /// <exception cref=\"ArgumentOutOfRangeException\">Thrown when a field value is outside its valid range.</exception>");
            sb.AppendLine($"    public static {messageDto.Name.FirstCharToUpper()} CreateValidated(this {messageDto.Name.FirstCharToUpper()} message)");
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
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();
            sb.AppendLine($"/// <summary>");
            sb.AppendLine($"/// Validation extension methods for {typeDto.Name}.");
            sb.AppendLine($"/// </summary>");
            sb.AppendLine($"public static class {typeDto.Name}Validation");
            sb.AppendLine("{");

            // Generate TryValidate method first (contains the core logic)
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Attempts to validate that the value is within the schema-defined constraints.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <param name=\"value\">The value to validate.</param>");
            sb.AppendLine($"    /// <param name=\"errorMessage\">The error message if validation fails, or null if validation succeeds.</param>");
            sb.AppendLine($"    /// <returns>True if validation succeeds, false otherwise.</returns>");
            sb.AppendLine($"    public static bool TryValidate(this {typeDto.Name} value, out string? errorMessage)");
            sb.AppendLine("    {");

            if (!string.IsNullOrEmpty(typeDto.MinValue) && !string.IsNullOrEmpty(typeDto.MaxValue))
            {
                sb.AppendLine($"        if (value.Value < {typeDto.MinValue} || value.Value > {typeDto.MaxValue})");
                sb.AppendLine("        {");
                sb.AppendLine($"            errorMessage = $\"{typeDto.Name} must be between {typeDto.MinValue} and {typeDto.MaxValue}. Actual value was {{value.Value}}.\";");
                sb.AppendLine("            return false;");
                sb.AppendLine("        }");
            }
            else if (!string.IsNullOrEmpty(typeDto.MinValue))
            {
                sb.AppendLine($"        if (value.Value < {typeDto.MinValue})");
                sb.AppendLine("        {");
                sb.AppendLine($"            errorMessage = $\"{typeDto.Name} must be greater than or equal to {typeDto.MinValue}. Actual value was {{value.Value}}.\";");
                sb.AppendLine("            return false;");
                sb.AppendLine("        }");
            }
            else if (!string.IsNullOrEmpty(typeDto.MaxValue))
            {
                sb.AppendLine($"        if (value.Value > {typeDto.MaxValue})");
                sb.AppendLine("        {");
                sb.AppendLine($"            errorMessage = $\"{typeDto.Name} must be less than or equal to {typeDto.MaxValue}. Actual value was {{value.Value}}.\";");
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
            sb.AppendLine($"    public static void Validate(this {typeDto.Name} value)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (!value.TryValidate(out string? errorMessage))");
            sb.AppendLine("        {");
            sb.AppendLine("            throw new ArgumentOutOfRangeException(nameof(value), errorMessage);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Generate factory method
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Creates a validated instance of {typeDto.Name}.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <param name=\"value\">The value to validate.</param>");
            sb.AppendLine($"    /// <returns>The validated value.</returns>");
            sb.AppendLine($"    /// <exception cref=\"ArgumentOutOfRangeException\">Thrown when the value is outside the valid range.</exception>");
            sb.AppendLine($"    public static {typeDto.Name} CreateValidated(this {typeDto.Name} value)");
            sb.AppendLine("    {");
            sb.AppendLine("        value.Validate();");
            sb.AppendLine("        return value;");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
