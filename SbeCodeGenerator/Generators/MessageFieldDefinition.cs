namespace SbeSourceGenerator
{
    public class MessageFieldDefinition : IFileContentGenerator, IBlittableMessageField
    {
        public string Name { get; }
        public string Id { get; }
        public string Type { get; }
        public string Description { get; }
        public int? Offset { get; set; }
        public int Length { get; }

        public MessageFieldDefinition(string Name, string Id, string Type, string Description,
            int? Offset, int Length)
        {
            this.Name = Name;
            this.Id = Id;
            this.Type = Type;
            this.Description = Description;
            this.Offset = Offset;
            this.Length = Length;
        }
        public string GenerateFileContent()
        {
            return $$"""
                    [FieldOffset({{Offset}})]
                {{SummaryGenerator.Generate(Description, 1, nameof(MessageFieldDefinition))}}
                    public {{Type}} {{Name}};
                """;
        }
    }
}