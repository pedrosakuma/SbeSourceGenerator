using System;
using System.Text.Json;

namespace SbeSourceGenerator
{
    internal static class StringExtensions
    {
        public static string FirstCharToUpper(this string value)
        {
            return value.ToUpper((c, i) => i == 0);
        }

        public static string ToUpper(this string value, Func<char, int, bool> predicate)
        {
            var result = new char[value.Length];
            for (int i = 0; i < value.Length; i++)
                result[i] = predicate(value[i], i) ? char.ToUpper(value[i]) : value[i];

            return new string(result);
        }

        public static string FirstCharToLower(this string value)
        {
            return value.ToLower((c, i) => i == 0);
        }

        public static string ToLower(this string value, Func<char, int, bool> predicate)
        {
            var result = new char[value.Length];
            for (int i = 0; i < value.Length; i++)
                result[i] = predicate(value[i], i) ? char.ToLower(value[i]) : value[i];

            return new string(result);
        }

        public static string ToKebabCase(this string value)
        {
            return JsonNamingPolicy.SnakeCaseUpper.ConvertName(value);
        }
    }

}
