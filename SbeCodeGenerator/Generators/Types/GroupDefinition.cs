using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SbeSourceGenerator
{
    public record GroupDefinition(string Namespace, string Name, string Id, string DimensionType, string Description,
        List<IFileContentGenerator> Fields, List<IFileContentGenerator> Constants) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendStructDefinition(tabs, Description, Name, nameof(GroupDefinition));
            sb.AppendLine("{", tabs);

            AppendMessageDefinitionConstants(sb, tabs + 1);
            AppendConstantsFileContent(sb, tabs + 1);
            AppendFieldsFileContent(sb, tabs + 1);

            sb.AppendLine("}", tabs);
        }
        private void AppendMessageDefinitionConstants(StringBuilder sb, int tabs)
        {
            new ConstantMessageFieldDefinition("MessageSize", "Size", "int", "Message Size",
                Fields.Sum(f => ((IBlittableMessageField)f).Length).ToString()).AppendFileContent(sb, tabs);
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
                offset += blittableField.Length;
            }
        }
    }


}
