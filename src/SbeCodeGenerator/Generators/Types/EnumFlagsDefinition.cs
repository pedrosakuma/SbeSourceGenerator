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
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendSummary(Description, tabs);
            sb.AppendTabs(tabs).AppendLine("[System.Flags]");
            sb.AppendTabs(tabs).Append("public enum ").Append(Name).Append(" : ").Append(EncodingType).AppendLine();
            sb.AppendLine("{", tabs++);
            foreach (var field in Fields)
            {
                if (field.Description != "")
                    sb.AppendSummary(field.Description, tabs);
                else if (!string.IsNullOrEmpty(field.SinceVersion))
                {
                    sb.AppendTabs(tabs).AppendLine("/// <summary>");
                    sb.AppendTabs(tabs).Append("/// Since version ").AppendLine(field.SinceVersion);
                    sb.AppendTabs(tabs).AppendLine("/// </summary>");
                }
                if (!string.IsNullOrEmpty(field.Deprecated))
                {
                    var msg = string.IsNullOrEmpty(field.SinceVersion)
                        ? "This value is deprecated"
                        : $"This value is deprecated since version {field.SinceVersion}";
                    sb.AppendTabs(tabs).Append("[Obsolete(\"").Append(msg).AppendLine("\")]");
                }

                string fieldValue = GetFlagValueLiteral(EncodingType, field.Value);
                sb.AppendTabs(tabs).Append(field.Name).Append(" = ").Append(fieldValue).AppendLine(",");
            }
            sb.AppendLine("}", --tabs);

            AppendExtensionsClass(sb, tabs);
        }

        /// <summary>
        /// Emits a static class with aggressively-inlined bit-test extension methods.
        /// Provides Is{Flag}() per choice plus a generic Has(flag) helper.
        /// JIT inlines these to the same IL as a hand-written bit test — zero runtime cost.
        /// </summary>
        private void AppendExtensionsClass(StringBuilder sb, int tabs)
        {
            sb.AppendLine("", tabs);
            sb.AppendTabs(tabs).AppendLine("/// <summary>");
            sb.AppendTabs(tabs).Append("/// Inlinable bit-test helpers for <see cref=\"").Append(Name).AppendLine("\"/>.");
            sb.AppendTabs(tabs).AppendLine("/// </summary>");
            sb.AppendTabs(tabs).Append("public static class ").Append(Name).AppendLine("Extensions");
            sb.AppendLine("{", tabs++);

            // Generic Has(flag) helper — supports composite flags via equality
            sb.AppendTabs(tabs).AppendLine("/// <summary>");
            sb.AppendTabs(tabs).AppendLine("/// Returns true when all bits in <paramref name=\"flag\"/> are set on <paramref name=\"value\"/>.");
            sb.AppendTabs(tabs).AppendLine("/// </summary>");
            sb.AppendTabs(tabs).AppendLine("[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            sb.AppendTabs(tabs).Append("public static bool Has(this ").Append(Name).Append(" value, ").Append(Name).AppendLine(" flag)");
            sb.AppendTabs(tabs + 1).AppendLine("=> (value & flag) == flag;");
            sb.AppendLine("", tabs);

            foreach (var field in Fields)
            {
                // Skip flags whose bit position is invalid; "0" is not emittable as an Is* test.
                if (!int.TryParse(field.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    continue;

                if (!string.IsNullOrEmpty(field.Description))
                {
                    sb.AppendTabs(tabs).AppendLine("/// <summary>");
                    sb.AppendTabs(tabs).Append("/// True when <c>").Append(field.Name).Append("</c> bit is set. ").AppendLine(field.Description);
                    sb.AppendTabs(tabs).AppendLine("/// </summary>");
                }
                else
                {
                    sb.AppendTabs(tabs).AppendLine("/// <summary>");
                    sb.AppendTabs(tabs).Append("/// True when <c>").Append(field.Name).AppendLine("</c> bit is set.");
                    sb.AppendTabs(tabs).AppendLine("/// </summary>");
                }

                if (!string.IsNullOrEmpty(field.Deprecated))
                {
                    var msg = string.IsNullOrEmpty(field.SinceVersion)
                        ? "This value is deprecated"
                        : $"This value is deprecated since version {field.SinceVersion}";
                    sb.AppendTabs(tabs).Append("[Obsolete(\"").Append(msg).AppendLine("\")]");
                }

                sb.AppendTabs(tabs).AppendLine("[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
                sb.AppendTabs(tabs).Append("public static bool Is").Append(field.Name).Append("(this ").Append(Name).AppendLine(" value)");
                sb.AppendTabs(tabs + 1).Append("=> (value & ").Append(Name).Append(".").Append(field.Name).AppendLine(") != 0;");
                sb.AppendLine("", tabs);
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
