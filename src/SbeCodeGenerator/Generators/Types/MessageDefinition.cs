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
            sb.AppendLine($"public static bool TryParse(ReadOnlySpan<byte> buffer, out {Name}Data message, out ReadOnlySpan<byte> variableData)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("if (buffer.Length < MESSAGE_SIZE)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("message = default;", tabs);
            sb.AppendLine("variableData = default;", tabs);
            sb.AppendLine("return false;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine($"message = MemoryMarshal.Read<{Name}Data>(buffer);", tabs);
            sb.AppendLine("variableData = buffer.Slice(MESSAGE_SIZE);", tabs);
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
                sb.AppendLine("int offset = 0;", tabs);
                foreach (var group in Groups.Cast<GroupDefinition>())
                {
                    sb.AppendLine($"ref readonly GroupSizeEncoding group{group.Name} = ref MemoryMarshal.AsRef<GroupSizeEncoding>(buffer.Slice(offset));", tabs);
                    sb.AppendLine("offset += GroupSizeEncoding.MESSAGE_SIZE;", tabs);
                    sb.AppendLine($"for (int i = 0; i < group{group.Name}.NumInGroup; i++)", tabs);
                    sb.AppendLine("{", tabs++);
                    sb.AppendLine($"ref readonly var data = ref MemoryMarshal.AsRef<{group.Name}Data>(buffer.Slice(offset));", tabs);
                    sb.AppendLine($"callback{group.Name}(data);", tabs);
                    sb.AppendLine($"offset += {group.Name}Data.MESSAGE_SIZE;", tabs);
                    sb.AppendLine("}", --tabs);
                }
                foreach (var data in Datas.Cast<DataFieldDefinition>())
                {
                    sb.AppendLine($"var datas{data.Name} = {data.Type}.Create(buffer.Slice(offset));", tabs);
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