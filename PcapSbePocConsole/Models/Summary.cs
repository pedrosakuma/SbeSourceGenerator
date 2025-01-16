using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B3.Market.Data.Messages;

namespace PcapSbePocConsole.Models
{
    public record Summary
    {
        public decimal OpeningPrice { get; set; }
        public decimal? OpeningPriceNetChange { get; set; }
        public DateOnly OpeningTradeDate { get; set; }
        public decimal ClosingPrice { get; set; }
        public DateOnly ClosingTradeDate { get; set; }
        public decimal HighPrice { get; set; }
        public DateOnly HighTradeDate { get; set; }
        public decimal LowPrice { get; set; }
        public DateOnly LowTradeDate { get; set; }

    }
}
