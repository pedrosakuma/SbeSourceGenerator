using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

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
                    /// <summary>
                    /// Date value from offset and unit
                    /// </summary>
                    public DateTime{{(hasNullable ? "?" : "")}} Value => Time.ToDateTimeWithUnit(Unit);
                }
                """;
        }
    }
}