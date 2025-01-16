using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B3.Market.Data.Messages;

namespace PcapSbePocConsole
{
    public partial class MarketDataHandler
    {
        public void Callback(ref readonly ClosingPrice_17Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(ClosingPrice_17Data.MESSAGE_ID);

        }
        public void Callback(ref readonly ExecutionStatistics_56Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(ExecutionStatistics_56Data.MESSAGE_ID);

        }
        public void Callback(ref readonly ExecutionSummary_55Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(ExecutionSummary_55Data.MESSAGE_ID);

        }
        public void Callback(ref readonly OpeningPrice_15Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(OpeningPrice_15Data.MESSAGE_ID);

        }

    }
}
