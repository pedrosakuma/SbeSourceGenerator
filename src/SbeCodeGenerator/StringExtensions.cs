using System.Text.Json;

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

        public static string ToKebabCase(this string value)
        {
            return JsonNamingPolicy.SnakeCaseUpper.ConvertName(value);
        }
    }
}
