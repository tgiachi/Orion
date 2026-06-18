namespace OrionIrcd.Network.Exceptions.Buffers;

/// <summary>
/// Exception thrown when an index is outside the valid circular buffer bounds.
/// </summary>
public sealed class CircularBufferIndexOutOfRangeException : ArgumentOutOfRangeException
{
    public CircularBufferIndexOutOfRangeException(string paramName, int index, int size)
        : base(paramName, index, $"Cannot access index {index}. Buffer size is {size}.") { }
}
