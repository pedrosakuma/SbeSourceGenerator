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
		public void Callback(ref readonly HighPrice_24Data message, ReadOnlySpan<byte> variablePart)
		{
			RegisterStatistics(HighPrice_24Data.MESSAGE_ID);
		}
		public void Callback(ref readonly LowPrice_25Data message, ReadOnlySpan<byte> variablePart)
		{
			RegisterStatistics(LowPrice_25Data.MESSAGE_ID);
		}
        public void Callback(ref readonly LastTradePrice_27Data message, ReadOnlySpan<byte> variablePart)
        {

        }
        
    }
}
