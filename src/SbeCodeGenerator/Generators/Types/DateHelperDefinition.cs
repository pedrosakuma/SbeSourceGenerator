using System.Text;

namespace SbeSourceGenerator
{
    /// <summary>
    /// Generates a ToDateOnly() method on types with semanticType="LocalMktDate"
    /// (days since Unix epoch) and composites with semanticType="MonthYear".
    /// </summary>
    public record DateHelperDefinition : IFileContentGenerator
    {
        public string Namespace { get; }
        public string Name { get; }
        public DateHelperKind Kind { get; }

        private DateHelperDefinition(string ns, string name, DateHelperKind kind)
        {
            Namespace = ns;
            Name = name;
            Kind = kind;
        }

        public static DateHelperDefinition LocalMktDate(string ns, string name) =>
            new(ns, name, DateHelperKind.LocalMktDate);

        public static DateHelperDefinition LocalMktDateOptional(string ns, string name) =>
            new(ns, name, DateHelperKind.LocalMktDateOptional);

        public static DateHelperDefinition MonthYear(string ns, string name, bool yearOptional, bool monthOptional, bool hasDay) =>
            new(ns, name, DateHelperKind.MonthYear)
            {
                YearOptional = yearOptional,
                MonthOptional = monthOptional,
                HasDay = hasDay
            };

        public bool YearOptional { get; private init; }
        public bool MonthOptional { get; private init; }
        public bool HasDay { get; private init; }

        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendTabs(tabs).Append("namespace ").Append(Namespace).AppendLine(";");
            sb.AppendTabs(tabs).Append("public partial struct ").Append(Name).AppendLine();
            sb.AppendLine("{", tabs++);

            switch (Kind)
            {
                case DateHelperKind.LocalMktDate:
                    sb.AppendLine("/// <summary>Converts days since Unix epoch to DateOnly.</summary>", tabs);
                    sb.AppendLine("public readonly System.DateOnly ToDateOnly() => System.DateOnly.FromDateTime(System.DateTime.UnixEpoch.AddDays(Value));", tabs);
                    break;
                case DateHelperKind.LocalMktDateOptional:
                    sb.AppendLine("/// <summary>Converts days since Unix epoch to DateOnly. Returns null if the value is null.</summary>", tabs);
                    sb.AppendLine("public readonly System.DateOnly? ToDateOnly() => Value.HasValue ? System.DateOnly.FromDateTime(System.DateTime.UnixEpoch.AddDays(Value.Value)) : null;", tabs);
                    break;
                case DateHelperKind.MonthYear:
                    AppendMonthYearToDateOnly(sb, tabs);
                    break;
            }

            sb.AppendLine("}", --tabs);
        }

        private void AppendMonthYearToDateOnly(StringBuilder sb, int tabs)
        {
            sb.AppendLine("/// <summary>Converts to DateOnly using year, month, and day (defaults day to 1 if not set). Returns null if year or month is not set.</summary>", tabs);

            var yearAccess = YearOptional ? "Year" : "(ushort?)Year";
            var monthAccess = MonthOptional ? "Month" : "(byte?)Month";
            var dayExpr = HasDay ? "Day ?? 1" : "1";

            sb.AppendTabs(tabs).Append("public readonly System.DateOnly? ToDateOnly() => ")
                .Append(yearAccess).Append(" is { } y && ").Append(monthAccess).Append(" is { } m ? new System.DateOnly(y, m, ").Append(dayExpr).AppendLine(") : null;");
        }
    }

    public enum DateHelperKind
    {
        LocalMktDate,
        LocalMktDateOptional,
        MonthYear
    }
}
