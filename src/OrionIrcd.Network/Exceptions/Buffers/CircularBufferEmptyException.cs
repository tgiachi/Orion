namespace OrionIrcd.Network.Exceptions.Buffers;

/// <summary>
/// Exception thrown when an operation requires at least one element in the circular buffer.
/// </summary>
public sealed class CircularBufferEmptyException : InvalidOperationException
{
    public CircularBufferEmptyException(string? message = null)
        : base(message ?? "Circular buffer is empty.") { }
}
