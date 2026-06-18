using System.Net.WebSockets;

namespace OrionIrcd.Tests.Support.Network;

public sealed class BlockingCloseWebSocket : WebSocket
{
    private readonly TaskCompletionSource<object?> _closeRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource<object?> _closeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private WebSocketState _state = WebSocketState.Open;

    public Task CloseStarted => _closeStarted.Task;

    public override WebSocketCloseStatus? CloseStatus => null;

    public override string? CloseStatusDescription => null;

    public override WebSocketState State => _state;

    public override string? SubProtocol => null;

    public override void Abort()
    {
        _state = WebSocketState.Aborted;
        ReleaseClose();
    }

    public override Task CloseAsync(
        WebSocketCloseStatus closeStatus,
        string? statusDescription,
        CancellationToken cancellationToken
    )
        => CloseOutputAsync(closeStatus, statusDescription, cancellationToken);

    public override async Task CloseOutputAsync(
        WebSocketCloseStatus closeStatus,
        string? statusDescription,
        CancellationToken cancellationToken
    )
    {
        _closeStarted.TrySetResult(null);
        await _closeRelease.Task.WaitAsync(cancellationToken);
        _state = WebSocketState.CloseSent;
    }

    public override void Dispose()
    {
        _state = WebSocketState.Closed;
        ReleaseClose();
    }

    public override Task<WebSocketReceiveResult> ReceiveAsync(
        ArraySegment<byte> buffer,
        CancellationToken cancellationToken
    )
        => Task.FromException<WebSocketReceiveResult>(new NotSupportedException());

    public void ReleaseClose()
        => _closeRelease.TrySetResult(null);

    public override Task SendAsync(
        ArraySegment<byte> buffer,
        WebSocketMessageType messageType,
        bool endOfMessage,
        CancellationToken cancellationToken
    )
        => Task.CompletedTask;
}
