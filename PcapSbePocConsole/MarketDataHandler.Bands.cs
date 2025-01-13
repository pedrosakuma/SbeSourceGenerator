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
        public override void Callback(ref readonly PriceBand_20Data message, ReadOnlySpan<byte> variablePart)
        {
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly PriceBand_22Data message, ReadOnlySpan<byte> variablePart)
        {
            base.Callback(in message, variablePart);
        }
        public override void Callback(ref readonly QuantityBand_21Data message, ReadOnlySpan<byte> variablePart)
        {
            base.Callback(in message, variablePart);
        }
    }
}
