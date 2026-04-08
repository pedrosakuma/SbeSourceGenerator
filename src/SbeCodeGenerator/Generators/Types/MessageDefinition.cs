using SbeSourceGenerator.Generators.Fields;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SbeSourceGenerator
{
    public record MessageDefinition(string Namespace, string RuntimeNamespace, string Name, string Id, string Description, string SemanticType, string Deprecated,
        List<IFileContentGenerator> Fields, List<IFileContentGenerator> Constants,
        List<IFileContentGenerator> Groups, List<IFileContentGenerator> Datas) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendUsings(tabs, RuntimeNamespace, $"{RuntimeNamespace}.Runtime");

            sb.AppendStructDefinition(tabs, Description, Name, nameof(MessageDefinition), Namespace);

            sb.AppendLine("{", tabs++);
            AppendMessageDefinitionConstants(sb, tabs);
            AppendConstantsFileContent(sb, tabs);
            AppendFieldsFileContent(sb, tabs);
            AppendParseHelpers(sb, tabs);
            AppendEncodeHelpers(sb, tabs);
            AppendGroupsFileContent(sb, tabs);
            AppendConsumeVariable(sb, tabs);
            
            // Generate comprehensive TryEncode method if message has groups or varData
            if (Groups.Any() || Datas.Any())
            {
                AppendComprehensiveTryEncode(sb, tabs);
            }
            
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
            sb.AppendLine("if (blockLength > buffer.Length)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("variableData = default;", tabs);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
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
            // Generate private helper methods for each group (used internally by comprehensive TryEncode)
            foreach (var group in Groups.Cast<GroupDefinition>())
            {
                sb.AppendLine("/// <summary>", tabs);
                sb.AppendLine($"/// Internal helper: Encodes a {group.Name} group into the buffer.", tabs);
                sb.AppendLine("/// Use the comprehensive TryEncode method instead.", tabs);
                sb.AppendLine("/// </summary>", tabs);
                sb.AppendLine($"private static bool TryEncode{group.Name}(ref SpanWriter writer, ReadOnlySpan<{group.Name}Data> entries)", tabs);
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

            // Generate private helper methods for each varData field (used internally by comprehensive TryEncode)
            foreach (var data in Datas.Cast<DataFieldDefinition>())
            {
                sb.AppendLine("/// <summary>", tabs);
                sb.AppendLine($"/// Internal helper: Encodes a {data.Name} varData field into the buffer.", tabs);
                sb.AppendLine("/// Use the comprehensive TryEncode method instead.", tabs);
                sb.AppendLine("/// </summary>", tabs);
                sb.AppendLine($"private static bool TryEncode{data.Name.FirstCharToUpper()}(ref SpanWriter writer, ReadOnlySpan<byte> data)", tabs);
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
                    sb.AppendLine($"if (!reader.TryRead<{group.Name}Data>(out var data))", tabs);
                    sb.AppendLine("{", tabs++);
                    sb.AppendLine("break;", tabs);
                    sb.AppendLine("}", --tabs);
                    sb.AppendLine($"callback{group.Name}(data);", tabs);
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

        private void AppendComprehensiveTryEncode(StringBuilder sb, int tabs)
        {
            // First, generate delegate types for callback-based encoding
            foreach (var group in Groups.Cast<GroupDefinition>())
            {
                sb.AppendLine($"/// <summary>", tabs);
                sb.AppendLine($"/// Delegate for encoding {group.Name} group items one at a time (zero-allocation).", tabs);
                sb.AppendLine($"/// </summary>", tabs);
                sb.AppendLine($"/// <param name=\"index\">Zero-based index of the item to encode.</param>", tabs);
                sb.AppendLine($"/// <param name=\"item\">Reference to fill with the item data.</param>", tabs);
                sb.AppendLine($"public delegate void {group.Name}Encoder(int index, ref {Name}Data.{group.Name}Data item);", tabs);
                sb.AppendLine("", tabs);
            }
            
            // Generate span-based TryEncode method
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendLine($"/// Encodes this {Name}Data message with all variable-length fields in schema-defined order.", tabs);
            sb.AppendLine("/// This method ensures groups and varData are encoded in the correct sequence.", tabs);
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendLine("/// <param name=\"message\">The message to encode.</param>", tabs);
            sb.AppendLine("/// <param name=\"buffer\">The destination buffer.</param>", tabs);
            
            // Add parameters for each group in order
            foreach (var group in Groups.Cast<GroupDefinition>())
            {
                sb.AppendLine($"/// <param name=\"{group.Name.FirstCharToLower()}\">The {group.Name} group entries.</param>", tabs);
            }
            
            // Add parameters for each varData in order
            foreach (var data in Datas.Cast<DataFieldDefinition>())
            {
                sb.AppendLine($"/// <param name=\"{data.Name.FirstCharToLower()}\">The {data.Name} variable-length data.</param>", tabs);
            }
            
            sb.AppendLine("/// <param name=\"bytesWritten\">Number of bytes written on success.</param>", tabs);
            sb.AppendLine("/// <returns>True if encoding succeeded; otherwise, false.</returns>", tabs);
            
            sb.Append($"public static bool TryEncode({Name}Data message, Span<byte> buffer", tabs);
            
            // Add parameters for groups
            foreach (var group in Groups.Cast<GroupDefinition>())
            {
                sb.Append($", ReadOnlySpan<{Name}Data.{group.Name}Data> {group.Name.FirstCharToLower()}");
            }
            
            // Add parameters for varData
            foreach (var data in Datas.Cast<DataFieldDefinition>())
            {
                sb.Append($", ReadOnlySpan<byte> {data.Name.FirstCharToLower()}");
            }
            
            sb.AppendLine(", out int bytesWritten)");
            sb.AppendLine("{", tabs++);
            
            // Encode the message header
            sb.AppendLine("if (buffer.Length < MESSAGE_SIZE)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("bytesWritten = 0;", tabs);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("", tabs);
            sb.AppendLine("var writer = new SpanWriter(buffer);", tabs);
            sb.AppendLine("writer.Write(message);", tabs);
            sb.AppendLine("", tabs);
            
            // Encode groups in schema order
            foreach (var group in Groups.Cast<GroupDefinition>())
            {
                sb.AppendLine($"// Encode {group.Name} group", tabs);
                sb.AppendLine($"if (!TryEncode{group.Name}(ref writer, {group.Name.FirstCharToLower()}))", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("bytesWritten = 0;", tabs);
                sb.AppendLine("return false;", tabs);
                sb.AppendLine("}", --tabs);
                sb.AppendLine("", tabs);
            }
            
            // Encode varData in schema order
            foreach (var data in Datas.Cast<DataFieldDefinition>())
            {
                var capitalizedName = data.Name.FirstCharToUpper();
                sb.AppendLine($"// Encode {data.Name} varData", tabs);
                sb.AppendLine($"if (!TryEncode{capitalizedName}(ref writer, {data.Name.FirstCharToLower()}))", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("bytesWritten = 0;", tabs);
                sb.AppendLine("return false;", tabs);
                sb.AppendLine("}", --tabs);
                sb.AppendLine("", tabs);
            }
            
            sb.AppendLine("bytesWritten = writer.BytesWritten;", tabs);
            sb.AppendLine("return true;", tabs);
            sb.AppendLine("}", --tabs);
            
            // Generate callback-based TryEncode overload for zero-allocation scenarios
            if (Groups.Any())
            {
                sb.AppendLine("", tabs);
                sb.AppendLine("/// <summary>", tabs);
                sb.AppendLine($"/// Encodes this {Name}Data message with all variable-length fields using callbacks (zero-allocation).", tabs);
                sb.AppendLine("/// This method ensures groups and varData are encoded in the correct sequence.", tabs);
                sb.AppendLine("/// Use this overload to avoid array allocations when encoding groups.", tabs);
                sb.AppendLine("/// </summary>", tabs);
                sb.AppendLine("/// <param name=\"message\">The message to encode.</param>", tabs);
                sb.AppendLine("/// <param name=\"buffer\">The destination buffer.</param>", tabs);
                
                // Add parameters for each group (count + encoder)
                foreach (var group in Groups.Cast<GroupDefinition>())
                {
                    sb.AppendLine($"/// <param name=\"{group.Name.FirstCharToLower()}Count\">Number of {group.Name} entries.</param>", tabs);
                    sb.AppendLine($"/// <param name=\"{group.Name.FirstCharToLower()}Encoder\">Callback to encode each {group.Name} entry.</param>", tabs);
                }
                
                // Add parameters for each varData in order
                foreach (var data in Datas.Cast<DataFieldDefinition>())
                {
                    sb.AppendLine($"/// <param name=\"{data.Name.FirstCharToLower()}\">The {data.Name} variable-length data.</param>", tabs);
                }
                
                sb.AppendLine("/// <param name=\"bytesWritten\">Number of bytes written on success.</param>", tabs);
                sb.AppendLine("/// <returns>True if encoding succeeded; otherwise, false.</returns>", tabs);
                
                sb.Append($"public static bool TryEncode({Name}Data message, Span<byte> buffer", tabs);
                
                // Add parameters for groups (count + encoder callback)
                foreach (var group in Groups.Cast<GroupDefinition>())
                {
                    sb.Append($", int {group.Name.FirstCharToLower()}Count, {group.Name}Encoder {group.Name.FirstCharToLower()}Encoder");
                }
                
                // Add parameters for varData
                foreach (var data in Datas.Cast<DataFieldDefinition>())
                {
                    sb.Append($", ReadOnlySpan<byte> {data.Name.FirstCharToLower()}");
                }
                
                sb.AppendLine(", out int bytesWritten)");
                sb.AppendLine("{", tabs++);
                
                // Encode the message header
                sb.AppendLine("if (buffer.Length < MESSAGE_SIZE)", tabs);
                sb.AppendLine("{", tabs++);
                sb.AppendLine("bytesWritten = 0;", tabs);
                sb.AppendLine("return false;", tabs);
                sb.AppendLine("}", --tabs);
                sb.AppendLine("", tabs);
                sb.AppendLine("var writer = new SpanWriter(buffer);", tabs);
                sb.AppendLine("writer.Write(message);", tabs);
                sb.AppendLine("", tabs);
                
                // Encode groups in schema order using callbacks
                foreach (var group in Groups.Cast<GroupDefinition>())
                {
                    var groupNameLower = group.Name.FirstCharToLower();
                    sb.AppendLine($"// Encode {group.Name} group using callback", tabs);
                    sb.AppendLine($"if (!TryEncode{group.Name}WithCallback(ref writer, {groupNameLower}Count, {groupNameLower}Encoder))", tabs);
                    sb.AppendLine("{", tabs++);
                    sb.AppendLine("bytesWritten = 0;", tabs);
                    sb.AppendLine("return false;", tabs);
                    sb.AppendLine("}", --tabs);
                    sb.AppendLine("", tabs);
                }
                
                // Encode varData in schema order
                foreach (var data in Datas.Cast<DataFieldDefinition>())
                {
                    var capitalizedName = data.Name.FirstCharToUpper();
                    sb.AppendLine($"// Encode {data.Name} varData", tabs);
                    sb.AppendLine($"if (!TryEncode{capitalizedName}(ref writer, {data.Name.FirstCharToLower()}))", tabs);
                    sb.AppendLine("{", tabs++);
                    sb.AppendLine("bytesWritten = 0;", tabs);
                    sb.AppendLine("return false;", tabs);
                    sb.AppendLine("}", --tabs);
                    sb.AppendLine("", tabs);
                }
                
                sb.AppendLine("bytesWritten = writer.BytesWritten;", tabs);
                sb.AppendLine("return true;", tabs);
                sb.AppendLine("}", --tabs);
                
                // Generate helper methods for callback-based group encoding
                foreach (var group in Groups.Cast<GroupDefinition>())
                {
                    sb.AppendLine("", tabs);
                    sb.AppendLine($"private static bool TryEncode{group.Name}WithCallback(ref SpanWriter writer, int count, {group.Name}Encoder encoder)", tabs);
                    sb.AppendLine("{", tabs++);
                    sb.AppendLine($"// Write group header", tabs);
                    sb.AppendLine($"var header = new {group.DimensionType}", tabs);
                    sb.AppendLine("{", tabs++);
                    sb.AppendLine($"BlockLength = (ushort){group.Name}Data.MESSAGE_SIZE,", tabs);
                    sb.AppendLine($"NumInGroup = ({group.NumInGroupType})count", tabs);
                    sb.AppendLine("};", --tabs);
                    sb.AppendLine("", tabs);
                    sb.AppendLine("if (!writer.TryWrite(header))", tabs);
                    sb.AppendLine("    return false;", tabs + 1);
                    sb.AppendLine("", tabs);
                    sb.AppendLine($"// Encode each entry using callback", tabs);
                    sb.AppendLine("for (int i = 0; i < count; i++)", tabs);
                    sb.AppendLine("{", tabs++);
                    sb.AppendLine($"var item = new {group.Name}Data();", tabs);
                    sb.AppendLine("encoder(i, ref item);", tabs);
                    sb.AppendLine("if (!writer.TryWrite(item))", tabs);
                    sb.AppendLine("    return false;", tabs + 1);
                    sb.AppendLine("}", --tabs);
                    sb.AppendLine("", tabs);
                    sb.AppendLine("return true;", tabs);
                    sb.AppendLine("}", --tabs);
                }
            }
        }

    }
}