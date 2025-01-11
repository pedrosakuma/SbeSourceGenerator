using B3.Market.Data.Messages;

namespace PcapSbePocConsole
{
    public record InstrumentDefinition(
        ulong SecurityID,
        string SecurityExchange,
        SecurityIDSource SecurityIDSource,
        string SecurityGroup,
        string Symbol,
        SecurityType SecurityType,
        ushort SecuritySubType,
        uint TotNoRelatedSym,
        decimal? MinPriceIncrement,
        decimal? StrikePrice,
        decimal? ContractMultiplier,
        decimal? PriceDivisor,
        DateTime? SecurityValidityTimestamp,
        ulong NoSharesIssued,
        ulong? ClearingHouseID,
        long? MinOrderQty,
        long? MaxOrderQty,
        long? MinLotSize,
        long? MinTradeVol,
        uint CorporateActionEventId,
        DateOnly IssueDate,
        DateOnly? MaturityDate,
        string? CountryOfIssue,
        DateOnly? StartDate,
        DateOnly? EndDate,
        ushort? SettlType,
        DateOnly? SettlDate,
        DateOnly? DatedDate,
        string? IsinNumber,
        string Asset,
        string CfiCode,
        DateTime? MaturityMonthYear,
        string Currency,
        string? StrikeCurrency,
        string SecurityStrategyType,
        LotType? LotType,
        byte? TickSizeDenominator,
        Product Product,
        ExerciseStyle? ExerciseStyle,
        PutOrCall? PutOrCall,
        PriceType? PriceType,
        MarketSegmentID MarketSegmentID,
        GovernanceIndicator? GovernanceIndicator,
        SecurityMatchType? SecurityMatchType,
        MultiLegModel? MultiLegModel,
        MultiLegPriceMethod? MultiLegPriceMethod,
        decimal? MinCrossQty
    )
    {
        public string? Description { get; set; }
        public List<Underlyings> Underlyings { get; } = new();
        public List<Leg> Legs { get; } = new();
        public List<InstrAttrib> InstrAttribs { get; } = new();
        public LastTradePrice LastTradePrice { get; } = new(); 
    }
}
