using B3.Market.Data.Messages;

namespace PcapSbePocConsole.Models
{
    public record Leg(
        ulong LegSecurityID,
        decimal? LegRatioQty,
        SecurityType LegSecurityType,
        Side LegSide,
        string LegSymbol
    )
    {
    }
}
