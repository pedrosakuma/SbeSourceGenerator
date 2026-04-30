using SbeSourceGenerator.SemanticTypes;
using System.Text;

namespace SbeSourceGenerator.Generators.Fields
{
    /// <summary>
    /// Issue #166: emits an additional <c>{Field}Value</c> readonly property next to
    /// a raw wire field, delegating to a registered semantic converter. Carries no
    /// layout information (does not implement <see cref="IBlittable"/> or
    /// <see cref="IBlittableMessageField"/>), so it never affects offset computation
    /// in <c>SumFieldLength()</c> nor appears in the generated <c>ToString()</c>.
    /// </summary>
    public class SemanticAccessorDefinition : IFileContentGenerator
    {
        private readonly string _fieldName;
        private readonly string _converterFqn;
        private readonly string _semanticTypeDisplay;
        private readonly bool _isOptional;
        private readonly string _semanticTypeKey;
        private readonly bool _isBuiltIn;

        public SemanticAccessorDefinition(string fieldName, string converterFullyQualifiedName,
            string semanticTypeDisplay, bool isOptional, string semanticTypeKey, bool isBuiltIn)
        {
            _fieldName = fieldName;
            _converterFqn = converterFullyQualifiedName;
            _semanticTypeDisplay = semanticTypeDisplay;
            _isOptional = isOptional;
            _semanticTypeKey = semanticTypeKey;
            _isBuiltIn = isBuiltIn;
        }

        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.AppendLine("/// <summary>", tabs);
            sb.AppendTabs(tabs).Append("/// Typed accessor for <c>").Append(_fieldName)
                .Append("</c> derived via the <c>").Append(_semanticTypeKey).Append("</c> semantic converter")
                .Append(_isBuiltIn ? " (built-in)." : ".").AppendLine();
            sb.AppendLine("/// </summary>", tabs);

            if (_isOptional)
            {
                // Optional fields expose Field/HasField/Set patterns; we read via the public Field property
                // (which already handles endian conversion and null-sentinel comparison).
                sb.AppendTabs(tabs).Append("public readonly ").Append(_semanticTypeDisplay).Append("? ").Append(_fieldName).Append("Value => ")
                    .Append(_fieldName).Append(".HasValue ? ").Append(_converterFqn).Append(".FromWire(").Append(_fieldName).Append(".Value) : null;").AppendLine();
            }
            else
            {
                sb.AppendTabs(tabs).Append("public readonly ").Append(_semanticTypeDisplay).Append(" ").Append(_fieldName).Append("Value => ")
                    .Append(_converterFqn).Append(".FromWire(").Append(_fieldName).Append(");").AppendLine();
            }
        }
    }
}
