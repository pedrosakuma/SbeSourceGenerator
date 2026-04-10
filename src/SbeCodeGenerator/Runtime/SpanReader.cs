using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SbeSourceGenerator.Runtime
{
    /// <summary>
    /// Delegate for custom parsing logic that can be passed to SpanReader.
    /// Useful for schema evolution and types that need version-specific parsing.
    /// </summary>
    /// <typeparam name="T">The type being parsed.</typeparam>
    /// <param name="buffer">The buffer to parse from.</param>
    /// <param name="value">When this method returns, contains the parsed value if successful.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of bytes consumed if successful.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    /// <remarks>
    /// This delegate enables type-specific parsing strategies for:
    /// - Schema evolution (version-specific parsing)
    /// - Non-blittable types (strings, arrays, complex structures)
    /// - Memory alignment control (custom padding and alignment logic)
    /// - Variable-length data handling
    /// 
    /// For design rationale and pattern examples, see docs/SPAN_READER_DESIGN_RATIONALE.md
    /// </remarks>
    public delegate bool SpanParser<T>(ReadOnlySpan<byte> buffer, out T value, out int bytesConsumed);

    /// <summary>
    /// A ref struct that provides sequential reading of binary data from a ReadOnlySpan.
    /// Eliminates the need for manual offset management during parsing.
    /// </summary>
    /// <remarks>
    /// This is a stack-only type (ref struct) that cannot be used in async methods or stored as a field.
    /// It automatically advances the internal position as data is read, preventing offset calculation errors.
    /// 
    /// Supports extensibility through:
    /// - Custom parsing delegates for schema evolution
    /// - Flexible parsing patterns for non-blittable types
    /// - Memory alignment control via custom parsers
    /// 
    /// Memory Alignment:
    /// - TryRead{T} uses MemoryMarshal.Read which handles unaligned access on modern platforms
    /// - Custom parsers (TryReadWith) allow explicit alignment control
    /// - Use TrySkip for padding bytes to maintain alignment
    /// - SBE schema defines alignment requirements that parsers can implement
    /// 
    /// For comprehensive design rationale and usage patterns, see:
    /// - docs/SPAN_READER_EXTENSIBILITY.md - Usage examples and patterns
    /// - docs/SPAN_READER_DESIGN_RATIONALE.md - Design decisions and tradeoffs
    /// </remarks>
    public ref struct SpanReader
    {
        private ReadOnlySpan<byte> _buffer;

        /// <summary>
        /// Creates a new SpanReader from the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanReader(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
        }

        /// <summary>
        /// Gets the remaining unread portion of the buffer.
        /// </summary>
        public readonly ReadOnlySpan<byte> Remaining
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer;
        }

        /// <summary>
        /// Gets the number of bytes remaining to be read.
        /// </summary>
        public readonly int RemainingBytes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Length;
        }

        /// <summary>
        /// Checks if the specified number of bytes can be read from the buffer.
        /// </summary>
        /// <param name="count">Number of bytes to check.</param>
        /// <returns>True if count bytes are available; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool CanRead(int count) => _buffer.Length >= count;

        /// <summary>
        /// Attempts to read a blittable structure from the buffer and advances the reader position.
        /// </summary>
        /// <typeparam name="T">The type of structure to read. Must be a blittable type.</typeparam>
        /// <param name="value">When this method returns, contains the read value if successful; otherwise, the default value.</param>
        /// <returns>True if the structure was successfully read; otherwise, false.</returns>
        /// <remarks>
        /// This method uses MemoryMarshal.Read which handles unaligned memory access safely on modern platforms.
        /// For types requiring explicit alignment control or non-blittable types, use TryReadWith with a custom parser.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead<T>(out T value) where T : struct
        {
            int size = Unsafe.SizeOf<T>();
            if (_buffer.Length < size)
            {
                value = default;
                return false;
            }

            value = MemoryMarshal.Read<T>(_buffer);
            _buffer = _buffer.Slice(size);
            return true;
        }

        /// <summary>
        /// Attempts to read a block using the wire blockLength rather than sizeof(T).
        /// Advances by exactly blockLength bytes, handling zero-field structs (blockLength=0)
        /// and schema evolution (blockLength differs from sizeof(T)).
        /// </summary>
        /// <typeparam name="T">The type of structure to read. Must be a blittable type.</typeparam>
        /// <param name="blockLength">The wire blockLength from the group/message header.</param>
        /// <param name="value">When this method returns, contains the read value if successful; otherwise, the default value.</param>
        /// <returns>True if the block was successfully processed; false if buffer exhausted.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBlock<T>(int blockLength, out T value) where T : struct
        {
            value = default;
            if (blockLength <= 0)
                return true;
            if (_buffer.Length < blockLength)
                return false;
            int size = Unsafe.SizeOf<T>();
            if (blockLength >= size)
            {
                value = MemoryMarshal.Read<T>(_buffer);
            }
            else
            {
                // Partial read: copy available bytes into a zeroed struct (backward compat)
                Span<byte> temp = stackalloc byte[size];
                _buffer.Slice(0, blockLength).CopyTo(temp);
                value = MemoryMarshal.Read<T>(temp);
            }
            _buffer = _buffer.Slice(blockLength);
            return true;
        }

        /// <summary>
        /// Returns a readonly reference directly into the buffer (zero-copy).
        /// Advances by exactly blockLength bytes.
        /// Returns Unsafe.NullRef when blockLength is 0 or buffer is exhausted.
        /// Check with Unsafe.IsNullRef before accessing the returned reference.
        /// </summary>
        /// <typeparam name="T">The type of structure to reference. Must be a blittable type.</typeparam>
        /// <param name="blockLength">The wire blockLength from the group/message header.</param>
        /// <returns>A readonly reference into the buffer, or NullRef if unavailable.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T ReadBlockRef<T>(int blockLength) where T : struct
        {
            if (blockLength <= 0 || _buffer.Length < blockLength)
                return ref Unsafe.NullRef<T>();
            ref byte start = ref MemoryMarshal.GetReference(_buffer);
            _buffer = _buffer.Slice(blockLength);
            return ref Unsafe.As<byte, T>(ref start);
        }

        /// <summary>
        /// Attempts to read the specified number of bytes from the buffer and advances the reader position.
        /// </summary>
        /// <param name="count">Number of bytes to read.</param>
        /// <param name="bytes">When this method returns, contains the read bytes if successful; otherwise, an empty span.</param>
        /// <returns>True if the bytes were successfully read; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBytes(int count, out ReadOnlySpan<byte> bytes)
        {
            if (_buffer.Length < count)
            {
                bytes = default;
                return false;
            }

            bytes = _buffer.Slice(0, count);
            _buffer = _buffer.Slice(count);
            return true;
        }

        /// <summary>
        /// Attempts to skip the specified number of bytes in the buffer.
        /// </summary>
        /// <param name="count">Number of bytes to skip.</param>
        /// <returns>True if the bytes were successfully skipped; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySkip(int count)
        {
            if (_buffer.Length < count)
                return false;

            _buffer = _buffer.Slice(count);
            return true;
        }

        /// <summary>
        /// Peeks at a value without advancing the reader position.
        /// </summary>
        /// <typeparam name="T">The type of structure to peek. Must be a blittable type.</typeparam>
        /// <param name="value">When this method returns, contains the peeked value if successful; otherwise, the default value.</param>
        /// <returns>True if the structure was successfully peeked; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryPeek<T>(out T value) where T : struct
        {
            int size = Unsafe.SizeOf<T>();
            if (_buffer.Length < size)
            {
                value = default;
                return false;
            }

            value = MemoryMarshal.Read<T>(_buffer);
            return true;
        }

        /// <summary>
        /// Peeks at the specified number of bytes without advancing the reader position.
        /// </summary>
        /// <param name="count">Number of bytes to peek.</param>
        /// <param name="bytes">When this method returns, contains the peeked bytes if successful; otherwise, an empty span.</param>
        /// <returns>True if the bytes were successfully peeked; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryPeekBytes(int count, out ReadOnlySpan<byte> bytes)
        {
            if (_buffer.Length < count)
            {
                bytes = default;
                return false;
            }

            bytes = _buffer.Slice(0, count);
            return true;
        }

        /// <summary>
        /// Resets the reader to the specified buffer position.
        /// </summary>
        /// <param name="buffer">The new buffer to read from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
        }

        /// <summary>
        /// Attempts to parse a value using a custom parser delegate and advances the reader position.
        /// Useful for schema evolution where parsing logic may vary by version.
        /// </summary>
        /// <typeparam name="T">The type to parse.</typeparam>
        /// <param name="parser">The custom parser delegate.</param>
        /// <param name="value">When this method returns, contains the parsed value if successful; otherwise, the default value.</param>
        /// <returns>True if parsing succeeded; otherwise, false.</returns>
        /// <remarks>
        /// This method enables type-specific parsing logic including:
        /// - Schema evolution handling (different parsing based on version)
        /// - Non-blittable type support (strings, arrays, complex structures)
        /// - Memory alignment control (explicit padding and alignment strategies)
        /// - Variable-length data parsing
        /// 
        /// Performance: When using static methods as parsers, there are zero allocations.
        /// AggressiveInlining allows the JIT to optimize the delegate call in many scenarios.
        /// 
        /// Example:
        /// <code>
        /// static bool ParseOrder(ReadOnlySpan&lt;byte&gt; buffer, out Order order, out int consumed)
        /// {
        ///     var reader = new SpanReader(buffer);
        ///     // Custom parsing logic here
        ///     consumed = buffer.Length - reader.RemainingBytes;
        ///     return true;
        /// }
        /// 
        /// reader.TryReadWith(ParseOrder, out var order);
        /// </code>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadWith<T>(SpanParser<T> parser, out T value)
        {
            if (parser(_buffer, out value, out int bytesConsumed))
            {
                _buffer = _buffer.Slice(bytesConsumed);
                return true;
            }

            value = default!;
            return false;
        }
    }
}
