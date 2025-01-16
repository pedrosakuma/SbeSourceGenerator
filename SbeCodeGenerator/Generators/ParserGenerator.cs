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
            sb.AppendLine($"public class MessageParser", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("public static int[] MessageIds = new int[]", tabs);
            sb.AppendLine("{", tabs++);
            foreach (var type in MessageTypes)
                sb.AppendLine($"{type.Name}Data.MESSAGE_ID,", tabs);
            sb.AppendLine("};", --tabs);
            sb.AppendLine("private readonly ShouldConsumePredicate shouldConsume;", tabs);
            sb.AppendLine("public MessageParser(ShouldConsumePredicate shouldConsume)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("this.shouldConsume = shouldConsume;", tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("public void Parse(ReadOnlySpan<byte> data)", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("if(data.Length == 0) { return; }", tabs);
            sb.AppendLine("ref readonly PacketHeader packet = ref MemoryMarshal.AsRef<PacketHeader>(data);", tabs);
            sb.AppendLine("if(!shouldConsume(in packet, data))", tabs++);
            sb.AppendLine("return;", tabs--);
            sb.AppendLine("data = data.Slice(Unsafe.SizeOf<PacketHeader>());", tabs);
            sb.AppendLine("do", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendLine("ref readonly FramingHeader framingHeader = ref MemoryMarshal.AsRef<FramingHeader>(data);", tabs);
            sb.AppendLine("data = data.Slice(FramingHeader.MESSAGE_SIZE);", tabs);
            sb.AppendLine("ref readonly MessageHeader messageHeader = ref MemoryMarshal.AsRef<MessageHeader>(data);", tabs);
            sb.AppendLine("data = data.Slice(MessageHeader.MESSAGE_SIZE);", tabs);
            sb.AppendLine("var length = framingHeader.MessageLength - (FramingHeader.MESSAGE_SIZE + MessageHeader.MESSAGE_SIZE);", tabs);
            sb.AppendLine("var body = data.Slice(0, length);", tabs);
            sb.AppendLine("data = data.Slice(length);", tabs);
            sb.AppendLine("switch (messageHeader.TemplateId)", tabs);
            sb.AppendLine("{", tabs++);
            foreach (var type in MessageTypes)
            {
                sb.AppendLine($"case {type.Name}Data.MESSAGE_ID:", tabs++);
                sb.AppendLine($"{type.Name}MessageReceived?.Invoke(", tabs++);
                if (type.Fields.Count == 0)
                {
                    sb.AppendLine($"new {type.Name}Data(),", tabs);
                    sb.AppendLine($"Array.Empty<byte>()", tabs);
                }
                else
                {
                    sb.AppendLine($"in MemoryMarshal.AsRef<{type.Name}Data>(body),", tabs);
                    sb.AppendLine($"body.Slice({type.Name}Data.MESSAGE_SIZE)", tabs);
                }
                sb.AppendLine($");", --tabs);
                sb.AppendLine("break;", tabs--);
            }
            sb.AppendLine("default:", tabs++);
            sb.AppendLine("Console.WriteLine(messageHeader.TemplateId);", tabs);
            sb.AppendLine("break;", tabs--);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("}", --tabs);
            sb.AppendLine("while (data.Length != 0);", tabs);
            sb.AppendLine("}", --tabs);

            sb.AppendLine($"public delegate bool ShouldConsumePredicate(ref readonly PacketHeader packet, ReadOnlySpan<byte> data);", tabs);
            foreach (var type in MessageTypes)
            {
                sb.AppendLine($"public delegate void {type.Name}Message(ref readonly {type.Name}Data message, ReadOnlySpan<byte> variablePart);", tabs);
                sb.AppendLine($"public {type.Name}Message? {type.Name}MessageReceived {{ get; init; }}", tabs);
            }
            sb.AppendLine("}", --tabs);
        }
    }
}
