using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace SbeSourceGenerator.Schema
{
    /// <summary>
    /// Carries XML source locations for a parsed schema node and its attributes.
    /// </summary>
    internal sealed class SchemaSourceInfo
    {
        public static SchemaSourceInfo Empty { get; } =
            new SchemaSourceInfo(Location.None, ImmutableDictionary<string, Location>.Empty);

        public SchemaSourceInfo(Location elementLocation, ImmutableDictionary<string, Location> attributeLocations)
        {
            ElementLocation = elementLocation ?? Location.None;
            AttributeLocations = attributeLocations ?? ImmutableDictionary<string, Location>.Empty;
        }

        public Location ElementLocation { get; }

        public ImmutableDictionary<string, Location> AttributeLocations { get; }

        public Location GetAttributeOrElement(string attributeName)
        {
            if (!string.IsNullOrEmpty(attributeName)
                && AttributeLocations.TryGetValue(attributeName, out var location)
                && location != Location.None)
            {
                return location;
            }

            return ElementLocation;
        }
    }
}
