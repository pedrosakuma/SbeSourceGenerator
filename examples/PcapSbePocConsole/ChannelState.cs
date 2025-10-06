using PcapSbePocConsole.Models;

namespace PcapSbePocConsole
{
    public class ChannelState
    {
        private readonly Dictionary<ulong, Security> instrumentsById;
        private readonly Dictionary<string, Security> instrumentsBySymbol;
        private readonly Dictionary<string, List<Security>> instrumentsBySecurityGroup;

        public IReadOnlyDictionary<ulong, Security> InstrumentsById => instrumentsById;
        public IReadOnlyDictionary<string, Security> InstrumentsBySymbol => instrumentsBySymbol;
        public IReadOnlyDictionary<string, List<Security>> InstrumentsBySecurityGroup => instrumentsBySecurityGroup;

        public uint LastSequence { get; internal set; }

        public ChannelState(int capacity)
        {
            instrumentsById = new Dictionary<ulong, Security>(capacity);
            instrumentsBySymbol = new Dictionary<string, Security>(capacity);
            instrumentsBySecurityGroup = new Dictionary<string, List<Security>>();
        }

        public void Add(Definition definition)
        {
            var security = new Security(definition, 512);
            instrumentsById.Add(definition.SecurityID, security);
            instrumentsBySymbol.Add(definition.Symbol, security);
            if (!instrumentsBySecurityGroup.TryGetValue(definition.SecurityGroup, out var group))
            {
                group = new List<Security>();
                instrumentsBySecurityGroup.Add(definition.SecurityGroup, group);
            }
            group.Add(security);
        }
    }
}
