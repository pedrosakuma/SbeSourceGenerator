using SbeSourceGenerator.Generators.Fields;
using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    public record MessageDefinition(string Namespace, string RuntimeNamespace, string Name, string Id, string Description, string SemanticType, string Deprecated,
        List<IFileContentGenerator> Fields, List<IFileContentGenerator> Constants,
        List<IFileContentGenerator> Groups, List<IFileContentGenerator> Datas, string SchemaBlockLength = "",
        EndianConversion EndianConversion = EndianConversion.None, string SchemaId = "", string SchemaVersion = "0",
        string HeaderTypeName = "MessageHeader") : IFileContentGenerator
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
            if (EndianConversion != EndianConversion.None)
                sb.AppendUsings(tabs, RuntimeNamespace, $"{RuntimeNamespace}.Runtime", "System.Runtime.CompilerServices", "System.Buffers.Binary");
            else
                sb.AppendUsings(tabs, RuntimeNamespace, $"{RuntimeNamespace}.Runtime", "System.Runtime.CompilerServices");

            sb.AppendStructDefinition(tabs, Description, Name, nameof(MessageDefinition), Namespace);

            sb.AppendLine("{", tabs++);
            AppendMessageDefinitionConstants(sb, tabs);
            AppendConstantsFileContent(sb, tabs);
            AppendFieldsFileContent(sb, tabs);
            AppendParseHelpers(sb, tabs);
            AppendEncodeHelpers(sb, tabs);
            AppendGroupsFileContent(sb, tabs);
            AppendDelegateTypes(sb, tabs);
            
            // Generate comprehensive TryEncode method if message has groups or varData
            if (HasVariableData)
            {
                AppendComprehensiveTryEncode(sb, tabs);
            }

            FileContentGeneratorExtensions.AppendToString(sb, tabs, Name + "Data", Fields);
            AppendWriteHeader(sb, tabs);
            
            sb.AppendLine("}", --tabs);

            AppendReaderStruct(sb, tabs);
        }

        private void AppendParseHelpers(StringBuilder sb, int tabs)
        {
            // TryParse without blockLength parameter
            sb.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]", tabs);
            sb.AppendTabs(tabs).Append("public static bool TryParse(ReadOnlySpan<byte> buffer, out ").Append(Name).AppendLine("DataReader reader)");
            sb.AppendLine("{", tabs++);
            sb.AppendTabs(tabs).AppendLine("return TryParse(buffer, BLOCK_LENGTH, out reader);");
            sb.AppendLine("}", --tabs);

            // TryParse with blockLength parameter for schema evolution support
            sb.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]", tabs);
            sb.AppendTabs(tabs).Append("public static bool TryParse(ReadOnlySpan<byte> buffer, int blockLength, out ").Append(Name).AppendLine("DataReader reader)");
            sb.AppendLine("{", tabs++);
            sb.AppendTabs(tabs).AppendLine("if (buffer.Length < blockLength)");
            sb.AppendLine("{", tabs++);
            sb.AppendLine("reader = default;", tabs);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendTabs(tabs).Append("reader = new ").Append(Name).AppendLine("DataReader(buffer, blockLength);");
            sb.AppendLine("return true;", tabs);
            sb.AppendLine("}", --tabs);

            // TryParseWithReader for sequential reading from a SpanReader
            sb.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]", tabs);
            sb.AppendTabs(tabs).Append("public static bool TryParseWithReader(ref SpanReader reader, int blockLength, out ").Append(Name).AppendLine("DataReader messageReader)");
            sb.AppendLine("{", tabs++);
            sb.AppendTabs(tabs).AppendLine("var remaining = reader.Remaining;");
            sb.AppendTabs(tabs).AppendLine("if (remaining.Length < blockLength)");
            sb.AppendLine("{", tabs++);
            sb.AppendLine("messageReader = default;", tabs);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendTabs(tabs).Append("messageReader = new ").Append(Name).AppendLine("DataReader(remaining, blockLength);");
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
                sb.AppendTabs(tabs).Append("// Write length prefix (").Append(data.LengthPrefixType).AppendLine(")");
                sb.AppendTabs(tabs).Append("if (data.Length > ").Append(data.MaxLength).AppendLine(")");
                sb.AppendLine("    return false;", tabs + 1);
                sb.AppendLine("", tabs);
                sb.AppendTabs(tabs).Append("if (!writer.TryWrite((").Append(data.LengthPrefixType).AppendLine(")data.Length))");
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

        private void AppendDelegateTypes(StringBuilder sb, int tabs)
        {
            if (HasVariableData)
            {
                foreach (var group in TypedGroups)
                    AppendGroupDelegateTypes(sb, tabs, group);
            }
        }

        private static void AppendGroupConsume(StringBuilder sb, int tabs, GroupDefinition group, List<string>? ancestorVarNames = null, string dataTypePrefix = "")
        {
            var ancestors = ancestorVarNames ?? new List<string>();
            var depth = ancestors.Count;
            var dataVarName = depth == 0 ? "data" : $"nestedData{depth}";
            var loopVar = depth == 0 ? "i" : $"j{depth}";

            sb.AppendTabs(tabs).Append("if (reader.TryRead<").Append(group.DimensionType).Append(">(out var group").Append(group.Name).AppendLine("))");
            sb.AppendLine("{", tabs++);
            sb.AppendTabs(tabs).Append("for (int ").Append(loopVar).Append(" = 0; ").Append(loopVar).Append(" < group").Append(group.Name).Append(".NumInGroup; ").Append(loopVar).AppendLine("++)");
            sb.AppendLine("{", tabs++);
            sb.AppendTabs(tabs).Append("if (!reader.TryReadBlock<").Append(dataTypePrefix).Append(group.Name).Append("Data>(group").Append(group.Name).Append(".BlockLength, out var ").Append(dataVarName).AppendLine("))");
            sb.AppendLine("{", tabs++);
            sb.AppendLine("break;", tabs);
            sb.AppendLine("}", --tabs);

            // Invoke callback with ancestor context using 'in' for zero-copy pass
            sb.AppendTabs(tabs).Append("callback").Append(group.Name).Append("(");
            foreach (var ancestorVar in ancestors)
            {
                sb.Append("in ").Append(ancestorVar).Append(", ");
            }
            sb.Append("in ").Append(dataVarName).AppendLine(");");

            // Read group-level variable data after each entry
            foreach (var groupData in group.TypedDatas)
            {
                sb.AppendTabs(tabs).Append("var datas").Append(groupData.Name).Append(" = ").Append(groupData.Type).AppendLine(".Create(reader.Remaining);");
                sb.AppendTabs(tabs).Append("callback").Append(group.Name).Append(groupData.Name).Append("(datas").Append(groupData.Name).AppendLine(");");
                sb.AppendTabs(tabs).Append("reader.TrySkip(datas").Append(groupData.Name).AppendLine(".TotalLength);");
            }
            // Read nested groups recursively
            var ancestorsForChildren = new List<string>(ancestors);
            ancestorsForChildren.Add(dataVarName);
            foreach (var nestedGroup in group.TypedNestedGroups)
            {
                AppendGroupConsume(sb, tabs, nestedGroup, ancestorsForChildren, dataTypePrefix);
            }
            sb.AppendLine("}", --tabs);
            sb.AppendLine("}", --tabs);
        }

        private static void AppendGroupDelegateTypes(StringBuilder sb, int tabs, GroupDefinition group, List<string>? ancestorTypes = null)
        {
            var ancestors = ancestorTypes ?? new List<string>();
            var paramParts = new List<string>();
            foreach (var ancestor in ancestors)
                paramParts.Add($"in {ancestor} {char.ToLowerInvariant(ancestor[0])}{ancestor.Substring(1)}");
            paramParts.Add($"in {group.Name}Data data");
            sb.AppendTabs(tabs).Append("public delegate void ").Append(group.Name).Append("Handler(")
                .Append(string.Join(", ", paramParts)).AppendLine(");");

            var ancestorsForChildren = new List<string>(ancestors);
            ancestorsForChildren.Add($"{group.Name}Data");
            foreach (var nestedGroup in group.TypedNestedGroups)
                AppendGroupDelegateTypes(sb, tabs, nestedGroup, ancestorsForChildren);
        }

        private void AppendGroupsFileContent(StringBuilder sb, int tabs)
        {
            foreach (var group in Groups)
            {
                group.AppendFileContent(sb, tabs);
                var typedGroup = (GroupDefinition)group;
                AppendNestedGroupStructs(sb, tabs, typedGroup);
            }
        }

        private static void AppendNestedGroupStructs(StringBuilder sb, int tabs, GroupDefinition group)
        {
            if (!group.HasNestedGroups)
                return;
            foreach (var nested in group.NestedGroups!)
            {
                nested.AppendFileContent(sb, tabs);
                AppendNestedGroupStructs(sb, tabs, (GroupDefinition)nested);
            }
        }

        private void AppendMessageDefinitionConstants(StringBuilder sb, int tabs)
        {
            new ConstantMessageFieldDefinition("MessageId", "Id", "int", "Message Id",
                Id.ToString()).AppendFileContent(sb, tabs);
            new ConstantMessageFieldDefinition("MessageSize", "Size", "int", "Message Size",
                Fields.SumFieldLength().ToString()).AppendFileContent(sb, tabs);

            string blockLengthValue = !string.IsNullOrEmpty(SchemaBlockLength)
                ? SchemaBlockLength
                : "MESSAGE_SIZE";
            new ConstantMessageFieldDefinition("BlockLength", "BlockLength", "int",
                "Block length for wire protocol",
                blockLengthValue).AppendFileContent(sb, tabs);
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

        private string BuildCallbackParams(string delegateTypePrefix = "")
        {
            var parts = new List<string>(TypedGroups.Count + TypedDatas.Count);
            foreach (var group in TypedGroups)
                AppendGroupCallbackParams(parts, group, delegateTypePrefix: delegateTypePrefix);
            foreach (var data in TypedDatas)
                parts.Add($"{data.Type}.Callback callback{data.Name}");
            return string.Join(", ", parts);
        }

        private static void AppendGroupCallbackParams(List<string> parts, GroupDefinition group, List<string>? ancestorTypes = null, string delegateTypePrefix = "")
        {
            var ancestors = ancestorTypes ?? new List<string>();
            parts.Add($"{delegateTypePrefix}{group.Name}Handler callback{group.Name}");

            foreach (var groupData in group.TypedDatas)
                parts.Add($"{groupData.Type}.Callback callback{group.Name}{groupData.Name}");

            var ancestorsForChildren = new List<string>(ancestors);
            ancestorsForChildren.Add($"{group.Name}Data");
            foreach (var nestedGroup in group.TypedNestedGroups)
                AppendGroupCallbackParams(parts, nestedGroup, ancestorsForChildren, delegateTypePrefix);
        }

        private string BuildCallbackArgs()
        {
            var parts = new List<string>(TypedGroups.Count + TypedDatas.Count);
            foreach (var group in TypedGroups)
                AppendGroupCallbackArgs(parts, group);
            foreach (var data in TypedDatas)
                parts.Add($"callback{data.Name}");
            return string.Join(", ", parts);
        }

        private static void AppendGroupCallbackArgs(List<string> parts, GroupDefinition group)
        {
            parts.Add($"callback{group.Name}");
            foreach (var groupData in group.TypedDatas)
                parts.Add($"callback{group.Name}{groupData.Name}");
            foreach (var nestedGroup in group.TypedNestedGroups)
                AppendGroupCallbackArgs(parts, nestedGroup);
        }

        private void AppendReaderStruct(StringBuilder sb, int tabs)
        {
            sb.AppendLine("", tabs);
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendTabs(tabs).Append("/// Zero-copy reader for ").Append(Name).AppendLine("Data messages.");
            sb.AppendLine("/// Provides direct access to the message data in the underlying buffer without copying.", tabs);
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendTabs(tabs).Append("public ref struct ").Append(Name).AppendLine("DataReader");
            sb.AppendLine("{", tabs++);

            sb.AppendLine("private readonly ReadOnlySpan<byte> _buffer;", tabs);
            sb.AppendLine("private readonly int _blockLength;", tabs);
            sb.AppendLine("", tabs);

            // BytesConsumed property
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendLine("/// Total bytes consumed from the buffer (block + variable-length data).", tabs);
            sb.AppendLine("/// Updated after ReadGroups is called for messages with groups or varData.", tabs);
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendLine("public int BytesConsumed { get; private set; }", tabs);
            sb.AppendLine("", tabs);

            // Constructor
            sb.AppendLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]", tabs);
            sb.AppendTabs(tabs).Append("internal ").Append(Name).AppendLine("DataReader(ReadOnlySpan<byte> buffer, int blockLength)");
            sb.AppendLine("{", tabs++);
            sb.AppendLine("_buffer = buffer;", tabs);
            sb.AppendLine("_blockLength = blockLength;", tabs);
            sb.AppendLine("BytesConsumed = blockLength;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("", tabs);

            // Data property — zero-copy ref into buffer
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendTabs(tabs).Append("/// Gets a readonly reference to the ").Append(Name).AppendLine("Data directly in the buffer (zero-copy).");
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendTabs(tabs).Append("public ref readonly ").Append(Name).Append("Data Data => ref Unsafe.As<byte, ").Append(Name).AppendLine("Data>(ref MemoryMarshal.GetReference(_buffer));");

            // ReadGroups — only for messages with variable data
            if (HasVariableData)
            {
                AppendReadGroups(sb, tabs);
            }

            sb.AppendLine("}", --tabs);
        }

        private void AppendReadGroups(StringBuilder sb, int tabs)
        {
            var callbackParams = BuildCallbackParams($"{Name}Data.");
            sb.AppendLine("", tabs);
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendLine("/// Reads groups and variable-length data from the message buffer using callbacks.", tabs);
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendTabs(tabs).Append("public void ReadGroups(").Append(callbackParams).AppendLine(")");
            sb.AppendLine("{", tabs++);
            sb.AppendLine("var reader = new SpanReader(_buffer.Slice(_blockLength));", tabs);

            foreach (var group in TypedGroups)
            {
                AppendGroupConsume(sb, tabs, group, dataTypePrefix: $"{Name}Data.");
            }
            foreach (var data in TypedDatas)
            {
                sb.AppendTabs(tabs).Append("var datas").Append(data.Name).Append(" = ").Append(data.Type).AppendLine(".Create(reader.Remaining);");
                sb.AppendTabs(tabs).Append("callback").Append(data.Name).Append("(datas").Append(data.Name).AppendLine(");");
                sb.AppendTabs(tabs).Append("reader.TrySkip(datas").Append(data.Name).AppendLine(".TotalLength);");
            }

            sb.AppendLine("BytesConsumed = _buffer.Length - reader.Remaining.Length;", tabs);
            sb.AppendLine("}", --tabs);
        }

        private void AppendWriteHeader(StringBuilder sb, int tabs)
        {
            var schemaIdValue = string.IsNullOrEmpty(SchemaId) ? "0" : SchemaId;

            sb.AppendLine("/// <summary>", tabs);
            sb.AppendTabs(tabs).Append("/// Writes a pre-populated ").Append(HeaderTypeName).Append(" for this message type to the destination buffer.");
            sb.AppendLine();
            sb.AppendTabs(tabs).Append("/// Sets BlockLength=").Append("BLOCK_LENGTH").Append(", TemplateId=").Append(Id).Append(", SchemaId=").Append(schemaIdValue).Append(", Version=").Append(SchemaVersion).AppendLine(".");
            sb.AppendLine("/// </summary>", tabs);
            sb.AppendLine("/// <param name=\"buffer\">The destination buffer (must be at least MessageHeader.MESSAGE_SIZE bytes).</param>", tabs);
            sb.AppendTabs(tabs).Append("/// <returns>The number of header bytes written (").Append(HeaderTypeName).AppendLine(".MESSAGE_SIZE).</returns>");
            sb.AppendTabs(tabs).Append("public static int WriteHeader(Span<byte> buffer)").AppendLine();
            sb.AppendLine("{", tabs++);
            sb.AppendTabs(tabs).Append("ref var header = ref MemoryMarshal.AsRef<").Append(HeaderTypeName).AppendLine(">(buffer);");
            sb.AppendLine("header.BlockLength = (ushort)BLOCK_LENGTH;", tabs);
            sb.AppendTabs(tabs).Append("header.TemplateId = ").Append(Id).AppendLine(";");
            sb.AppendTabs(tabs).Append("header.SchemaId = ").Append(schemaIdValue).AppendLine(";");
            sb.AppendTabs(tabs).Append("header.Version = ").Append(SchemaVersion).AppendLine(";");
            sb.AppendTabs(tabs).Append("return ").Append(HeaderTypeName).AppendLine(".MESSAGE_SIZE;");
            sb.AppendLine("}", --tabs);
        }
    }
}
