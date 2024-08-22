namespace SbeSourceGenerator
{
    /// <summary>
    /// Marker interface to indicate that a class is blittable
    /// </summary>
    public interface IBlittableMessageField
    {
        public int Length { get; }
        public int? Offset { get; set; }
    }


}
