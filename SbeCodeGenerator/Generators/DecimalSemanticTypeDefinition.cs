using System;
using System.Collections.Generic;
using System.Linq;

namespace SbeSourceGenerator
{
    internal record DecimalSemanticTypeDefinition(string Namespace, string Name, List<IFileContentGenerator> Fields) : IFileContentGenerator
    {
        public string GenerateFileContent()
        {
            var hasNullable = Fields.Where(f => f is NullableValueFieldDefinition).Any();
            return $$"""
                namespace {{Namespace}};
                public partial struct {{Name}}
                {
                    /// <summary>
                    /// Decimal value from mantissa and exponent
                    /// </summary>
                    public decimal{{(hasNullable ? "?" : "")}} Value => Mantissa.ToDecimalWithPrecision(-Exponent);
                }
                """;
        }
    }
}