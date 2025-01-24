namespace PcapSbePocConsole.Connection.Packets
{
    [System.Runtime.CompilerServices.InlineArray(6)]
    public unsafe struct MacAddress
    {
        public byte value;
        public override string ToString()
        {
            fixed (byte* ptr = &value)
            {
                var span = new Span<byte>(ptr, 6);
                return span.ToHexString();
            }
        }
    }
}
