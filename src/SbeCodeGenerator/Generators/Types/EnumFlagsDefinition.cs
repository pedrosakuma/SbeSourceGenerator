using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SbeSourceGenerator
{
    public record EnumFlagsDefinition(string Namespace, string Name, string Description, string EncodingType, int Length, List<EnumFieldDefinition> Fields)
        : IFileContentGenerator, IBlittable
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendLine($"namespace {Namespace};", tabs);
            sb.AppendSummary(Description, tabs, nameof(EnumFlagsDefinition));
            sb.AppendLine($"[System.Flags]", tabs);
            sb.AppendLine($"public enum {Name} : {EncodingType}", tabs);
            sb.AppendLine("{", tabs++);
            foreach (var field in Fields)
            {
                if (field.Description != "")
                    sb.AppendSummary(Description, tabs, nameof(EnumFlagsDefinition));

                string fieldValue = GetFlagValueLiteral(EncodingType, field.Value);
                sb.AppendLine($"{field.Name} = {fieldValue},", tabs);
            }
            sb.AppendLine("}", --tabs);
        }

        private static string GetFlagValueLiteral(string encodingType, string bitPosition)
        {
            if (!int.TryParse(bitPosition, NumberStyles.Integer, CultureInfo.InvariantCulture, out int shift))
                return bitPosition;

            return encodingType switch
            {
                "ulong" => (1UL << shift).ToString(CultureInfo.InvariantCulture),
                "long" => (1L << shift).ToString(CultureInfo.InvariantCulture),
                "uint" => ((uint)(1UL << shift)).ToString(CultureInfo.InvariantCulture),
                "int" => (1 << shift).ToString(CultureInfo.InvariantCulture),
                "ushort" => ((ushort)(1UL << shift)).ToString(CultureInfo.InvariantCulture),
                "short" => ((short)(1 << shift)).ToString(CultureInfo.InvariantCulture),
                "byte" => ((byte)(1 << shift)).ToString(CultureInfo.InvariantCulture),
                "sbyte" => ((sbyte)(1 << shift)).ToString(CultureInfo.InvariantCulture),
                _ => (1UL << shift).ToString(CultureInfo.InvariantCulture)
            };
        }
    }
}