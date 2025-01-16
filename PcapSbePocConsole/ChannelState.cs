using PcapSbePocConsole.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PcapSbePocConsole
{
    public class ChannelState
    {
        private readonly Dictionary<ulong, InstrumentDefinition> instrumentsById;
        private readonly Dictionary<string, InstrumentDefinition> instrumentsBySymbol;
        private readonly Dictionary<string, List<InstrumentDefinition>> instrumentsBySecurityGroup;

        public IReadOnlyDictionary<ulong, InstrumentDefinition> InstrumentsById => instrumentsById;
        public IReadOnlyDictionary<string, InstrumentDefinition> InstrumentsBySymbol => instrumentsBySymbol;
        public IReadOnlyDictionary<string, List<InstrumentDefinition>> InstrumentsBySecurityGroup => instrumentsBySecurityGroup;

        public uint LastSequence { get; internal set; }

        public ChannelState()
        {
            instrumentsById = new Dictionary<ulong, InstrumentDefinition>();
            instrumentsBySymbol = new Dictionary<string, InstrumentDefinition>();
            instrumentsBySecurityGroup = new Dictionary<string, List<InstrumentDefinition>>();
        }

        public void Add(InstrumentDefinition instrument)
        {
            instrumentsById.Add(instrument.SecurityID, instrument);
            instrumentsBySymbol.Add(instrument.Symbol, instrument);
            if (!instrumentsBySecurityGroup.TryGetValue(instrument.SecurityGroup, out var group))
            {
                group = new List<InstrumentDefinition>();
                instrumentsBySecurityGroup.Add(instrument.SecurityGroup, group);
            }
            group.Add(instrument);
        }
    }
}
