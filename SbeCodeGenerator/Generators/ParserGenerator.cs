using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator.Generators
{
    internal record ParserGenerator(string Namespace, string Description, List<MessageDefinition> MessageTypes) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendUsings(tabs,
                "System",
                "System.Runtime.InteropServices",
                "System.Runtime.CompilerServices");
            sb.AppendLine($"namespace {Namespace};", tabs);
            sb.AppendSummary(Description, tabs, nameof(ParserGenerator));
            sb.AppendLine($"public abstract class BaseParser", tabs);
            sb.AppendLine("{", tabs);
            sb.AppendLine("public void Parse(ReadOnlySpan<byte> data)", tabs + 1);
            sb.AppendLine("{", tabs + 1);
            sb.AppendLine("ref readonly PacketHeader packet = ref MemoryMarshal.AsRef<PacketHeader>(data);", tabs + 2);
            sb.AppendLine("data = data.Slice(Unsafe.SizeOf<PacketHeader>());", tabs + 2);
            sb.AppendLine("do", tabs + 2);
            sb.AppendLine("{", tabs + 2);
            sb.AppendLine("ref readonly FramingHeader framingHeader = ref MemoryMarshal.AsRef<FramingHeader>(data);", tabs + 3);
            sb.AppendLine("data = data.Slice(FramingHeader.MESSAGE_SIZE);", tabs + 3);
            sb.AppendLine("ref readonly MessageHeader messageHeader = ref MemoryMarshal.AsRef<MessageHeader>(data);", tabs + 3);
            sb.AppendLine("data = data.Slice(MessageHeader.MESSAGE_SIZE);", tabs + 3);
            sb.AppendLine("var length = framingHeader.MessageLength - (FramingHeader.MESSAGE_SIZE + MessageHeader.MESSAGE_SIZE);", tabs + 3);
            sb.AppendLine("var body = data.Slice(0, length);", tabs + 3);
            sb.AppendLine("data = data.Slice(length);", tabs + 3);
            sb.AppendLine("switch (messageHeader.TemplateId)", tabs + 3);
            sb.AppendLine("{", tabs + 3);
            foreach (var type in MessageTypes)
            {
                sb.AppendLine($"case {type.Name}Data.MESSAGE_ID:", tabs + 4);
                sb.AppendLine($"Callback(", tabs + 5);
                if (type.Fields.Count == 0)
                {
                    sb.AppendLine($"new {type.Name}Data(),", tabs + 6);
                    sb.AppendLine($"Array.Empty<byte>()", tabs + 6);
                }
                else
                {
                    sb.AppendLine($"in MemoryMarshal.AsRef<{type.Name}Data>(body),", tabs + 6);
                    sb.AppendLine($"body.Slice({type.Name}Data.MESSAGE_SIZE)", tabs + 6);
                }
                sb.AppendLine($");", tabs + 5);
                sb.AppendLine("break;", tabs + 5);
            }
            sb.AppendLine("default:", tabs + 4);
            sb.AppendLine("Console.WriteLine(messageHeader.TemplateId);", tabs + 5);
            sb.AppendLine("break;", tabs + 5);
            sb.AppendLine("}", tabs + 3);
            sb.AppendLine("}", tabs + 2);
            sb.AppendLine("while (data.Length != 0);", tabs + 2);
            sb.AppendLine("}", tabs + 1);
            foreach (var type in MessageTypes)
            {
                sb.AppendLine($"public virtual void Callback(ref readonly {type.Name}Data message, ReadOnlySpan<byte> variablePart) {{ }}", tabs + 1);
            }
            sb.AppendLine("}", tabs);
        }
    }
}
