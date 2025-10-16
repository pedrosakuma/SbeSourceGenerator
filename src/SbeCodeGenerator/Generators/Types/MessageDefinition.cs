using SbeSourceGenerator.Generators.Fields;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SbeSourceGenerator
{
    public record MessageDefinition(string Namespace, string Name, string Id, string Description, string SemanticType, string Deprecated,
        List<IFileContentGenerator> Fields, List<IFileContentGenerator> Constants,
        List<IFileContentGenerator> Groups, List<IFileContentGenerator> Datas) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            // Add namespace.Runtime using for SpanReader (used in TryParse) and groups/data fields
            // For versioned namespaces (e.g., MySchema.V1), use the base namespace's Runtime (MySchema.Runtime)
            var baseNamespace = Namespace.Contains(".V") 
                ? Namespace.Substring(0, Namespace.LastIndexOf(".V"))
                : Namespace;
            sb.AppendUsings(tabs, $"{baseNamespace}.Runtime");
            
            sb.AppendStructDefinition(tabs, Description, Name, nameof(MessageDefinition), Namespace);

            sb.AppendLine("{", tabs++);
            AppendMessageDefinitionConstants(sb, tabs);
            AppendConstantsFileContent(sb, tabs);
            AppendFieldsFileContent(sb, tabs);
            AppendParseHelpers(sb, tabs);
            AppendEncodeHelpers(sb, tabs);
            AppendGroupsFileContent(sb, tabs);
            AppendConsumeVariable(sb, tabs);
            sb.AppendLine("}", --tabs);
        }

        private void AppendParseHelpers(StringBuilder sb, int tabs)
        {
            // Original TryParse without blockLength parameter for backward compatibility
            sb.AppendLine($"public static bool TryParse(ReadOnlySpan<byte> buffer, out {Name}Data message, out ReadOnlySpan<byte> variableData)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine($"return TryParse(buffer, MESSAGE_SIZE, out message, out variableData);", tabs);
            sb.AppendLine("}", --tabs);
            
            // TryParse with blockLength parameter for schema evolution support
            sb.AppendLine($"public static bool TryParse(ReadOnlySpan<byte> buffer, int blockLength, out {Name}Data message, out ReadOnlySpan<byte> variableData)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("var reader = new SpanReader(buffer);", tabs);
            sb.AppendLine("// Read the message data", tabs);
            sb.AppendLine($"if (!reader.TryRead<{Name}Data>(out message))", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("variableData = default;", tabs);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("", tabs);
            sb.AppendLine("// Handle schema evolution: skip additional bytes if blockLength > MESSAGE_SIZE", tabs);
            sb.AppendLine("var additionalBytes = blockLength - MESSAGE_SIZE;", tabs);
            sb.AppendLine("if (additionalBytes > 0)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("if (!reader.TrySkip(additionalBytes))", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("variableData = default;", tabs);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("variableData = reader.Remaining;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("else", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("// For backward compatibility: variable data starts at blockLength", tabs);
            sb.AppendLine("variableData = buffer.Slice(blockLength);", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("", tabs);
            sb.AppendLine("return true;", tabs);
            sb.AppendLine("}", --tabs);
            
            // Public method that uses SpanReader for parsing - reader is passed by ref to update offset in caller
            // This method is designed for advanced scenarios where users manage their own SpanReader
            // Caller can access remaining data via reader.Remaining after successful parse
            sb.AppendLine($"public static bool TryParseWithReader(ref SpanReader reader, int blockLength, out {Name}Data message)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("// Read the message data", tabs);
            sb.AppendLine($"if (!reader.TryRead<{Name}Data>(out message))", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("", tabs);
            sb.AppendLine("// Handle schema evolution: skip additional bytes if blockLength > MESSAGE_SIZE", tabs);
            sb.AppendLine("var additionalBytes = blockLength - MESSAGE_SIZE;", tabs);
            sb.AppendLine("if (additionalBytes > 0 && !reader.TrySkip(additionalBytes))", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("", tabs);
            sb.AppendLine("return true;", tabs);
            sb.AppendLine("}", --tabs);
        }

        private void AppendEncodeHelpers(StringBuilder sb, int tabs)
        {
            // TryEncode - basic encoding method
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendLine($"/// Encodes this {Name} message to the provided buffer.", tabs);
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendLine("/// <param name=\"buffer\">The destination buffer.</param>", tabs);
            sb.AppendLine("/// <param name=\"bytesWritten\">Number of bytes written on success.</param>", tabs);
            sb.AppendLine("/// <returns>True if encoding succeeded; otherwise, false.</returns>", tabs);
            sb.AppendLine($"public bool TryEncode(Span<byte> buffer, out int bytesWritten)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("if (buffer.Length < MESSAGE_SIZE)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("bytesWritten = 0;", tabs);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("", tabs);
            sb.AppendLine("var writer = new SpanWriter(buffer);", tabs);
            sb.AppendLine("writer.Write(this);", tabs);
            sb.AppendLine("bytesWritten = MESSAGE_SIZE;", tabs);
            sb.AppendLine("return true;", tabs);
            sb.AppendLine("}", --tabs);
            
            // TryEncodeWithWriter - encoding with existing SpanWriter
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendLine($"/// Encodes this {Name} message using an existing SpanWriter.", tabs);
            sb.AppendLine("/// Useful for composing multiple messages or adding headers.", tabs);
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendLine("/// <param name=\"writer\">The writer to use.</param>", tabs);
            sb.AppendLine("/// <returns>True if encoding succeeded; otherwise, false.</returns>", tabs);
            sb.AppendLine($"public bool TryEncodeWithWriter(ref SpanWriter writer)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("return writer.TryWrite(this);", tabs);
            sb.AppendLine("}", --tabs);
            
            // Encode - throwing version
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendLine($"/// Encodes this {Name} message to the provided buffer.", tabs);
            sb.AppendLine("/// Throws InvalidOperationException if encoding fails.", tabs);
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendLine("/// <param name=\"buffer\">The destination buffer.</param>", tabs);
            sb.AppendLine("/// <returns>Number of bytes written.</returns>", tabs);
            sb.AppendLine("/// <exception cref=\"InvalidOperationException\">Thrown when buffer is too small.</exception>", tabs);
            sb.AppendLine($"public int Encode(Span<byte> buffer)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("if (!TryEncode(buffer, out int bytesWritten))", tabs);
            sb.AppendLine($"    throw new InvalidOperationException($\"Failed to encode {Name}Data. Buffer size: {{buffer.Length}}, Required: {{MESSAGE_SIZE}}\");", tabs);
            sb.AppendLine("", tabs);
            sb.AppendLine("return bytesWritten;", tabs);
            sb.AppendLine("}", --tabs);
            
            // If message has groups or varData, generate encoding methods for them
            if (Groups.Any() || Datas.Any())
            {
                AppendVariableDataEncoding(sb, tabs);
            }
        }

        private void AppendVariableDataEncoding(StringBuilder sb, int tabs)
        {
            // Generate BeginEncoding method that returns a SpanWriter positioned after the message
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendLine($"/// Begins encoding this {Name} message with variable-length data support.", tabs);
            sb.AppendLine("/// Use the returned writer to encode groups and varData fields.", tabs);
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendLine("/// <param name=\"buffer\">The destination buffer.</param>", tabs);
            sb.AppendLine("/// <param name=\"writer\">The writer positioned after the message header for writing variable data.</param>", tabs);
            sb.AppendLine("/// <returns>True if encoding started successfully; otherwise, false.</returns>", tabs);
            sb.AppendLine($"public bool BeginEncoding(Span<byte> buffer, out SpanWriter writer)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("if (buffer.Length < MESSAGE_SIZE)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("writer = default;", tabs);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("", tabs);
            sb.AppendLine("writer = new SpanWriter(buffer);", tabs);
            sb.AppendLine("writer.Write(this);", tabs);
            sb.AppendLine("return true;", tabs);
            sb.AppendLine("}", --tabs);

            // Generate helper methods for each group
            foreach (var group in Groups.Cast<GroupDefinition>())
            {
                sb.AppendLine("/// <summary>", tabs);
                sb.AppendLine($"/// Encodes a {group.Name} group into the buffer.", tabs);
                sb.AppendLine("/// </summary>", tabs);
                sb.AppendLine("/// <param name=\"writer\">The writer to use for encoding.</param>", tabs);
                sb.AppendLine($"/// <param name=\"entries\">The {group.Name} entries to encode.</param>", tabs);
                sb.AppendLine("/// <returns>True if encoding succeeded; otherwise, false.</returns>", tabs);
                sb.AppendLine($"public static bool TryEncode{group.Name}(ref SpanWriter writer, ReadOnlySpan<{group.Name}Data> entries)", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine($"// Write group header", tabs);
                sb.AppendLine($"var header = new {group.DimensionType}", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine($"BlockLength = (ushort){group.Name}Data.MESSAGE_SIZE,", tabs);
                sb.AppendLine($"NumInGroup = ({group.NumInGroupType})entries.Length", tabs);
                sb.AppendLine("};", --tabs);
                sb.AppendLine("", tabs);
                sb.AppendLine("if (!writer.TryWrite(header))", tabs);
                sb.AppendLine("    return false;", tabs + 1);
                sb.AppendLine("", tabs);
                sb.AppendLine($"// Write each entry", tabs);
                sb.AppendLine("for (int i = 0; i < entries.Length; i++)", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("if (!writer.TryWrite(entries[i]))", tabs);
                sb.AppendLine("    return false;", tabs + 1);
                sb.AppendLine("}", --tabs);
                sb.AppendLine("", tabs);
                sb.AppendLine("return true;", tabs);
                sb.AppendLine("}", --tabs);
            }

            // Generate helper methods for each varData field
            foreach (var data in Datas.Cast<DataFieldDefinition>())
            {
                sb.AppendLine("/// <summary>", tabs);
                sb.AppendLine($"/// Encodes a {data.Name} varData field into the buffer.", tabs);
                sb.AppendLine("/// </summary>", tabs);
                sb.AppendLine("/// <param name=\"writer\">The writer to use for encoding.</param>", tabs);
                sb.AppendLine($"/// <param name=\"data\">The {data.Name} data to encode.</param>", tabs);
                sb.AppendLine("/// <returns>True if encoding succeeded; otherwise, false.</returns>", tabs);
                sb.AppendLine($"public static bool TryEncode{data.Name.FirstCharToUpper()}(ref SpanWriter writer, ReadOnlySpan<byte> data)", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine($"// Write length prefix (uint8 for VarString8)", tabs);
                sb.AppendLine($"if (data.Length > 255)", tabs);
                sb.AppendLine("    return false;", tabs + 1);
                sb.AppendLine("", tabs);
                sb.AppendLine("if (!writer.TryWrite((byte)data.Length))", tabs);
                sb.AppendLine("    return false;", tabs + 1);
                sb.AppendLine("", tabs);
                sb.AppendLine($"// Write data bytes", tabs);
                sb.AppendLine("if (!writer.TryWriteBytes(data))", tabs);
                sb.AppendLine("    return false;", tabs + 1);
                sb.AppendLine("", tabs);
                sb.AppendLine("return true;", tabs);
                sb.AppendLine("}", --tabs);
            }
        }

        private void AppendConsumeVariable(StringBuilder sb, int tabs)
        {
            if (Groups.Any() || Datas.Any())
            {
                // Original overload that creates its own SpanReader
                sb.AppendLine("public void ConsumeVariableLengthSegments(ReadOnlySpan<byte> buffer, ", tabs);
                foreach (var group in Groups.Cast<GroupDefinition>())
                {
                    sb.Append($"Action<{group.Name}Data> callback{group.Name}, ", tabs);
                }
                foreach (var data in Datas.Cast<DataFieldDefinition>())
                {
                    sb.Append($"{data.Type}.Callback callback{data.Name}, ", tabs);
                }
                sb.Remove(sb.Length - 2, 2);
                sb.AppendLine(")", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("var reader = new SpanReader(buffer);", tabs);
                sb.Append("ConsumeVariableLengthSegments(ref reader, ", tabs);
                foreach (var group in Groups.Cast<GroupDefinition>())
                {
                    sb.Append($"callback{group.Name}, ", tabs);
                }
                foreach (var data in Datas.Cast<DataFieldDefinition>())
                {
                    sb.Append($"callback{data.Name}, ", tabs);
                }
                sb.Remove(sb.Length - 2, 2);
                sb.AppendLine(");", tabs);
                sb.AppendLine("}", --tabs);
                
                // New overload that accepts SpanReader by reference
                sb.AppendLine("public void ConsumeVariableLengthSegments(ref SpanReader reader, ", tabs);
                foreach (var group in Groups.Cast<GroupDefinition>())
                {
                    sb.Append($"Action<{group.Name}Data> callback{group.Name}, ", tabs);
                }
                foreach (var data in Datas.Cast<DataFieldDefinition>())
                {
                    sb.Append($"{data.Type}.Callback callback{data.Name}, ", tabs);
                }
                sb.Remove(sb.Length - 2, 2);
                sb.AppendLine(")", tabs);
                sb.AppendLine("{", tabs++);
                foreach (var group in Groups.Cast<GroupDefinition>())
                {
                    sb.AppendLine($"if (reader.TryRead<{group.DimensionType}>(out var group{group.Name}))", tabs);
                    sb.AppendLine("{", tabs++);
                    sb.AppendLine($"for (int i = 0; i < group{group.Name}.NumInGroup; i++)", tabs);
                    sb.AppendLine("{", tabs++);
                    sb.AppendLine($"if (reader.TryRead<{group.Name}Data>(out var data))", tabs);
                    sb.AppendLine("{", tabs++);
                    sb.AppendLine($"callback{group.Name}(data);", tabs);
                    sb.AppendLine("}", --tabs);
                    sb.AppendLine("}", --tabs);
                    sb.AppendLine("}", --tabs);
                }
                foreach (var data in Datas.Cast<DataFieldDefinition>())
                {
                    sb.AppendLine($"var datas{data.Name} = {data.Type}.Create(reader.Remaining);", tabs);
                    sb.AppendLine($"callback{data.Name}(datas{data.Name});", tabs);
                }
                sb.AppendLine("}", --tabs);
            }
        }

        private void AppendGroupsFileContent(StringBuilder sb, int tabs)
        {
            foreach (var group in Groups)
                group.AppendFileContent(sb, tabs);
        }

        private void AppendMessageDefinitionConstants(StringBuilder sb, int tabs)
        {
            new ConstantMessageFieldDefinition("MessageId", "Id", "int", "Message Id",
                Id.ToString()).AppendFileContent(sb, tabs);
            new ConstantMessageFieldDefinition("MessageSize", "Size", "int", "Message Size",
                Fields.SumFieldLength().ToString()).AppendFileContent(sb, tabs);
        }

        private void AppendConstantsFileContent(StringBuilder sb, int tabs)
        {
            foreach (var field in Constants)
                field.AppendFileContent(sb, tabs);
        }
        private void AppendFieldsFileContent(StringBuilder sb, int tabs)
        {
            int offset = 0;
            foreach (var field in Fields)
            {
                var blittableField = (IBlittableMessageField)field;
                blittableField.Offset ??= offset;
                field.AppendFileContent(sb, tabs);
                offset = blittableField.Offset.Value + blittableField.Length;
            }
        }
    }
}