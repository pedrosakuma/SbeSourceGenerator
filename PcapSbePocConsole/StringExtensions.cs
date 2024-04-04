using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcapSbePocConsole
{
    internal static class StringExtensions
    {
        public static string FirstCharToUpper(this string value)
        {
            return value.ToUpper((c, i) => i == 0);
        }

        public static string ToUpper(this string value, Func<char, int, bool> predicate)
        {
            return string.Create(value.Length, (predicate, value),
                static (span, data) =>
                {
                    var predicate = data.predicate;
                    var value = data.value;
                    for (int i = 0; i < value.Length; i++)
                    {
                        span[i] = predicate(value[i], i) ? char.ToUpper(value[i]) : value[i];
                    }
                });
        }
    }
}
