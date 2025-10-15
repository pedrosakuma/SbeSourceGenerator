using System.Text;

namespace SbeSourceGenerator.Generators
{
    /// <summary>
    /// Generates custom encoding/decoding hook infrastructure for SBE messages.
    /// </summary>
    internal record EncodingHooksGenerator(string Namespace) : IFileContentGenerator
    {
        public void AppendFileContent(StringBuilder sb, int tabs = 0)
        {
            sb.Append($$"""
                using System;
                using System.Runtime.InteropServices;

                namespace {{Namespace}}.Runtime
                {
                    /// <summary>
                    /// Delegate for custom field encoding logic.
                    /// Allows users to intercept and modify field values before encoding.
                    /// </summary>
                    /// <typeparam name="T">The type of the field being encoded.</typeparam>
                    /// <param name="fieldName">The name of the field being encoded.</param>
                    /// <param name="value">The value to encode. Can be modified by the hook.</param>
                    /// <returns>True to continue with encoding; false to skip this field.</returns>
                    public delegate bool FieldEncodingHook<T>(string fieldName, ref T value);

                    /// <summary>
                    /// Delegate for custom field decoding logic.
                    /// Allows users to intercept and transform field values after decoding.
                    /// </summary>
                    /// <typeparam name="T">The type of the field being decoded.</typeparam>
                    /// <param name="fieldName">The name of the field being decoded.</param>
                    /// <param name="value">The decoded value. Can be modified by the hook.</param>
                    /// <returns>True to continue with decoding; false to indicate an error.</returns>
                    public delegate bool FieldDecodingHook<T>(string fieldName, ref T value);

                    /// <summary>
                    /// Delegate for custom message pre-encoding logic.
                    /// Invoked before a message is encoded to bytes.
                    /// </summary>
                    /// <typeparam name="TMessage">The type of the message being encoded.</typeparam>
                    /// <param name="message">The message to encode. Can be modified by the hook.</param>
                    /// <returns>True to continue with encoding; false to abort encoding.</returns>
                    public delegate bool MessagePreEncodingHook<TMessage>(ref TMessage message) where TMessage : struct;

                    /// <summary>
                    /// Delegate for custom message post-encoding logic.
                    /// Invoked after a message has been encoded to bytes.
                    /// </summary>
                    /// <typeparam name="TMessage">The type of the message that was encoded.</typeparam>
                    /// <param name="message">The message that was encoded.</param>
                    /// <param name="buffer">The buffer containing the encoded message. Can be modified.</param>
                    public delegate void MessagePostEncodingHook<TMessage>(ref TMessage message, Span<byte> buffer) where TMessage : struct;

                    /// <summary>
                    /// Delegate for custom message pre-decoding logic.
                    /// Invoked before a message is decoded from bytes.
                    /// </summary>
                    /// <param name="buffer">The buffer containing the encoded message.</param>
                    /// <returns>True to continue with decoding; false to abort decoding.</returns>
                    public delegate bool MessagePreDecodingHook(ReadOnlySpan<byte> buffer);

                    /// <summary>
                    /// Delegate for custom message post-decoding logic.
                    /// Invoked after a message has been decoded from bytes.
                    /// </summary>
                    /// <typeparam name="TMessage">The type of the message that was decoded.</typeparam>
                    /// <param name="message">The decoded message. Can be modified by the hook.</param>
                    /// <returns>True if the decoded message is valid; false to indicate an error.</returns>
                    public delegate bool MessagePostDecodingHook<TMessage>(ref TMessage message) where TMessage : struct;

                    /// <summary>
                    /// Container for encoding/decoding hooks that can be applied to messages.
                    /// Provides extensibility points for custom serialization logic.
                    /// </summary>
                    /// <typeparam name="TMessage">The type of the message.</typeparam>
                    /// <example>
                    /// <code>
                    /// var hooks = new EncodingHooks&lt;MyMessage&gt;
                    /// {
                    ///     PreEncoding = (ref MyMessage msg) => 
                    ///     {
                    ///         // Custom validation or transformation
                    ///         return msg.Price > 0;
                    ///     },
                    ///     PostDecoding = (ref MyMessage msg) => 
                    ///     {
                    ///         // Custom validation after decoding
                    ///         return msg.Quantity >= 0;
                    ///     }
                    /// };
                    /// </code>
                    /// </example>
                    public class EncodingHooks<TMessage> where TMessage : struct
                    {
                        /// <summary>
                        /// Hook invoked before encoding a message.
                        /// </summary>
                        public MessagePreEncodingHook<TMessage>? PreEncoding { get; set; }

                        /// <summary>
                        /// Hook invoked after encoding a message.
                        /// </summary>
                        public MessagePostEncodingHook<TMessage>? PostEncoding { get; set; }

                        /// <summary>
                        /// Hook invoked before decoding a message.
                        /// </summary>
                        public MessagePreDecodingHook? PreDecoding { get; set; }

                        /// <summary>
                        /// Hook invoked after decoding a message.
                        /// </summary>
                        public MessagePostDecodingHook<TMessage>? PostDecoding { get; set; }
                    }

                    /// <summary>
                    /// Helper methods for encoding/decoding messages with custom hooks.
                    /// </summary>
                    public static class EncodingHooksHelper
                    {
                        /// <summary>
                        /// Encodes a message to a byte buffer with optional custom hooks.
                        /// </summary>
                        /// <typeparam name="TMessage">The type of the message.</typeparam>
                        /// <param name="message">The message to encode.</param>
                        /// <param name="buffer">The buffer to encode into.</param>
                        /// <param name="hooks">Optional hooks to apply during encoding.</param>
                        /// <returns>True if encoding succeeded; otherwise, false.</returns>
                        public static bool TryEncode<TMessage>(ref TMessage message, Span<byte> buffer, EncodingHooks<TMessage>? hooks = null) 
                            where TMessage : struct
                        {
                            // Apply pre-encoding hook if provided
                            if (hooks?.PreEncoding != null && !hooks.PreEncoding(ref message))
                            {
                                return false;
                            }

                            // Encode the message
                            int messageSize = Marshal.SizeOf<TMessage>();
                            if (buffer.Length < messageSize)
                            {
                                return false;
                            }

                            MemoryMarshal.Write(buffer, ref message);

                            // Apply post-encoding hook if provided
                            hooks?.PostEncoding?.Invoke(ref message, buffer);

                            return true;
                        }

                        /// <summary>
                        /// Decodes a message from a byte buffer with optional custom hooks.
                        /// </summary>
                        /// <typeparam name="TMessage">The type of the message.</typeparam>
                        /// <param name="buffer">The buffer to decode from.</param>
                        /// <param name="message">The decoded message.</param>
                        /// <param name="hooks">Optional hooks to apply during decoding.</param>
                        /// <returns>True if decoding succeeded; otherwise, false.</returns>
                        public static bool TryDecode<TMessage>(ReadOnlySpan<byte> buffer, out TMessage message, EncodingHooks<TMessage>? hooks = null)
                            where TMessage : struct
                        {
                            message = default;

                            // Apply pre-decoding hook if provided
                            if (hooks?.PreDecoding != null && !hooks.PreDecoding(buffer))
                            {
                                return false;
                            }

                            // Decode the message
                            int messageSize = Marshal.SizeOf<TMessage>();
                            if (buffer.Length < messageSize)
                            {
                                return false;
                            }

                            message = MemoryMarshal.Read<TMessage>(buffer);

                            // Apply post-decoding hook if provided
                            if (hooks?.PostDecoding != null && !hooks.PostDecoding(ref message))
                            {
                                return false;
                            }

                            return true;
                        }
                    }
                }
                """);
        }
    }
}
