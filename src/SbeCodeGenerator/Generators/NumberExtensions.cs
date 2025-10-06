using System.Text;

namespace SbeSourceGenerator
{
    internal record NumberExtensions(string Namespace) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.Append($$"""
                using System.Runtime.CompilerServices;
                namespace {{Namespace}}; 
                public static class NumberExtensions
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static decimal ToDecimalWithPrecision(this long value, byte scale)
                    {
                        bool negative = value < 0;
                        if (negative)
                            value = -value;
                        return new decimal((int)value, (int)(value >> 32), 0, negative, scale);
                    }

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    private static decimal ToDecimalWithPrecision(this int value, byte scale)
                    {
                        bool negative = value < 0;
                        if (negative)
                            value = -value;
                        return new decimal(value, 0, 0, negative, scale);
                    }

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    private static decimal ToDecimalWithPrecision(this short value, byte scale)
                    {
                        bool negative = value < 0;
                        if (negative)
                            value = (short)-value;
                        return new decimal(value, 0, 0, negative, scale);
                    }

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static decimal? ToDecimalWithPrecision(this long? value, byte scale)
                    {
                        if(value == null)
                            return null;
                        return ToDecimalWithPrecision(value.Value, scale);
                    }
                
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    private static decimal? ToDecimalWithPrecision(this int? value, byte scale)
                    {
                        if(value == null)
                            return null;
                        return ToDecimalWithPrecision(value.Value, scale);
                    }
                
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    private static decimal? ToDecimalWithPrecision(this short? value, byte scale)
                    {
                        if(value == null)
                            return null;
                        return ToDecimalWithPrecision(value.Value, scale);
                    }

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static DateTime ToDateTimeWithUnit(this ulong time, byte unit)
                    {
                        return DateTimeOffset.UnixEpoch.AddTicks((long)(time * Math.Pow(10, 7 - unit))).DateTime;
                    }
                
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static DateTime? ToDateTimeWithUnit(this ulong? time, byte unit)
                    {
                        if(time == null)
                            return null;
                        return time.Value.ToDateTimeWithUnit(unit);
                    }

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static DateTime ToDateTimeWithUnit(this long time, byte unit)
                    {
                        return DateTimeOffset.UnixEpoch.AddTicks((long)(time * Math.Pow(10, 7 - unit))).DateTime;
                    }
                
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public static DateTime? ToDateTimeWithUnit(this long? time, byte unit)
                    {
                        if(time == null)
                            return null;
                        return time.Value.ToDateTimeWithUnit(unit);
                    }
                }
                """);
        }
    }
}