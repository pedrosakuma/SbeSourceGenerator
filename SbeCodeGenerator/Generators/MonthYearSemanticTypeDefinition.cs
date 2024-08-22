using System.Collections.Generic;
using System.Linq;

namespace SbeSourceGenerator
{
    internal record MonthYearSemanticTypeDefinition(string Namespace, string Name, List<IFileContentGenerator> Fields) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            var hasNullable = Fields.Where(f => f is NullableValueFieldDefinition).Any();
            if (hasNullable)
            {
                return $$"""
                    namespace {{Namespace}};
                    public partial struct {{Name}}
                    {
                        {{SummaryGenerator.Generate("Date value from offset and unit", 1, nameof(MonthYearSemanticTypeDefinition))}}
                        public DateTime? Value => (Year.HasValue && Month.HasValue && Day.HasValue) ? new DateTime(Year.Value, Month.Value, Day.Value) : null;
                    }
                    """;
            }
            else
            {
                return $$"""
                    namespace {{Namespace}};
                    public partial struct {{Name}}
                    {
                        {{SummaryGenerator.Generate("Date value from offset and unit", 1, nameof(MonthYearSemanticTypeDefinition))}}
                        public DateTime Value => new DateTime(Year, Month, Day);
                    }
                    """;
            }
        }
    }
}