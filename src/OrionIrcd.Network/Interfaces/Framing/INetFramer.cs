namespace OrionIrcd.Network.Interfaces.Framing;

/// <summary>
/// Extracts discrete frames from a continuous byte stream.
/// </summary>
/// <remarks>
/// A framer is the optional bridge between the byte-oriented middleware pipeline
/// and a protocol-specific consumer. Implementations are typically stateless and
/// MUST be safe to call repeatedly: the same buffer may be inspected several times
/// as more bytes arrive across socket reads. Implementations holding per-connection
/// state must be supplied as a fresh instance per client.
/// </remarks>
public interface INetFramer
{
    /// <summary>
    /// Tries to read one complete frame from the start of <paramref name="buffer" />.
    /// </summary>
    /// <param name="buffer">Accumulated bytes available for inspection.</param>
    /// <param name="frameLength">
    /// The number of bytes to consume from the start of the buffer when a complete
    /// frame is present. Undefined when the method returns <c>false</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> when <paramref name="buffer" /> starts with a complete frame and
    /// <paramref name="frameLength" /> has been written; <c>false</c> when more bytes
    /// are required.
    /// </returns>
    bool TryReadFrame(ReadOnlySpan<byte> buffer, out int frameLength);
}
