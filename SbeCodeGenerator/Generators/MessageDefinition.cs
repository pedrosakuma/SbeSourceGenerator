using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    public record MessageDefinition(string Namespace, string Name, string Id, string Description, string SemanticType, string Deprecated,
        List<IFileContentGenerator> Fields, List<IFileContentGenerator> Constants, List<IFileContentGenerator> Groups, List<IFileContentGenerator> Datas) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine($$"""
                using System.Runtime.InteropServices;
                namespace {{Namespace}};
                
                {{SummaryGenerator.Generate(Description, nameof(MessageDefinition))}}
                [StructLayout(LayoutKind.Explicit)]
                public partial struct {{Name}}Data
                {
                """);
            AppendConstantsFileContent(sb);
            AppendFieldsFileContent(sb);
            sb.AppendLine("}");
            return sb.ToString();
        }

        private void AppendConstantsFileContent(StringBuilder sb)
        {
            foreach (var field in Constants)
                sb.AppendLine(field.GenerateFileContent());
        }
        private void AppendFieldsFileContent(StringBuilder sb)
        {
            int offset = 0;
            foreach (var field in Fields)
            {
                var blittableField = (IBlittableMessageField)field;
                blittableField.Offset ??= offset;
                sb.AppendLine(field.GenerateFileContent());
                offset += blittableField.Length;
            }
        }
    }
}