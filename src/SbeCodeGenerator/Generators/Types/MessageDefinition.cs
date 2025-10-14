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
            sb.AppendUsings(tabs, $"{Namespace}.Runtime");
            
            sb.AppendStructDefinition(tabs, Description, Name, nameof(MessageDefinition), Namespace);

            sb.AppendLine("{", tabs++);
            AppendMessageDefinitionConstants(sb, tabs);
            AppendConstantsFileContent(sb, tabs);
            AppendFieldsFileContent(sb, tabs);
            AppendParseHelpers(sb, tabs);
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
            
            // New TryParse with blockLength parameter for schema evolution support
            sb.AppendLine($"public static bool TryParse(ReadOnlySpan<byte> buffer, int blockLength, out {Name}Data message, out ReadOnlySpan<byte> variableData)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("var reader = new SpanReader(buffer);", tabs);
            sb.AppendLine("", tabs);
            sb.AppendLine("// Validate buffer has enough bytes for the specified block length", tabs);
            sb.AppendLine("if (!reader.CanRead(blockLength))", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("message = default;", tabs);
            sb.AppendLine("variableData = default;", tabs);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("", tabs);
            sb.AppendLine("// Read the message data (MESSAGE_SIZE bytes)", tabs);
            sb.AppendLine("// For schema evolution: the message struct is always MESSAGE_SIZE,", tabs);
            sb.AppendLine("// but variable data starts at blockLength to skip/include version-specific fields", tabs);
            sb.AppendLine($"if (!reader.TryRead<{Name}Data>(out message))", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("message = default;", tabs);
            sb.AppendLine("variableData = default;", tabs);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("", tabs);
            sb.AppendLine("// Calculate variable data position based on blockLength for schema evolution", tabs);
            sb.AppendLine("// If blockLength > MESSAGE_SIZE: skip additional bytes (newer schema)", tabs);
            sb.AppendLine("// If blockLength < MESSAGE_SIZE: variable data overlaps with message (older schema)", tabs);
            sb.AppendLine("var variableDataOffset = blockLength;", tabs);
            sb.AppendLine("var bytesConsumed = MESSAGE_SIZE;", tabs);
            sb.AppendLine("if (variableDataOffset > bytesConsumed)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("// Skip additional bytes for newer schema versions", tabs);
            sb.AppendLine("if (!reader.TrySkip(variableDataOffset - bytesConsumed))", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("variableData = default;", tabs);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("variableData = reader.Remaining;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("else", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("// Variable data starts at blockLength (may overlap with message for older schemas)", tabs);
            sb.AppendLine("variableData = buffer.Slice(variableDataOffset);", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("", tabs);
            sb.AppendLine("return true;", tabs);
            sb.AppendLine("}", --tabs);
        }

        private void AppendConsumeVariable(StringBuilder sb, int tabs)
        {
            if (Groups.Any() || Datas.Any())
            {
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