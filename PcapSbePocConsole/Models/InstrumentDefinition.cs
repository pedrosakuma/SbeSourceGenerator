using B3.Market.Data.Messages;
using System.Text;

namespace PcapSbePocConsole.Models
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
        public Status Status { get; } = new();
        public Phase Phase { get; } = new();
        public Summary Summary { get; } = new();
        public OrderBook OrderBook { get; } = new();

        public static InstrumentDefinition Convert(SecurityDefinition_4Data message, ReadOnlySpan<byte> variablePart)
        {
            var instrument = new InstrumentDefinition(
                message.SecurityID.Value,
                message.SecurityExchange.ToString(),
                message.SecurityIDSource,
                message.SecurityGroup.ToString(),
                message.Symbol.ToString(),
                message.SecurityType,
                message.SecuritySubType,
                message.TotNoRelatedSym,
                message.MinPriceIncrement?.Value,
                message.StrikePrice?.Value,
                message.ContractMultiplier?.Value,
                message.PriceDivisor?.Value,
                message.SecurityValidityTimestamp.Value,
                message.NoSharesIssued,
                message.ClearingHouseID.Value,
                message.MinOrderQty.Value,
                message.MaxOrderQty.Value,
                message.MinLotSize.Value,
                message.MinTradeVol.Value,
                message.CorporateActionEventId,
                message.IssueDate.Date,
                message.MaturityDate?.Date,
                message.CountryOfIssue.ToString(),
                message.StartDate?.Date,
                message.EndDate?.Date,
                message.SettlType?.Value,
                message.SettlDate?.Date,
                message.DatedDate?.Date,
                message.IsinNumber?.ToString(),
                message.Asset.ToString(),
                message.CfiCode.ToString(),
                message.MaturityMonthYear?.Value,
                message.Currency.ToString(),
                message.StrikeCurrency?.ToString(),
                message.SecurityStrategyType.ToString(),
                message.LotType,
                message.TickSizeDenominator,
                message.Product,
                message.ExerciseStyle,
                message.PutOrCall,
                message.PriceType,
                message.MarketSegmentID,
                message.GovernanceIndicator,
                message.SecurityMatchType,
                message.MultiLegModel,
                message.MultiLegPriceMethod,
                message.MinCrossQty.Value
            );
            string? description = null;
            message.ConsumeVariableLengthSegments(variablePart,
                noUnderlyings =>
                {
                    instrument.Underlyings.Add(
                        new Underlyings(
                            noUnderlyings.UnderlyingSecurityID.Value,
                            noUnderlyings.UnderlyingSymbol.ToString()
                        )
                    );
                },
                noLegs =>
                {
                    instrument.Legs.Add(
                        new Leg(
                            noLegs.LegSecurityID.Value,
                            noLegs.LegRatioQty.Value,
                            noLegs.LegSecurityType,
                            noLegs.LegSide,
                            noLegs.LegSymbol.ToString()
                        )
                    );
                },
                noInstrAttribs =>
                {
                    instrument.InstrAttribs.Add(
                        new InstrAttrib(
                            noInstrAttribs.InstrAttribType,
                            noInstrAttribs.InstrAttribValue
                        )
                    );
                },
                securityDesc =>
                {
                    description = Encoding.UTF8.GetString(securityDesc.VarData);
                });

            instrument.Description = description;
            return instrument;
        }
        public static InstrumentDefinition Convert(SecurityDefinition_12Data message, ReadOnlySpan<byte> variablePart)
        {
            var instrument = new InstrumentDefinition(
                message.SecurityID.Value,
                message.SecurityExchange.ToString(),
                message.SecurityIDSource,
                message.SecurityGroup.ToString(),
                message.Symbol.ToString(),
                message.SecurityType,
                message.SecuritySubType,
                message.TotNoRelatedSym,
                message.MinPriceIncrement?.Value,
                message.StrikePrice?.Value,
                message.ContractMultiplier?.Value,
                message.PriceDivisor?.Value,
                message.SecurityValidityTimestamp.Value,
                message.NoSharesIssued,
                message.ClearingHouseID.Value,
                message.MinOrderQty.Value,
                message.MaxOrderQty.Value,
                message.MinLotSize.Value,
                message.MinTradeVol.Value,
                message.CorporateActionEventId,
                message.IssueDate.Date,
                message.MaturityDate?.Date,
                message.CountryOfIssue.ToString(),
                message.StartDate?.Date,
                message.EndDate?.Date,
                message.SettlType?.Value,
                message.SettlDate?.Date,
                message.DatedDate?.Date,
                message.IsinNumber?.ToString(),
                message.Asset.ToString(),
                message.CfiCode.ToString(),
                message.MaturityMonthYear?.Value,
                message.Currency.ToString(),
                message.StrikeCurrency?.ToString(),
                message.SecurityStrategyType.ToString(),
                message.LotType,
                message.TickSizeDenominator,
                message.Product,
                message.ExerciseStyle,
                message.PutOrCall,
                message.PriceType,
                message.MarketSegmentID,
                message.GovernanceIndicator,
                message.SecurityMatchType,
                message.MultiLegModel,
                message.MultiLegPriceMethod,
                message.MinCrossQty.Value
            );
            string? description = null;
            message.ConsumeVariableLengthSegments(variablePart,
                noUnderlyings =>
                {
                    instrument.Underlyings.Add(
                        new Underlyings(
                            noUnderlyings.UnderlyingSecurityID.Value,
                            noUnderlyings.UnderlyingSymbol.ToString()
                        )
                    );
                },
                noLegs =>
                {
                    instrument.Legs.Add(
                        new Leg(
                            noLegs.LegSecurityID.Value,
                            noLegs.LegRatioQty.Value,
                            noLegs.LegSecurityType,
                            noLegs.LegSide,
                            noLegs.LegSymbol.ToString()
                        )
                    );
                },
                noInstrAttribs =>
                {
                    instrument.InstrAttribs.Add(
                        new InstrAttrib(
                            noInstrAttribs.InstrAttribType,
                            noInstrAttribs.InstrAttribValue
                        )
                    );
                },
                securityDesc =>
                {
                    description = Encoding.UTF8.GetString(securityDesc.VarData);
                });

            instrument.Description = description;
            return instrument;
        }
    }
}
