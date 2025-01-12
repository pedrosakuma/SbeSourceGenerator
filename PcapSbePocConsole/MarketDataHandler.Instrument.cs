using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;
using System.Text;

namespace PcapSbePocConsole
{
    public partial class MarketDataHandler
    {
        public override void Callback(ref readonly SecurityDefinition_12Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(SecurityDefinition_12Data.MESSAGE_ID);
            if (state == MarketDataState.InstrumentDefinition)
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

                instrumentsById[message.SecurityID.Value] = instrument;
                instrumentsBySymbol[instrument.Symbol] = instrument;
            }
        }
    }
}
