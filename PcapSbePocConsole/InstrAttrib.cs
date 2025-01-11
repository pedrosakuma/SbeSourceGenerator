using B3.Market.Data.Messages;

namespace PcapSbePocConsole
{
    public record InstrAttrib(
        InstrAttribType InstrAttribType,
        InstrAttribValue InstrAttribValue
    )
    {
    }
}
