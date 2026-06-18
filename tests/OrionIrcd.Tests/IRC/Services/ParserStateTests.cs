using OrionIrcd.IRC.Services;

namespace OrionIrcd.Tests.IRC.Services;

public class ParserStateTests
{
    [Fact]
    public void RemoveProcessedBytes_WithCountGreaterThanLength_ClearsBuffer()
    {
        var state = new ParserState();
        state.TryAppendData([1, 2, 3]);

        state.RemoveProcessedBytes(4);

        Assert.Equal(0, state.Length);
        Assert.Empty(state.AccumulatedData.ToArray());
    }

    [Fact]
    public void RemoveProcessedBytes_WithPartialCount_KeepsUnprocessedTail()
    {
        var state = new ParserState();
        state.TryAppendData([1, 2, 3, 4, 5]);

        state.RemoveProcessedBytes(2);

        Assert.Equal(3, state.Length);
        Assert.Equal(new byte[] { 3, 4, 5 }, state.AccumulatedData.ToArray());
    }

    [Fact]
    public void TryAppendData_WhenBufferWouldOverflow_ReturnsFalse()
    {
        var state = new ParserState();
        var fullBuffer = new byte[65536];

        var first = state.TryAppendData(fullBuffer);
        var second = state.TryAppendData([1]);

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(65536, state.Length);
    }

    [Fact]
    public void TryAppendData_WithData_AppendsAndExposesAccumulatedData()
    {
        var state = new ParserState();

        var result = state.TryAppendData([1, 2, 3]);

        Assert.True(result);
        Assert.Equal(3, state.Length);
        Assert.Equal(new byte[] { 1, 2, 3 }, state.AccumulatedData.ToArray());
    }
}
