using System;
using System.Text;

namespace SbeSourceGenerator
{
    public record ConstantTypeFieldDefinition(string Name, string Description, string PrimitiveType, string Value, string ValueRef) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"\t/// <summary>");
            sb.AppendLine($"\t/// {Description}");
            sb.AppendLine($"\t/// </summary>");
            if (Value == "")
                sb.AppendLine($"\tpublic const {PrimitiveType} {Name} = ({PrimitiveType}){ValueRef};");
            else
                sb.AppendLine($"\tpublic const {PrimitiveType} {Name} = {Value};");
            return sb.ToString();
        }
    }

    public record ArrayFieldDefinition(string Name, string Description, string PrimitiveType) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            return $$"""
                    /// <summary>
                    /// {{Description}}
                    /// </summary>
                    public {{PrimitiveType}}[] {{Name}};
                """;
        }
    }
    public record ValueFieldDefinition(string Name, string Description, string PrimitiveType) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            return $$"""
                    /// <summary>
                    /// {{Description}}
                    /// </summary>
                    public {{PrimitiveType}} {{Name}};
                """;
        }
    }

    public record NullableValueFieldDefinition(string Name, string Description, string PrimitiveType) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            return $$"""
                    /// <summary>
                    /// {{Description}}
                    /// </summary>
                    private {{PrimitiveType}} {{Name.FirstCharToLower()}};
                    public {{PrimitiveType}}? {{Name}} => {{Name.FirstCharToLower()}} == {{PrimitiveTypes.NullValueByType[PrimitiveType]}} ? null : {{Name.FirstCharToLower()}};
                """;
        }
    }
}
