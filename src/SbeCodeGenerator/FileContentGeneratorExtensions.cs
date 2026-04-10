using System.Collections.Generic;
using System.Text;

namespace SbeSourceGenerator
{
    internal static class FileContentGeneratorExtensions
    {
        public static int SumFieldLength(this IEnumerable<IFileContentGenerator> fields)
        {
            checked
            {
                int offset = 0;
                foreach (var field in fields)
                {
                    if (field is IBlittableMessageField blittableMessageField)
                    {
                        blittableMessageField.Offset ??= offset;
                        offset = blittableMessageField.Offset.Value + blittableMessageField.Length;
                    }
                    else if (field is IBlittable blittable)
                    {
                        offset += blittable.Length;
                    }
                }
                return offset;
            }
        }

        /// <summary>
        /// Appends a ToString() override that includes all named fields.
        /// </summary>
        internal static void AppendToString(StringBuilder sb, int tabs, string structName, List<IFileContentGenerator> fields)
        {
            var fieldNames = new List<string>();
            bool hasDeprecated = false;
            foreach (var field in fields)
            {
                string? name = GetFieldName(field);
                if (name != null)
                    fieldNames.Add(name);
                if (IsDeprecated(field))
                    hasDeprecated = true;
            }

            if (fieldNames.Count == 0)
                return;

            if (hasDeprecated)
                sb.AppendLine("#pragma warning disable CS0618", tabs);

            sb.AppendLine("/// <summary>Returns a string representation of this struct for debugging.</summary>", tabs);
            sb.AppendTabs(tabs).Append("public readonly override string ToString() => $\"").Append(structName).Append(" {{ ");

            for (int i = 0; i < fieldNames.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(fieldNames[i]).Append("={").Append(fieldNames[i]).Append("}");
            }

            sb.AppendLine(" }}\";");

            if (hasDeprecated)
                sb.AppendLine("#pragma warning restore CS0618", tabs);
        }

        private static bool IsDeprecated(IFileContentGenerator field)
        {
            if (field is MessageFieldDefinition mfd)
                return !string.IsNullOrEmpty(mfd.Deprecated);
            if (field is OptionalMessageFieldDefinition omfd)
                return !string.IsNullOrEmpty(omfd.Deprecated);
            return false;
        }

        private static string? GetFieldName(IFileContentGenerator field)
        {
            if (field is MessageFieldDefinition mfd)
                return mfd.Name;
            if (field is OptionalMessageFieldDefinition omfd)
                return omfd.Name;
            if (field is ValueFieldDefinition vfd)
                return vfd.Name;
            if (field is NullableValueFieldDefinition nvfd)
                return nvfd.Name;
            if (field is ArrayFieldDefinition afd)
                return afd.Name;
            return null;
        }
    }
}
