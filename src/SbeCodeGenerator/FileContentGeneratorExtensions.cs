using System.Collections.Generic;

namespace SbeSourceGenerator
{
    internal static class FileContentGeneratorExtensions
    {
        public static int SumFieldLength(this IEnumerable<IFileContentGenerator> fields)
        {
            int offset = 0;
            foreach (var field in fields)
            {
                if (field is IBlittableMessageField blittableMessageField)
                {
                    blittableMessageField.Offset ??= offset;
                    offset = blittableMessageField.Offset.Value + blittableMessageField.Length;
                }
                else if (field is IBlittable blittable)
                {
                    offset += blittable.Length;
                }
            }
            return offset;
        }
    }
}
