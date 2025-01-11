namespace PcapSbePocConsole
{
    public record Underlyings(
        ulong UnderlyingSecurityID, 
        decimal? IndexPct,
        decimal? IndexTheoreticalQty,
        string UnderlyingSymbol
    )
    {
    }
}
