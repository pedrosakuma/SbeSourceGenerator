using SbeSourceGenerator.Generators.Fields;
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

            sb.AppendLine("{", tabs);
            AppendMessageDefinitionConstants(sb, tabs + 1);
            AppendConstantsFileContent(sb, tabs + 1);
            AppendFieldsFileContent(sb, tabs + 1);
            AppendGroupsFileContent(sb, tabs + 1);
            AppendConsumeVariable(sb, tabs + 1);
            sb.AppendLine("}", tabs);
        }

        private void AppendConsumeVariable(StringBuilder sb, int tabs)
        {
            if (Groups.Any() || Datas.Any())
            {
                sb.AppendLine("public void ConsumeVariableLengthSegments(ReadOnlySpan<byte> buffer, ", tabs);
                foreach (var group in Groups.Cast<GroupDefinition>())
                {
                    sb.AppendLine($"Action<{group.Name}Data> callback{group.Name}, ", tabs + 1);
                }
                foreach (var data in Datas.Cast<DataFieldDefinition>())
                {
                    sb.AppendLine($"{data.Type}.Callback callback{data.Name}, ", tabs + 1);
                }
                sb.Remove(sb.Length - 4, 4);
                sb.AppendLine(")");
                sb.AppendLine("{", tabs);
                sb.AppendLine("int offset = 0;", tabs + 1);
                foreach (var group in Groups.Cast<GroupDefinition>())
                {
                    sb.AppendLine($"ref readonly GroupSizeEncoding group{group.Name} = ref MemoryMarshal.AsRef<GroupSizeEncoding>(buffer.Slice(offset));", tabs + 1);
                    sb.AppendLine("offset += GroupSizeEncoding.MESSAGE_SIZE;", tabs + 1);
                    sb.AppendLine($"for (int i = 0; i < group{group.Name}.NumInGroup; i++)", tabs + 1);
                    sb.AppendLine("{", tabs + 1);
                    sb.AppendLine($"ref readonly var data = ref MemoryMarshal.AsRef<{group.Name}Data>(buffer.Slice(offset));", tabs +2);
                    sb.AppendLine($"callback{group.Name}(data);", tabs + 2);
                    sb.AppendLine($"offset += {group.Name}Data.MESSAGE_SIZE;", tabs + 2);
                    sb.AppendLine("}", tabs + 1);
                }
                foreach (var data in Datas.Cast<DataFieldDefinition>())
                {
                    sb.AppendLine($"var datas{data.Name} = {data.Type}.Create(buffer.Slice(offset));", tabs + 1);
                    sb.AppendLine($"callback{data.Name}(datas{data.Name});", tabs + 1);
                }
                sb.AppendLine("}", tabs);
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