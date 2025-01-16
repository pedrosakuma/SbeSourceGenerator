using B3.Market.Data.Messages;
using PcapSbePocConsole.Models;
using System.Text;

namespace PcapSbePocConsole
{
    public partial class MarketDataHandler
    {
        public void Callback(ref readonly EmptyBook_9Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(EmptyBook_9Data.MESSAGE_ID);

        }

        public void Callback(ref readonly Order_MBO_50Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(Order_MBO_50Data.MESSAGE_ID);

        }
        public void Callback(ref readonly DeleteOrder_MBO_51Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(DeleteOrder_MBO_51Data.MESSAGE_ID);

        }

        public void Callback(ref readonly MassDeleteOrders_MBO_52Data message, ReadOnlySpan<byte> variablePart)
        {
            RegisterStatistics(MassDeleteOrders_MBO_52Data.MESSAGE_ID);

        }

        public void Callback(ref readonly SnapshotFullRefresh_Orders_MBO_71Data message, ReadOnlySpan<byte> variablePart)
        {
            message.ConsumeVariableLengthSegments(variablePart, order =>
            {
            });
            RegisterStatistics(SnapshotFullRefresh_Orders_MBO_71Data.MESSAGE_ID);

        }
    }
}
