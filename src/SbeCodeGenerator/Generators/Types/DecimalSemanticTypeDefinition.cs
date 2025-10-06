using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SbeSourceGenerator
{
    internal record DecimalSemanticTypeDefinition(string Namespace, string Name, List<IFileContentGenerator> Fields) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            var hasNullable = Fields.Where(f => f is NullableValueFieldDefinition).Any();
            sb.AppendLine($"namespace {Namespace};", tabs);
            sb.AppendLine($"public partial struct {Name}", tabs);
            sb.AppendLine("{", tabs++);
            sb.AppendSummary("Decimal value from mantissa and exponent", tabs, nameof(DecimalSemanticTypeDefinition));
            sb.AppendLine($"public decimal{(hasNullable ? "?" : "")} Value => Mantissa.ToDecimalWithPrecision(-Exponent);", tabs);
            sb.AppendLine("}", --tabs);
        }
    }
}