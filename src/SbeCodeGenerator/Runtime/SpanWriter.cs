using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SbeSourceGenerator.Runtime
{
    /// <summary>
    /// Delegate for custom encoding logic that can be passed to SpanWriter.
    /// Enables type-specific encoding strategies for schema evolution and complex types.
    /// </summary>
    /// <typeparam name="T">The type being encoded.</typeparam>
    /// <param name="buffer">The buffer to write to.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written if successful.</param>
    /// <returns>True if encoding succeeded; otherwise, false.</returns>
    /// <remarks>
    /// This delegate enables type-specific encoding strategies for:
    /// - Schema evolution (version-specific encoding)
    /// - Non-blittable types (strings, arrays, complex structures)
    /// - Memory alignment control (custom padding and alignment logic)
    /// - Variable-length data handling
    /// 
    /// For design rationale and pattern examples, see docs/SPAN_WRITER_DESIGN_RATIONALE.md
    /// </remarks>
    public delegate bool SpanEncoder<T>(Span<byte> buffer, T value, out int bytesWritten);

    /// <summary>
    /// A ref struct that provides sequential writing of binary data to a Span.
    /// Eliminates the need for manual offset management during encoding.
    /// Symmetric counterpart to SpanReader for encoding SBE messages.
    /// </summary>
    /// <remarks>
    /// This is a stack-only type (ref struct) that cannot be used in async methods or stored as a field.
    /// It automatically advances the internal position as data is written, preventing offset calculation errors.
    /// 
    /// Supports extensibility through:
    /// - Custom encoding delegates for schema evolution
    /// - Flexible encoding patterns for non-blittable types
    /// - Memory alignment control via custom encoders
    /// 
    /// Memory Alignment:
    /// - TryWrite{T} uses MemoryMarshal.Write which handles unaligned writes on modern platforms
    /// - Custom encoders (TryWriteWith) allow explicit alignment control
    /// - Use TrySkip for padding bytes to maintain alignment
    /// - SBE schema defines alignment requirements that encoders can implement
    /// 
    /// For comprehensive design rationale and usage patterns, see:
    /// - docs/SPAN_WRITER_DESIGN_RATIONALE.md - Design decisions and tradeoffs
    /// - docs/ENCODING_GUIDE.md - Usage examples and patterns
    /// </remarks>
    public ref struct SpanWriter
    {
        private Span<byte> _buffer;
        private int _position;

        /// <summary>
        /// Creates a new SpanWriter from the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanWriter(Span<byte> buffer)
        {
            _buffer = buffer;
            _position = 0;
        }

        /// <summary>
        /// Gets the remaining writable portion of the buffer.
        /// </summary>
        public readonly Span<byte> Remaining
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Slice(_position);
        }

        /// <summary>
        /// Gets the number of bytes written so far.
        /// </summary>
        public readonly int BytesWritten
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _position;
        }

        /// <summary>
        /// Gets the number of bytes remaining available for writing.
        /// </summary>
        public readonly int RemainingBytes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Length - _position;
        }

        /// <summary>
        /// Checks if the specified number of bytes can be written to the buffer.
        /// </summary>
        /// <param name="count">Number of bytes to check.</param>
        /// <returns>True if count bytes are available; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool CanWrite(int count) => RemainingBytes >= count;

        /// <summary>
        /// Attempts to write a blittable structure to the buffer and advances the writer position.
        /// </summary>
        /// <typeparam name="T">The type of structure to write. Must be a blittable type.</typeparam>
        /// <param name="value">The value to write.</param>
        /// <returns>True if the structure was successfully written; otherwise, false.</returns>
        /// <remarks>
        /// This method uses MemoryMarshal.Write which handles unaligned memory writes safely on modern platforms.
        /// For types requiring explicit alignment control or non-blittable types, use TryWriteWith with a custom encoder.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite<T>(T value) where T : struct
        {
            int size = Unsafe.SizeOf<T>();
            if (RemainingBytes < size)
                return false;

            MemoryMarshal.Write(_buffer.Slice(_position), ref value);
            _position += size;
            return true;
        }

        /// <summary>
        /// Writes a blittable structure to the buffer and advances the writer position.
        /// Throws InvalidOperationException if insufficient space.
        /// </summary>
        /// <typeparam name="T">The type of structure to write. Must be a blittable type.</typeparam>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">Thrown when buffer has insufficient space.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(T value) where T : struct
        {
            if (!TryWrite(value))
                throw new InvalidOperationException($"Insufficient buffer space. Required: {Unsafe.SizeOf<T>()} bytes, Available: {RemainingBytes}");
        }

        /// <summary>
        /// Attempts to write the specified bytes to the buffer and advances the writer position.
        /// </summary>
        /// <param name="bytes">Bytes to write.</param>
        /// <returns>True if the bytes were successfully written; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteBytes(ReadOnlySpan<byte> bytes)
        {
            if (RemainingBytes < bytes.Length)
                return false;

            bytes.CopyTo(_buffer.Slice(_position));
            _position += bytes.Length;
            return true;
        }

        /// <summary>
        /// Writes the specified bytes to the buffer and advances the writer position.
        /// Throws InvalidOperationException if insufficient space.
        /// </summary>
        /// <param name="bytes">Bytes to write.</param>
        /// <exception cref="InvalidOperationException">Thrown when buffer has insufficient space.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(ReadOnlySpan<byte> bytes)
        {
            if (!TryWriteBytes(bytes))
                throw new InvalidOperationException($"Insufficient buffer space to write {bytes.Length} bytes. Available: {RemainingBytes}");
        }

        /// <summary>
        /// Attempts to skip the specified number of bytes in the buffer.
        /// Optionally clears the skipped bytes to zero for security.
        /// </summary>
        /// <param name="count">Number of bytes to skip.</param>
        /// <param name="clear">Whether to zero out the skipped bytes. Default is true for security.</param>
        /// <returns>True if the bytes were successfully skipped; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySkip(int count, bool clear = true)
        {
            if (RemainingBytes < count)
                return false;

            if (clear)
                _buffer.Slice(_position, count).Clear();

            _position += count;
            return true;
        }

        /// <summary>
        /// Skips the specified number of bytes in the buffer.
        /// Optionally clears the skipped bytes to zero for security.
        /// Throws InvalidOperationException if insufficient space.
        /// </summary>
        /// <param name="count">Number of bytes to skip.</param>
        /// <param name="clear">Whether to zero out the skipped bytes. Default is true for security.</param>
        /// <exception cref="InvalidOperationException">Thrown when buffer has insufficient space.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int count, bool clear = true)
        {
            if (!TrySkip(count, clear))
                throw new InvalidOperationException($"Insufficient buffer space to skip {count} bytes. Available: {RemainingBytes}");
        }

        /// <summary>
        /// Gets a writable slice at a specific offset from the current position without advancing.
        /// Useful for reserving space and writing later (e.g., group headers).
        /// </summary>
        /// <param name="offset">Offset from current position.</param>
        /// <param name="length">Length of the slice.</param>
        /// <param name="slice">When this method returns, contains the slice if successful; otherwise, an empty span.</param>
        /// <returns>True if the slice was successfully obtained; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetSlice(int offset, int length, out Span<byte> slice)
        {
            if (offset < 0 || length < 0 || RemainingBytes < offset + length)
            {
                slice = default;
                return false;
            }

            slice = _buffer.Slice(_position + offset, length);
            return true;
        }

        /// <summary>
        /// Resets the writer to the specified position.
        /// Useful for overwriting previously written data.
        /// </summary>
        /// <param name="position">The new position. Must be between 0 and current position.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when position is invalid.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(int position = 0)
        {
            if (position < 0 || position > _buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(position), position, $"Position must be between 0 and {_buffer.Length}");

            _position = position;
        }

        /// <summary>
        /// Attempts to encode a value using a custom encoder delegate and advances the writer position.
        /// Useful for schema evolution where encoding logic may vary by version.
        /// </summary>
        /// <typeparam name="T">The type to encode.</typeparam>
        /// <param name="encoder">The custom encoder delegate.</param>
        /// <param name="value">The value to encode.</param>
        /// <returns>True if encoding succeeded; otherwise, false.</returns>
        /// <remarks>
        /// This method enables type-specific encoding logic including:
        /// - Schema evolution handling (different encoding based on version)
        /// - Non-blittable type support (strings, arrays, complex structures)
        /// - Memory alignment control (explicit padding and alignment strategies)
        /// - Variable-length data encoding
        /// 
        /// Performance: When using static methods as encoders, there are zero allocations.
        /// AggressiveInlining allows the JIT to optimize the delegate call in many scenarios.
        /// 
        /// Example:
        /// <code>
        /// static bool EncodeOrder(Span&lt;byte&gt; buffer, Order order, out int written)
        /// {
        ///     var writer = new SpanWriter(buffer);
        ///     // Custom encoding logic here
        ///     written = writer.BytesWritten;
        ///     return true;
        /// }
        /// 
        /// writer.TryWriteWith(EncodeOrder, myOrder);
        /// </code>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteWith<T>(SpanEncoder<T> encoder, T value)
        {
            if (encoder(Remaining, value, out int bytesWritten))
            {
                _position += bytesWritten;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Encodes a value using a custom encoder delegate and advances the writer position.
        /// Throws InvalidOperationException if encoding fails.
        /// </summary>
        /// <typeparam name="T">The type to encode.</typeparam>
        /// <param name="encoder">The custom encoder delegate.</param>
        /// <param name="value">The value to encode.</param>
        /// <exception cref="InvalidOperationException">Thrown when encoding fails.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteWith<T>(SpanEncoder<T> encoder, T value)
        {
            if (!TryWriteWith(encoder, value))
                throw new InvalidOperationException($"Failed to encode value using custom encoder. Available: {RemainingBytes} bytes");
        }
    }
}
