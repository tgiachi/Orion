namespace OrionIrcd.IRC.Services;

/// <summary>
/// Manages internal accumulation buffer state for IRC message parsing across TCP stream calls.
/// This class is responsible for maintaining the byte accumulation buffer that enables
/// the IrcMessageParser to handle incomplete messages spanning multiple TCP packets.
/// It provides a fixed 64KB buffer with methods to append new data, remove processed
/// bytes, and clear the buffer entirely.
/// The buffer is essential for streaming scenarios:
/// - TCP packets may arrive incomplete (partial message)
/// - Multiple complete messages may arrive in a single packet
/// - Messages may be split across multiple packets
/// The accumulation buffer strategy:
/// 1. Append incoming bytes to the buffer
/// 2. Search for complete messages (marked by separator)
/// 3. Extract and process complete messages
/// 4. Remove processed bytes and shift remaining data to the beginning
/// 5. Repeat for next TCP packet
/// Thread-safety: NOT thread-safe. A single ParserState instance must be used
/// by one thread at a time. For multi-threaded scenarios, use a parser instance per thread.
/// Memory usage: Fixed 64KB (65,536 bytes) regardless of actual data, allocated once at creation.
/// This enables zero-allocation message parsing during steady-state operation.
/// </summary>
public class ParserState
{
    private const int BufferSize = 65536; // 64KB
    private readonly byte[] _buffer = new byte[BufferSize];

    /// <summary>
    /// Gets a view of the currently accumulated data as a byte span.
    /// This property returns a span covering only the accumulated data portion
    /// of the internal buffer. The span is valid only for the current snapshot
    /// and should not be stored for later use, as RemoveProcessedBytes() will
    /// shift the buffer contents.
    /// Typical usage:
    /// <code>
    /// var accumulated = _state.AccumulatedData;
    /// // Search for complete messages in accumulated
    /// // Process messages
    /// // Call RemoveProcessedBytes() to shift buffer
    /// </code>
    /// </summary>
    /// <returns>A Span&lt;byte&gt; covering the accumulated data from position 0 to _dataLength.</returns>
    public Span<byte> AccumulatedData => _buffer.AsSpan(0, Length);

    /// <summary>
    /// Gets the current length of accumulated data in bytes.
    /// This represents how many bytes are currently stored in the accumulation buffer.
    /// The maximum possible value is 65,536 (64KB). This property is useful for:
    /// - Diagnostics and monitoring
    /// - Checking if buffer is approaching capacity
    /// - Determining if data is present for parsing
    /// Note: This is the data length, not the buffer capacity (which is always 64KB).
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// Clears the buffer completely, resetting the state as if newly created.
    /// This method discards all accumulated data and resets the data length to 0.
    /// The underlying 64KB buffer memory is not deallocated, only the tracked length is reset.
    /// This is useful for:
    /// - Recovering from error conditions
    /// - Resetting state between connections
    /// - Starting fresh after a protocol error
    /// Note: The buffer memory itself is NOT zeroed (for performance), only the length is reset.
    /// </summary>
    public void Clear()
        => Length = 0;

    /// <summary>
    /// Removes processed bytes from the beginning of the buffer and compacts remaining data.
    /// After complete messages are extracted and processed, this method is called to:
    /// 1. Remove the processed bytes from the front of the buffer
    /// 2. Shift remaining (unprocessed) bytes to the beginning
    /// 3. Update the internal data length counter
    /// This "sliding window" technique keeps the buffer optimized for future appends
    /// while preserving any incomplete message data for the next parse iteration.
    /// Examples:
    /// - If buffer contains "msg1\\r\\nmsg2_incomplete" and bytesToRemove=8,
    /// buffer becomes "msg2_incomplete" and length decreases accordingly
    /// - If bytesToRemove is 0 or negative, the buffer is unchanged
    /// - If bytesToRemove >= current data length, the buffer is cleared entirely
    /// Performance: Uses efficient Span&lt;T&gt; copy operations (typically optimized
    /// by the runtime to memmove for large blocks).
    /// </summary>
    /// <param name="bytesToRemove">
    /// Number of bytes to remove from the beginning. If &lt;= 0, no change. If >= dataLength, buffer is
    /// cleared.
    /// </param>
    public void RemoveProcessedBytes(int bytesToRemove)
    {
        if (bytesToRemove <= 0)
        {
            // No bytes to remove, leave buffer as is
            return;
        }

        if (bytesToRemove >= Length)
        {
            // All bytes processed, clear buffer
            Length = 0;

            return;
        }

        var remaining = Length - bytesToRemove;
        _buffer.AsSpan(bytesToRemove, remaining).CopyTo(_buffer.AsSpan(0));
        Length = remaining;
    }

    /// <summary>
    /// Attempts to append new incoming data to the accumulation buffer.
    /// This method is called each time new bytes arrive from the TCP stream.
    /// It performs a capacity check before appending to ensure the 64KB buffer
    /// is not exceeded. If the requested append would overflow the buffer,
    /// the operation fails and false is returned.
    /// The data is copied using efficient span operations and the internal
    /// data length counter is updated.
    /// Example:
    /// <code>
    /// byte[] incomingData = ...; // From TCP stream
    /// if (!parser.TryAppendData(incomingData))
    /// {
    ///     throw new InvalidOperationException("Buffer overflow");
    /// }
    /// </code>
    /// </summary>
    /// <param name="data">The new data bytes to append from the TCP stream. May be empty.</param>
    /// <returns>True if data was appended successfully; false if appending would exceed 64KB capacity.</returns>
    public bool TryAppendData(ReadOnlySpan<byte> data)
    {
        if (Length + data.Length > BufferSize)
        {
            return false;
        }

        data.CopyTo(_buffer.AsSpan(Length));
        Length += data.Length;

        return true;
    }
}
