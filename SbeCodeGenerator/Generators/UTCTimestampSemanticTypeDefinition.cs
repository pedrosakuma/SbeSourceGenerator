using System.Collections.Generic;
using System.Linq;

namespace SbeSourceGenerator
{
    internal record UTCTimestampSemanticTypeDefinition(string Namespace, string Name, List<IFileContentGenerator> Fields) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            var hasNullable = Fields.Where(f => f is NullableValueFieldDefinition).Any();
            return $$"""
                namespace {{Namespace}};
                public partial struct {{Name}}
                {
                {{SummaryGenerator.Generate("Date value from offset and unit", 1, nameof(UTCTimestampSemanticTypeDefinition))}}
                    public DateTime{{(hasNullable ? "?" : "")}} Value => Time.ToDateTimeWithUnit(Unit);
                }
                """;
        }
    }
}