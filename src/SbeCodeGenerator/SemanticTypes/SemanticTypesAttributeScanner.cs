using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SbeSourceGenerator.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace SbeSourceGenerator.SemanticTypes
{
    /// <summary>
    /// Issue #166: incremental, syntax-first scanner that picks up
    /// <c>[assembly: SbeSemanticType("Name", typeof(MyConverter))]</c> declarations
    /// in the consumer compilation, validates that the converter implements
    /// <c>ISbeSemanticConverter&lt;TWire, TSemantic&gt;</c>, and produces stable
    /// <see cref="SemanticConverterRegistration"/> DTOs (no <c>ISymbol</c>s leak
    /// past this point so the incremental cache stays sound).
    /// </summary>
    internal static class SemanticTypesAttributeScanner
    {
        private const string AttributeFullName = "SbeSourceGenerator.Runtime.SbeSemanticTypeAttribute";
        private const string InterfaceFullName = "SbeSourceGenerator.Runtime.ISbeSemanticConverter`2";

        /// <summary>Syntactically pre-filters assembly attribute lists named like the registration attribute.</summary>
        public static bool IsCandidate(SyntaxNode node, CancellationToken _)
        {
            if (node is not AttributeListSyntax list) return false;
            if (list.Target?.Identifier.Text != "assembly") return false;
            foreach (var attr in list.Attributes)
            {
                var name = attr.Name.ToString();
                if (name == "SbeSemanticType" || name == "SbeSemanticTypeAttribute"
                    || name.EndsWith(".SbeSemanticType") || name.EndsWith(".SbeSemanticTypeAttribute"))
                    return true;
            }
            return false;
        }

        /// <summary>Resolves candidate attribute lists to immutable registrations + diagnostics.</summary>
        public static ImmutableArray<UserAttributeResult> Transform(GeneratorSyntaxContext context, CancellationToken ct)
        {
            var list = (AttributeListSyntax)context.Node;
            var results = ImmutableArray.CreateBuilder<UserAttributeResult>();
            foreach (var attr in list.Attributes)
            {
                ct.ThrowIfCancellationRequested();
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attr, ct);
                if (symbolInfo.Symbol is not IMethodSymbol ctor) continue;
                if (ctor.ContainingType?.ToDisplayString() != AttributeFullName) continue;

                var args = attr.ArgumentList?.Arguments;
                if (args is null || args.Value.Count != 2) continue;

                // arg0: string semanticType
                var semanticTypeConstant = context.SemanticModel.GetConstantValue(args.Value[0].Expression, ct);
                if (!semanticTypeConstant.HasValue || semanticTypeConstant.Value is not string semanticTypeName || string.IsNullOrEmpty(semanticTypeName))
                    continue;

                // arg1: typeof(T)
                if (args.Value[1].Expression is not TypeOfExpressionSyntax typeOf) continue;
                var typeInfo = context.SemanticModel.GetTypeInfo(typeOf.Type, ct);
                if (typeInfo.Type is not INamedTypeSymbol converterSymbol) continue;

                var location = attr.GetLocation();

                // Find ISbeSemanticConverter<TWire, TSemantic> implementation.
                var iface = converterSymbol.AllInterfaces.FirstOrDefault(i =>
                    i.IsGenericType && i.ConstructedFrom?.ToDisplayString() == "SbeSourceGenerator.Runtime.ISbeSemanticConverter<TWire, TSemantic>");
                if (iface is null || iface.TypeArguments.Length != 2)
                {
                    results.Add(UserAttributeResult.Diag(Diagnostic.Create(
                        SbeDiagnostics.SemanticConverterMissingInterface,
                        location,
                        converterSymbol.ToDisplayString(),
                        semanticTypeName)));
                    continue;
                }

                var wireSpecial = iface.TypeArguments[0].SpecialType;
                var semantic = iface.TypeArguments[1];
                var converterFqn = "global::" + converterSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                    .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                var semanticDisplay = semantic.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                results.Add(UserAttributeResult.Reg(new SemanticConverterRegistration(
                    SemanticType: semanticTypeName,
                    ConverterFullyQualifiedName: converterFqn,
                    WireSpecialType: wireSpecial,
                    SemanticTypeDisplay: semanticDisplay,
                    IsBuiltIn: false,
                    Location: location)));
            }
            return results.ToImmutable();
        }
    }

    internal readonly struct UserAttributeResult
    {
        public SemanticConverterRegistration? Registration { get; }
        public Diagnostic? Diagnostic { get; }
        private UserAttributeResult(SemanticConverterRegistration? r, Diagnostic? d) { Registration = r; Diagnostic = d; }
        public static UserAttributeResult Reg(SemanticConverterRegistration r) => new(r, null);
        public static UserAttributeResult Diag(Diagnostic d) => new(null, d);
    }
}
