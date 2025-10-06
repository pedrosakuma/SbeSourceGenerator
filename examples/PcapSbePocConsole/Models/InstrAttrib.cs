using B3.Market.Data.Messages;

namespace PcapSbePocConsole.Models
{
    public record InstrAttrib(
        InstrAttribType InstrAttribType,
        InstrAttribValue InstrAttribValue
    )
    {
    }
}
