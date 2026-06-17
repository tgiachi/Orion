using OrionIrcd.Network.Buffers;

namespace OrionIrcd.Tests.Network.Buffers;

public class CircularBufferTests
{
    [Fact]
    public void PushBack_FillsBuffer()
    {
        var buffer = new CircularBuffer<int>(4);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);

        Assert.Equal(3, buffer.Size);
        Assert.Equal(1, buffer.Front());
        Assert.Equal(3, buffer.Back());
    }

    [Fact]
    public void PushBack_WhenFull_DropsFront()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);
        buffer.PushBack(4);

        Assert.Equal(3, buffer.Size);
        Assert.Equal(2, buffer.Front());
        Assert.Equal(4, buffer.Back());
    }

    [Fact]
    public void PushBackRange_EmptySpan_NoOp()
    {
        var buffer = new CircularBuffer<byte>(8);
        buffer.PushBackRange(ReadOnlySpan<byte>.Empty);

        Assert.Equal(0, buffer.Size);
    }

    [Fact]
    public void PushBackRange_FitsInBuffer_AppendsAll()
    {
        var buffer = new CircularBuffer<byte>(8);
        ReadOnlySpan<byte> data = [1, 2, 3, 4];

        buffer.PushBackRange(data);

        Assert.Equal(4, buffer.Size);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, buffer.ToArray());
    }

    [Fact]
    public void PushBackRange_MatchesPerByteSequence()
    {
        // Equivalence regression: PushBackRange must produce the same logical contents
        // as a sequence of PushBack calls with the same data.
        var sequential = new CircularBuffer<byte>(8);
        var bulk = new CircularBuffer<byte>(8);

        ReadOnlySpan<byte> data = [10, 20, 30, 40, 50, 60, 70, 80, 90, 100];

        foreach (var b in data)
        {
            sequential.PushBack(b);
        }

        bulk.PushBackRange(data);

        Assert.Equal(sequential.ToArray(), bulk.ToArray());
    }

    [Fact]
    public void PushBackRange_OverCapacity_KeepsOnlyLastN()
    {
        var buffer = new CircularBuffer<byte>(4);
        ReadOnlySpan<byte> data = [1, 2, 3, 4, 5, 6, 7];

        buffer.PushBackRange(data);

        Assert.Equal(4, buffer.Size);
        Assert.Equal(new byte[] { 4, 5, 6, 7 }, buffer.ToArray());
    }

    [Fact]
    public void PushBackRange_WrapsAcrossEnd()
    {
        var buffer = new CircularBuffer<byte>(8);
        buffer.PushBackRange([1, 2, 3, 4, 5, 6]);

        // Pop 4 from front so writes wrap around past _end.
        buffer.PopFront();
        buffer.PopFront();
        buffer.PopFront();
        buffer.PopFront();

        buffer.PushBackRange([7, 8, 9, 10]);

        Assert.Equal(6, buffer.Size);
        Assert.Equal(new byte[] { 5, 6, 7, 8, 9, 10 }, buffer.ToArray());
    }
}
