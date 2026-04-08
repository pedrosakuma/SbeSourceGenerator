using SbeSourceGenerator.Generators.Fields;
using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    public record MessageDefinition(string Namespace, string RuntimeNamespace, string Name, string Id, string Description, string SemanticType, string Deprecated,
        List<IFileContentGenerator> Fields, List<IFileContentGenerator> Constants,
        List<IFileContentGenerator> Groups, List<IFileContentGenerator> Datas) : IFileContentGenerator
    {
        private List<GroupDefinition>? _typedGroups;
        private List<DataFieldDefinition>? _typedDatas;

        private List<GroupDefinition> TypedGroups
        {
            get
            {
                if (_typedGroups == null)
                {
                    _typedGroups = new List<GroupDefinition>(Groups.Count);
                    foreach (var item in Groups)
                        _typedGroups.Add((GroupDefinition)item);
                }
                return _typedGroups;
            }
        }

        private List<DataFieldDefinition> TypedDatas
        {
            get
            {
                if (_typedDatas == null)
                {
                    _typedDatas = new List<DataFieldDefinition>(Datas.Count);
                    foreach (var item in Datas)
                        _typedDatas.Add((DataFieldDefinition)item);
                }
                return _typedDatas;
            }
        }

        private bool HasVariableData => Groups.Count > 0 || Datas.Count > 0;

        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendUsings(tabs, RuntimeNamespace, $"{RuntimeNamespace}.Runtime", "System.Runtime.CompilerServices");

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
            if (HasVariableData)
            {
                AppendComprehensiveTryEncode(sb, tabs);
            }
            
            sb.AppendLine("}", --tabs);
        }

        private void AppendParseHelpers(StringBuilder sb, int tabs)
        {
            // Original TryParse without blockLength parameter for backward compatibility
            sb.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]", tabs);
            sb.AppendTabs(tabs).Append("public static bool TryParse(ReadOnlySpan<byte> buffer, out ").Append(Name).AppendLine("Data message, out ReadOnlySpan<byte> variableData)");
            sb.AppendLine("{", tabs++);
            sb.AppendTabs(tabs).AppendLine("return TryParse(buffer, MESSAGE_SIZE, out message, out variableData);");
            sb.AppendLine("}", --tabs);

            // TryParse with blockLength parameter for schema evolution support
            sb.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]", tabs);
            sb.AppendTabs(tabs).Append("public static bool TryParse(ReadOnlySpan<byte> buffer, int blockLength, out ").Append(Name).AppendLine("Data message, out ReadOnlySpan<byte> variableData)");
            sb.AppendLine("{", tabs++);
            sb.AppendLine("var reader = new SpanReader(buffer);", tabs);
            sb.AppendLine("// Read the message data", tabs);
            sb.AppendTabs(tabs).Append("if (!reader.TryRead<").Append(Name).AppendLine("Data>(out message))");
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
            sb.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]", tabs);
            sb.AppendTabs(tabs).Append("public static bool TryParseWithReader(ref SpanReader reader, int blockLength, out ").Append(Name).AppendLine("Data message)");
            sb.AppendLine("{", tabs++);
            sb.AppendLine("// Read the message data", tabs);
            sb.AppendTabs(tabs).Append("if (!reader.TryRead<").Append(Name).AppendLine("Data>(out message))");
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
            sb.AppendTabs(tabs).Append("/// Encodes this ").Append(Name).AppendLine(" message to the provided buffer.");
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendLine("/// <param name=\"buffer\">The destination buffer.</param>", tabs);
            sb.AppendLine("/// <param name=\"bytesWritten\">Number of bytes written on success.</param>", tabs);
            sb.AppendLine("/// <returns>True if encoding succeeded; otherwise, false.</returns>", tabs);
            sb.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]", tabs);
            sb.AppendTabs(tabs).AppendLine("public bool TryEncode(Span<byte> buffer, out int bytesWritten)");
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
            sb.AppendTabs(tabs).Append("/// Encodes this ").Append(Name).AppendLine(" message using an existing SpanWriter.");
            sb.AppendLine("/// Useful for composing multiple messages or adding headers.", tabs);
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendLine("/// <param name=\"writer\">The writer to use.</param>", tabs);
            sb.AppendLine("/// <returns>True if encoding succeeded; otherwise, false.</returns>", tabs);
            sb.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]", tabs);
            sb.AppendTabs(tabs).AppendLine("public bool TryEncodeWithWriter(ref SpanWriter writer)");
            sb.AppendLine("{", tabs++);
            sb.AppendLine("return writer.TryWrite(this);", tabs);
            sb.AppendLine("}", --tabs);

            // Encode - throwing version
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendTabs(tabs).Append("/// Encodes this ").Append(Name).AppendLine(" message to the provided buffer.");
            sb.AppendLine("/// Throws InvalidOperationException if encoding fails.", tabs);
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendLine("/// <param name=\"buffer\">The destination buffer.</param>", tabs);
            sb.AppendLine("/// <returns>Number of bytes written.</returns>", tabs);
            sb.AppendLine("/// <exception cref=\"InvalidOperationException\">Thrown when buffer is too small.</exception>", tabs);
            sb.AppendTabs(tabs).AppendLine("public int Encode(Span<byte> buffer)");
            sb.AppendLine("{", tabs++);
            sb.AppendLine("if (!TryEncode(buffer, out int bytesWritten))", tabs);
            sb.AppendTabs(tabs).Append("    throw new InvalidOperationException($\"Failed to encode ").Append(Name).AppendLine("Data. Buffer size: {buffer.Length}, Required: {MESSAGE_SIZE}\");");
            sb.AppendLine("", tabs);
            sb.AppendLine("return bytesWritten;", tabs);
            sb.AppendLine("}", --tabs);

            // If message has groups or varData, generate encoding methods for them
            if (HasVariableData)
            {
                AppendVariableDataEncoding(sb, tabs);
            }
        }

        private void AppendVariableDataEncoding(StringBuilder sb, int tabs)
        {
            // Generate private helper methods for each group (used internally by comprehensive TryEncode)
            foreach (var group in TypedGroups)
            {
                sb.AppendLine("/// <summary>", tabs);
                sb.AppendTabs(tabs).Append("/// Internal helper: Encodes a ").Append(group.Name).AppendLine(" group into the buffer.");
                sb.AppendLine("/// Use the comprehensive TryEncode method instead.", tabs);
                sb.AppendLine("/// </summary>", tabs);
                sb.AppendTabs(tabs).Append("private static bool TryEncode").Append(group.Name).Append("(ref SpanWriter writer, ReadOnlySpan<").Append(group.Name).AppendLine("Data> entries)");
                sb.AppendLine("{", tabs++);
                sb.AppendTabs(tabs).AppendLine("// Write group header");
                sb.AppendTabs(tabs).Append("var header = new ").Append(group.DimensionType).AppendLine();
                sb.AppendLine("{", tabs++);
                sb.AppendTabs(tabs).Append("BlockLength = (ushort)").Append(group.Name).AppendLine("Data.MESSAGE_SIZE,");
                sb.AppendTabs(tabs).Append("NumInGroup = (").Append(group.NumInGroupType).AppendLine(")entries.Length");
                sb.AppendLine("};", --tabs);
                sb.AppendLine("", tabs);
                sb.AppendLine("if (!writer.TryWrite(header))", tabs);
                sb.AppendLine("    return false;", tabs + 1);
                sb.AppendLine("", tabs);
                sb.AppendTabs(tabs).AppendLine("// Write each entry");
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
            foreach (var data in TypedDatas)
            {
                sb.AppendLine("/// <summary>", tabs);
                sb.AppendTabs(tabs).Append("/// Internal helper: Encodes a ").Append(data.Name).AppendLine(" varData field into the buffer.");
                sb.AppendLine("/// Use the comprehensive TryEncode method instead.", tabs);
                sb.AppendLine("/// </summary>", tabs);
                sb.AppendTabs(tabs).Append("private static bool TryEncode").Append(data.Name.FirstCharToUpper()).AppendLine("(ref SpanWriter writer, ReadOnlySpan<byte> data)");
                sb.AppendLine("{", tabs++);
                sb.AppendTabs(tabs).AppendLine("// Write length prefix (uint8 for VarString8)");
                sb.AppendTabs(tabs).AppendLine("if (data.Length > 255)");
                sb.AppendLine("    return false;", tabs + 1);
                sb.AppendLine("", tabs);
                sb.AppendLine("if (!writer.TryWrite((byte)data.Length))", tabs);
                sb.AppendLine("    return false;", tabs + 1);
                sb.AppendLine("", tabs);
                sb.AppendTabs(tabs).AppendLine("// Write data bytes");
                sb.AppendLine("if (!writer.TryWriteBytes(data))", tabs);
                sb.AppendLine("    return false;", tabs + 1);
                sb.AppendLine("", tabs);
                sb.AppendLine("return true;", tabs);
                sb.AppendLine("}", --tabs);
            }
        }

        private void AppendConsumeVariable(StringBuilder sb, int tabs)
        {
            if (HasVariableData)
            {
                // Build callback parameter lists once
                var callbackParams = BuildCallbackParams();
                var callbackArgs = BuildCallbackArgs();

                // Original overload that creates its own SpanReader
                sb.AppendTabs(tabs).Append("public void ConsumeVariableLengthSegments(ReadOnlySpan<byte> buffer, ").Append(callbackParams).AppendLine(")");
                sb.AppendLine("{", tabs++);
                sb.AppendLine("var reader = new SpanReader(buffer);", tabs);
                sb.AppendTabs(tabs).Append("ConsumeVariableLengthSegments(ref reader, ").Append(callbackArgs).AppendLine(");");
                sb.AppendLine("}", --tabs);

                // New overload that accepts SpanReader by reference
                sb.AppendTabs(tabs).Append("public void ConsumeVariableLengthSegments(ref SpanReader reader, ").Append(callbackParams).AppendLine(")");
                sb.AppendLine("{", tabs++);
                foreach (var group in TypedGroups)
                {
                    sb.AppendTabs(tabs).Append("if (reader.TryRead<").Append(group.DimensionType).Append(">(out var group").Append(group.Name).AppendLine("))");
                    sb.AppendLine("{", tabs++);
                    sb.AppendTabs(tabs).Append("for (int i = 0; i < group").Append(group.Name).AppendLine(".NumInGroup; i++)");
                    sb.AppendLine("{", tabs++);
                    sb.AppendTabs(tabs).Append("if (!reader.TryRead<").Append(group.Name).AppendLine("Data>(out var data))");
                    sb.AppendLine("{", tabs++);
                    sb.AppendLine("break;", tabs);
                    sb.AppendLine("}", --tabs);
                    sb.AppendTabs(tabs).Append("callback").Append(group.Name).AppendLine("(data);");
                    sb.AppendLine("}", --tabs);
                    sb.AppendLine("}", --tabs);
                }
                foreach (var data in TypedDatas)
                {
                    sb.AppendTabs(tabs).Append("var datas").Append(data.Name).Append(" = ").Append(data.Type).AppendLine(".Create(reader.Remaining);");
                    sb.AppendTabs(tabs).Append("callback").Append(data.Name).Append("(datas").Append(data.Name).AppendLine(");");
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
            // Offsets are already computed by SumFieldLength() in AppendMessageDefinitionConstants
            foreach (var field in Fields)
                field.AppendFileContent(sb, tabs);
        }

        private void AppendComprehensiveTryEncode(StringBuilder sb, int tabs)
        {
            // First, generate delegate types for callback-based encoding
            foreach (var group in TypedGroups)
            {
                sb.AppendTabs(tabs).AppendLine("/// <summary>");
                sb.AppendTabs(tabs).Append("/// Delegate for encoding ").Append(group.Name).AppendLine(" group items one at a time (zero-allocation).");
                sb.AppendTabs(tabs).AppendLine("/// </summary>");
                sb.AppendTabs(tabs).AppendLine("/// <param name=\"index\">Zero-based index of the item to encode.</param>");
                sb.AppendTabs(tabs).AppendLine("/// <param name=\"item\">Reference to fill with the item data.</param>");
                sb.AppendTabs(tabs).Append("public delegate void ").Append(group.Name).Append("Encoder(int index, ref ").Append(Name).Append("Data.").Append(group.Name).AppendLine("Data item);");
                sb.AppendLine("", tabs);
            }
            
            // Generate span-based TryEncode method
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendTabs(tabs).Append("/// Encodes this ").Append(Name).AppendLine("Data message with all variable-length fields in schema-defined order.");
            sb.AppendLine("/// This method ensures groups and varData are encoded in the correct sequence.", tabs);
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendLine("/// <param name=\"message\">The message to encode.</param>", tabs);
            sb.AppendLine("/// <param name=\"buffer\">The destination buffer.</param>", tabs);
            
            // Add parameters for each group in order
            foreach (var group in TypedGroups)
            {
                sb.AppendTabs(tabs).Append("/// <param name=\"").Append(group.Name.FirstCharToLower()).Append("\">The ").Append(group.Name).AppendLine(" group entries.</param>");
            }
            
            // Add parameters for each varData in order
            foreach (var data in TypedDatas)
            {
                sb.AppendTabs(tabs).Append("/// <param name=\"").Append(data.Name.FirstCharToLower()).Append("\">The ").Append(data.Name).AppendLine(" variable-length data.</param>");
            }
            
            sb.AppendLine("/// <param name=\"bytesWritten\">Number of bytes written on success.</param>", tabs);
            sb.AppendLine("/// <returns>True if encoding succeeded; otherwise, false.</returns>", tabs);
            
            sb.AppendTabs(tabs).Append("public static bool TryEncode(").Append(Name).Append("Data message, Span<byte> buffer");
            
            // Add parameters for groups
            foreach (var group in TypedGroups)
            {
                sb.Append(", ReadOnlySpan<").Append(Name).Append("Data.").Append(group.Name).Append("Data> ").Append(group.Name.FirstCharToLower());
            }
            
            // Add parameters for varData
            foreach (var data in TypedDatas)
            {
                sb.Append(", ReadOnlySpan<byte> ").Append(data.Name.FirstCharToLower());
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
            foreach (var group in TypedGroups)
            {
                sb.AppendTabs(tabs).Append("// Encode ").Append(group.Name).AppendLine(" group");
                sb.AppendTabs(tabs).Append("if (!TryEncode").Append(group.Name).Append("(ref writer, ").Append(group.Name.FirstCharToLower()).AppendLine("))");
                sb.AppendLine("{", tabs++);
                sb.AppendLine("bytesWritten = 0;", tabs);
                sb.AppendLine("return false;", tabs);
                sb.AppendLine("}", --tabs);
                sb.AppendLine("", tabs);
            }
            
            // Encode varData in schema order
            foreach (var data in TypedDatas)
            {
                var capitalizedName = data.Name.FirstCharToUpper();
                sb.AppendTabs(tabs).Append("// Encode ").Append(data.Name).AppendLine(" varData");
                sb.AppendTabs(tabs).Append("if (!TryEncode").Append(capitalizedName).Append("(ref writer, ").Append(data.Name.FirstCharToLower()).AppendLine("))");
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
            if (Groups.Count > 0)
            {
                sb.AppendLine("", tabs);
                sb.AppendLine("/// <summary>", tabs);
                sb.AppendTabs(tabs).Append("/// Encodes this ").Append(Name).AppendLine("Data message with all variable-length fields using callbacks (zero-allocation).");
                sb.AppendLine("/// This method ensures groups and varData are encoded in the correct sequence.", tabs);
                sb.AppendLine("/// Use this overload to avoid array allocations when encoding groups.", tabs);
                sb.AppendLine("/// </summary>", tabs);
                sb.AppendLine("/// <param name=\"message\">The message to encode.</param>", tabs);
                sb.AppendLine("/// <param name=\"buffer\">The destination buffer.</param>", tabs);
                
                // Add parameters for each group (count + encoder)
                foreach (var group in TypedGroups)
                {
                    sb.AppendTabs(tabs).Append("/// <param name=\"").Append(group.Name.FirstCharToLower()).Append("Count\">Number of ").Append(group.Name).AppendLine(" entries.</param>");
                    sb.AppendTabs(tabs).Append("/// <param name=\"").Append(group.Name.FirstCharToLower()).Append("Encoder\">Callback to encode each ").Append(group.Name).AppendLine(" entry.</param>");
                }
                
                // Add parameters for each varData in order
                foreach (var data in TypedDatas)
                {
                    sb.AppendTabs(tabs).Append("/// <param name=\"").Append(data.Name.FirstCharToLower()).Append("\">The ").Append(data.Name).AppendLine(" variable-length data.</param>");
                }
                
                sb.AppendLine("/// <param name=\"bytesWritten\">Number of bytes written on success.</param>", tabs);
                sb.AppendLine("/// <returns>True if encoding succeeded; otherwise, false.</returns>", tabs);
                
                sb.AppendTabs(tabs).Append("public static bool TryEncode(").Append(Name).Append("Data message, Span<byte> buffer");
                
                // Add parameters for groups (count + encoder callback)
                foreach (var group in TypedGroups)
                {
                    sb.Append(", int ").Append(group.Name.FirstCharToLower()).Append("Count, ").Append(group.Name).Append("Encoder ").Append(group.Name.FirstCharToLower()).Append("Encoder");
                }
                
                // Add parameters for varData
                foreach (var data in TypedDatas)
                {
                    sb.Append(", ReadOnlySpan<byte> ").Append(data.Name.FirstCharToLower());
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
                foreach (var group in TypedGroups)
                {
                    var groupNameLower = group.Name.FirstCharToLower();
                    sb.AppendTabs(tabs).Append("// Encode ").Append(group.Name).AppendLine(" group using callback");
                    sb.AppendTabs(tabs).Append("if (!TryEncode").Append(group.Name).Append("WithCallback(ref writer, ").Append(groupNameLower).Append("Count, ").Append(groupNameLower).AppendLine("Encoder))");
                    sb.AppendLine("{", tabs++);
                    sb.AppendLine("bytesWritten = 0;", tabs);
                    sb.AppendLine("return false;", tabs);
                    sb.AppendLine("}", --tabs);
                    sb.AppendLine("", tabs);
                }
                
                // Encode varData in schema order
                foreach (var data in TypedDatas)
                {
                    var capitalizedName = data.Name.FirstCharToUpper();
                    sb.AppendTabs(tabs).Append("// Encode ").Append(data.Name).AppendLine(" varData");
                    sb.AppendTabs(tabs).Append("if (!TryEncode").Append(capitalizedName).Append("(ref writer, ").Append(data.Name.FirstCharToLower()).AppendLine("))");
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
                foreach (var group in TypedGroups)
                {
                    sb.AppendLine("", tabs);
                    sb.AppendTabs(tabs).Append("private static bool TryEncode").Append(group.Name).Append("WithCallback(ref SpanWriter writer, int count, ").Append(group.Name).AppendLine("Encoder encoder)");
                    sb.AppendLine("{", tabs++);
                    sb.AppendTabs(tabs).AppendLine("// Write group header");
                    sb.AppendTabs(tabs).Append("var header = new ").Append(group.DimensionType).AppendLine();
                    sb.AppendLine("{", tabs++);
                    sb.AppendTabs(tabs).Append("BlockLength = (ushort)").Append(group.Name).AppendLine("Data.MESSAGE_SIZE,");
                    sb.AppendTabs(tabs).Append("NumInGroup = (").Append(group.NumInGroupType).AppendLine(")count");
                    sb.AppendLine("};", --tabs);
                    sb.AppendLine("", tabs);
                    sb.AppendLine("if (!writer.TryWrite(header))", tabs);
                    sb.AppendLine("    return false;", tabs + 1);
                    sb.AppendLine("", tabs);
                    sb.AppendTabs(tabs).AppendLine("// Encode each entry using callback");
                    sb.AppendLine("for (int i = 0; i < count; i++)", tabs);
                    sb.AppendLine("{", tabs++);
                    sb.AppendTabs(tabs).Append("var item = new ").Append(group.Name).AppendLine("Data();");
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

        private string BuildCallbackParams()
        {
            var parts = new List<string>(TypedGroups.Count + TypedDatas.Count);
            foreach (var group in TypedGroups)
                parts.Add($"Action<{group.Name}Data> callback{group.Name}");
            foreach (var data in TypedDatas)
                parts.Add($"{data.Type}.Callback callback{data.Name}");
            return string.Join(", ", parts);
        }

        private string BuildCallbackArgs()
        {
            var parts = new List<string>(TypedGroups.Count + TypedDatas.Count);
            foreach (var group in TypedGroups)
                parts.Add($"callback{group.Name}");
            foreach (var data in TypedDatas)
                parts.Add($"callback{data.Name}");
            return string.Join(", ", parts);
        }
    }
}
