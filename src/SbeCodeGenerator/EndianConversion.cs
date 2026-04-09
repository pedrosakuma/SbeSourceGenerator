namespace SbeSourceGenerator
{
    /// <summary>
    /// Determines how multi-byte fields handle endianness conversion.
    /// Computed from schema byteOrder + optional SbeAssumeHostEndianness hint.
    /// </summary>
    public enum EndianConversion
    {
        /// <summary>Wire format matches host — no conversion needed.</summary>
        None,

        /// <summary>Wire format always differs from host — always reverse bytes.</summary>
        AlwaysReverse,

        /// <summary>
        /// Host endianness unknown — conditionally reverse at runtime.
        /// For big-endian schemas: reverse if BitConverter.IsLittleEndian.
        /// </summary>
        Conditional
    }
}
