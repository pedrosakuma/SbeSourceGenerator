using System;
using System.Text;

namespace SbeSourceGenerator
{
    public enum TimeUnitKind
    {
        Nanosecond,
        Microsecond,
        Millisecond,
        Second
    }

    /// <summary>
    /// Generates ToDateTime() and ToDateTimeOffset() methods on composites that match
    /// the SBE timestamp pattern (time field + constant unit field).
    /// </summary>
    public record TimestampHelperDefinition(string Namespace, string Name, string Description,
        TimeUnitKind Unit, bool IsOptionalTime) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendTabs(tabs).Append("public partial struct ").Append(Name).AppendLine();
            sb.AppendLine("{", tabs++);

            AppendToDateTime(sb, tabs);
            AppendToDateTimeOffset(sb, tabs);

            sb.AppendLine("}", --tabs);
        }

        private void AppendToDateTime(StringBuilder sb, int tabs)
        {
            var unitLabel = Unit switch
            {
                TimeUnitKind.Nanosecond => "nanoseconds",
                TimeUnitKind.Microsecond => "microseconds",
                TimeUnitKind.Millisecond => "milliseconds",
                TimeUnitKind.Second => "seconds",
                _ => "units"
            };

            sb.AppendLine("/// <summary>", tabs);
            sb.AppendTabs(tabs).Append("/// Converts ").Append(unitLabel).AppendLine(" since Unix epoch to DateTime (UTC).");
            if (IsOptionalTime)
                sb.AppendLine("/// Returns null if the time field is null.", tabs);
            sb.AppendLine("/// </summary>", tabs);

            if (IsOptionalTime)
            {
                sb.AppendTabs(tabs).Append("public readonly System.DateTime? ToDateTime() => Time.HasValue ? ");
                AppendConversionExpression(sb, "Time.Value", "System.DateTime");
                sb.AppendLine(" : null;");
            }
            else
            {
                sb.AppendTabs(tabs).Append("public readonly System.DateTime ToDateTime() => ");
                AppendConversionExpression(sb, "Time", "System.DateTime");
                sb.AppendLine(";");
            }
        }

        private void AppendToDateTimeOffset(StringBuilder sb, int tabs)
        {
            var unitLabel = Unit switch
            {
                TimeUnitKind.Nanosecond => "nanoseconds",
                TimeUnitKind.Microsecond => "microseconds",
                TimeUnitKind.Millisecond => "milliseconds",
                TimeUnitKind.Second => "seconds",
                _ => "units"
            };

            sb.AppendLine("/// <summary>", tabs);
            sb.AppendTabs(tabs).Append("/// Converts ").Append(unitLabel).AppendLine(" since Unix epoch to DateTimeOffset (UTC).");
            if (IsOptionalTime)
                sb.AppendLine("/// Returns null if the time field is null.", tabs);
            sb.AppendLine("/// </summary>", tabs);

            if (IsOptionalTime)
            {
                sb.AppendTabs(tabs).Append("public readonly System.DateTimeOffset? ToDateTimeOffset() => Time.HasValue ? ");
                AppendConversionExpression(sb, "Time.Value", "System.DateTimeOffset");
                sb.AppendLine(" : null;");
            }
            else
            {
                sb.AppendTabs(tabs).Append("public readonly System.DateTimeOffset ToDateTimeOffset() => ");
                AppendConversionExpression(sb, "Time", "System.DateTimeOffset");
                sb.AppendLine(";");
            }
        }

        private void AppendConversionExpression(StringBuilder sb, string timeAccess, string targetType)
        {
            switch (Unit)
            {
                case TimeUnitKind.Nanosecond:
                    sb.Append(targetType).Append(".UnixEpoch.AddTicks((long)(").Append(timeAccess).Append(" / 100))");
                    break;
                case TimeUnitKind.Microsecond:
                    sb.Append(targetType).Append(".UnixEpoch.AddTicks((long)(").Append(timeAccess).Append(" * 10))");
                    break;
                case TimeUnitKind.Millisecond:
                    sb.Append("System.DateTimeOffset.FromUnixTimeMilliseconds((long)").Append(timeAccess).Append(")");
                    if (targetType == "System.DateTime")
                        sb.Append(".UtcDateTime");
                    break;
                case TimeUnitKind.Second:
                    sb.Append("System.DateTimeOffset.FromUnixTimeSeconds((long)").Append(timeAccess).Append(")");
                    if (targetType == "System.DateTime")
                        sb.Append(".UtcDateTime");
                    break;
            }
        }
    }
}
