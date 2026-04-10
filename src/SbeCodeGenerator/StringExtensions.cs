namespace SbeSourceGenerator
{
    internal static class StringExtensions
    {
        public static string FirstCharToUpper(this string value)
        {
            if (string.IsNullOrEmpty(value) || char.IsUpper(value[0]))
                return value;

            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        public static string FirstCharToLower(this string value)
        {
            if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
                return value;

            return char.ToLowerInvariant(value[0]) + value.Substring(1);
        }

        public static string ToScreamingSnakeCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var sb = new System.Text.StringBuilder(value.Length + value.Length / 2);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '_')
                {
                    sb.Append('_');
                    continue;
                }

                bool isUpper = char.IsUpper(c);
                bool isDigit = char.IsDigit(c);

                if (i > 0)
                {
                    char prev = value[i - 1];
                    bool prevIsUpper = char.IsUpper(prev);
                    bool prevIsDigit = char.IsDigit(prev);
                    bool prevIsSeparator = prev == '_';

                    if (!prevIsSeparator)
                    {
                        // lowerUpper or digitUpper → boundary (e.g., blockL → BLOCK_L)
                        if (isUpper && !prevIsUpper && !prevIsDigit)
                            sb.Append('_');
                        // upperUpperLower → acronym end (e.g., XMLParser → XML_P)
                        else if (isUpper && prevIsUpper && i + 1 < value.Length && char.IsLower(value[i + 1]))
                            sb.Append('_');
                        // digitUpper → boundary (e.g., 2M → 2_M)
                        else if (isUpper && prevIsDigit)
                            sb.Append('_');
                        // letterDigit not needed (Field1 → FIELD1), but digitLetter is:
                        else if (!isDigit && !isUpper && prevIsDigit)
                            sb.Append('_');
                    }
                }

                sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString();
        }
    }
}
