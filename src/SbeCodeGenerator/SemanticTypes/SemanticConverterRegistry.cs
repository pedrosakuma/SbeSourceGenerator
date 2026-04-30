using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SbeSourceGenerator.SemanticTypes
{
    public sealed class SemanticConverterRegistry
    {
        public static readonly SemanticConverterRegistry Empty =
            new SemanticConverterRegistry(ImmutableDictionary<string, SemanticConverterRegistration>.Empty);

        private readonly ImmutableDictionary<string, SemanticConverterRegistration> _byName;

        private SemanticConverterRegistry(ImmutableDictionary<string, SemanticConverterRegistration> byName)
        {
            _byName = byName;
        }

        public bool TryGet(string semanticType, out SemanticConverterRegistration registration)
        {
            if (string.IsNullOrEmpty(semanticType))
            {
                registration = null!;
                return false;
            }
            return _byName.TryGetValue(semanticType, out registration!);
        }

        public static SemanticConverterRegistry Build(ImmutableArray<SemanticConverterRegistration> userRegistrations)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, SemanticConverterRegistration>(System.StringComparer.Ordinal);
            foreach (var b in BuiltInSemanticConverters.All)
                builder[b.SemanticType] = b;
            if (!userRegistrations.IsDefaultOrEmpty)
            {
                foreach (var u in userRegistrations)
                    builder[u.SemanticType] = u;
            }
            return new SemanticConverterRegistry(builder.ToImmutable());
        }

        public IEnumerable<SemanticConverterRegistration> All => _byName.Values;
    }
}
