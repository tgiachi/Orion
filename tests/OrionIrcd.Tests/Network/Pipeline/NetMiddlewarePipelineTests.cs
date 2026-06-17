using OrionIrcd.Network.Client;
using OrionIrcd.Network.Interfaces.Middleware;
using OrionIrcd.Network.Pipeline;

namespace OrionIrcd.Tests.Network.Pipeline;

public class NetMiddlewarePipelineTests
{
    [Fact]
    public void ContainsMiddleware_AfterAdd_ReturnsTrue()
    {
        var pipeline = new NetMiddlewarePipeline();
        pipeline.AddMiddleware(new AppendMiddleware(0xCC));

        Assert.True(pipeline.ContainsMiddleware<AppendMiddleware>());
        Assert.False(pipeline.ContainsMiddleware<DropMiddleware>());
    }

    [Fact]
    public async Task ExecuteAsync_DropMiddleware_ShortCircuits()
    {
        var pipeline = new NetMiddlewarePipeline();
        var trailing = new AppendMiddleware(0xFF);
        pipeline.AddMiddleware(new DropMiddleware());
        pipeline.AddMiddleware(trailing);

        var result = await pipeline.ExecuteAsync(null, new byte[] { 0x42 }, CancellationToken.None);

        Assert.True(result.IsEmpty);
        Assert.Equal(0, trailing.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_MiddlewaresRunInRegistrationOrder()
    {
        var pipeline = new NetMiddlewarePipeline();
        pipeline.AddMiddleware(new AppendMiddleware(0xAA));
        pipeline.AddMiddleware(new AppendMiddleware(0xBB));

        var result = await pipeline.ExecuteAsync(null, new byte[] { 0x01 }, CancellationToken.None);

        Assert.Equal(new byte[] { 0x01, 0xAA, 0xBB }, result.ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_NoMiddleware_ReturnsInputUnchanged()
    {
        var pipeline = new NetMiddlewarePipeline();
        var data = new byte[] { 1, 2, 3 };

        var result = await pipeline.ExecuteAsync(null, data, CancellationToken.None);

        Assert.Equal(data, result.ToArray());
    }

    [Fact]
    public async Task ExecuteSendAsync_UsesProcessSendAsyncOverload()
    {
        var pipeline = new NetMiddlewarePipeline();
        var spy = new DirectionAwareMiddleware();
        pipeline.AddMiddleware(spy);

        await pipeline.ExecuteAsync(null, new byte[] { 1 }, CancellationToken.None);
        await pipeline.ExecuteSendAsync(null, new byte[] { 1 }, CancellationToken.None);

        Assert.Equal(1, spy.ReceiveCalls);
        Assert.Equal(1, spy.SendCalls);
    }

    [Fact]
    public void RemoveMiddleware_RemovesAllInstances()
    {
        var pipeline = new NetMiddlewarePipeline();
        pipeline.AddMiddleware(new AppendMiddleware(0x01));
        pipeline.AddMiddleware(new AppendMiddleware(0x02));
        pipeline.AddMiddleware(new DropMiddleware());

        var removed = pipeline.RemoveMiddleware<AppendMiddleware>();

        Assert.True(removed);
        Assert.False(pipeline.ContainsMiddleware<AppendMiddleware>());
        Assert.True(pipeline.ContainsMiddleware<DropMiddleware>());
    }

    [Fact]
    public void RemoveMiddleware_WhenNotPresent_ReturnsFalse()
    {
        var pipeline = new NetMiddlewarePipeline();
        Assert.False(pipeline.RemoveMiddleware<AppendMiddleware>());
    }

    private sealed class AppendMiddleware : INetMiddleware
    {
        private readonly byte _value;

        public AppendMiddleware(byte value)
        {
            _value = value;
        }

        public int CallCount { get; private set; }

        public ValueTask<ReadOnlyMemory<byte>> ProcessAsync(
            OrionTcpClient? client,
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            var output = new byte[data.Length + 1];
            data.Span.CopyTo(output);
            output[^1] = _value;

            return ValueTask.FromResult<ReadOnlyMemory<byte>>(output);
        }
    }

    private sealed class DropMiddleware : INetMiddleware
    {
        public ValueTask<ReadOnlyMemory<byte>> ProcessAsync(
            OrionTcpClient? client,
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken = default
        )
        {
            return ValueTask.FromResult(ReadOnlyMemory<byte>.Empty);
        }
    }

    private sealed class DirectionAwareMiddleware : INetMiddleware
    {
        public int ReceiveCalls { get; private set; }
        public int SendCalls { get; private set; }

        public ValueTask<ReadOnlyMemory<byte>> ProcessAsync(
            OrionTcpClient? client,
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken = default
        )
        {
            ReceiveCalls++;

            return ValueTask.FromResult(data);
        }

        public ValueTask<ReadOnlyMemory<byte>> ProcessSendAsync(
            OrionTcpClient? client,
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken = default
        )
        {
            SendCalls++;

            return ValueTask.FromResult(data);
        }
    }
}
