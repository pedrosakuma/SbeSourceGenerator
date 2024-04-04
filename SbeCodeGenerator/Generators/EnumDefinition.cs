using System;
using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{

    public record EnumFlagsDefinition(string Namespace, string Name, string Description, string EncodingType, List<EnumFieldDefinition> Fields) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine($$"""
                namespace {{Namespace}};
                /// <summary>
                /// {{Description}}
                /// </summary>
                [System.Flags]
                public enum {{Name}} : {{EncodingType}}
                {
                """);
            foreach (var field in Fields)
            {
                if (field.Description != "")
                    sb.AppendLine($$"""
                            /// <summary>
                            /// {{field.Description}}
                            /// </summary>
                        """);
                sb.AppendLine($"\t{field.Name} = {1 << int.Parse(field.Value)},");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
    public record EnumDefinition(string Namespace, string Name, string Description, string EncodingType, string SemanticType, List<EnumFieldDefinition> Fields) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine($$"""
                namespace {{Namespace}};
                /// <summary>
                /// {{Description}}
                /// </summary>
                public enum {{Name}} : {{ChangeTypeIfNeeded(EncodingType)}}
                {
                """);
            foreach (var field in Fields)
            {
                if (field.Description != "")
                    sb.AppendLine($$"""
                            /// <summary>
                            /// {{field.Description}}
                            /// </summary>
                        """);
                sb.AppendLine($"\t{field.Name} = {IncludeQuotationAndCastIfNeeded(field.Value, EncodingType)},");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string IncludeQuotationAndCastIfNeeded(string value, string encodingType)
        {
            return encodingType switch
            {
                "char" => $"(short)'{value}'",
                _ => value
            };
        }

        private string ChangeTypeIfNeeded(string encodingType)
        {
            return encodingType switch
            {
                "char" => "short",
                _ => encodingType
            };
        }
    }
}