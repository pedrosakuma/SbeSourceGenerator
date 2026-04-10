using System.Collections.Generic;
using System.Text;
using SbeSourceGenerator.Generators.Fields;

namespace SbeSourceGenerator
{
    public record GroupDefinition(string Namespace, string Name, string Id, string DimensionType, string Description,
        List<IFileContentGenerator> Fields, List<IFileContentGenerator> Constants,
        string NumInGroupType = "ushort", List<IFileContentGenerator>? Datas = null,
        List<IFileContentGenerator>? NestedGroups = null,
        EndianConversion EndianConversion = EndianConversion.None) : IFileContentGenerator
    {
        private List<DataFieldDefinition>? _typedDatas;
        private List<GroupDefinition>? _typedNestedGroups;

        public List<DataFieldDefinition> TypedDatas
        {
            get
            {
                if (_typedDatas == null)
                {
                    _typedDatas = new List<DataFieldDefinition>();
                    if (Datas != null)
                    {
                        foreach (var item in Datas)
                            _typedDatas.Add((DataFieldDefinition)item);
                    }
                }
                return _typedDatas;
            }
        }

        public List<GroupDefinition> TypedNestedGroups
        {
            get
            {
                if (_typedNestedGroups == null)
                {
                    _typedNestedGroups = new List<GroupDefinition>();
                    if (NestedGroups != null)
                    {
                        foreach (var item in NestedGroups)
                            _typedNestedGroups.Add((GroupDefinition)item);
                    }
                }
                return _typedNestedGroups;
            }
        }

        public bool HasGroupData => Datas != null && Datas.Count > 0;
        public bool HasNestedGroups => NestedGroups != null && NestedGroups.Count > 0;
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendStructDefinition(tabs, Description, Name, nameof(GroupDefinition));
            if (EndianConversion != EndianConversion.None)
                sb.AppendUsings(tabs, "System.Buffers.Binary");
            sb.AppendLine("{", tabs++);

            AppendMessageDefinitionConstants(sb, tabs);
            AppendConstantsFileContent(sb, tabs);
            AppendFieldsFileContent(sb, tabs);

            sb.AppendLine("}", --tabs);
        }
        private void AppendMessageDefinitionConstants(StringBuilder sb, int tabs)
        {
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
                if (field is IBlittableMessageField blittableField)
                {
                    blittableField.Offset ??= offset;
                    field.AppendFileContent(sb, tabs);
                    offset = blittableField.Offset.Value + blittableField.Length;
                }
                else
                {
                    field.AppendFileContent(sb, tabs);
                }
            }
        }
    }


}
